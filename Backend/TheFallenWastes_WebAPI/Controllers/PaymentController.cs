using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        // Package definitions: id -> (colaAmount, amountCents)
        private static readonly Dictionary<string, (int Cola, long Cents)> Packages = new()
        {
            ["starter"]   = (500,   499),
            ["scout"]     = (1500,  999),
            ["commander"] = (4000,  1999),
            ["warlord"]   = (12500, 4999),
            ["overlord"]  = (25000, 7999),
        };

        private static readonly HashSet<string> PlaceholderPrefixes =
        [
            "REPLACE_", "sk_test_REPLACE", "pk_test_REPLACE", "whsec_REPLACE",
        ];

        private readonly GameDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentController> _logger;
        private readonly IHostEnvironment _env;

        public PaymentController(
            GameDbContext context,
            IConfiguration config,
            ILogger<PaymentController> logger,
            IHostEnvironment env)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _env = env;
        }

        private bool IsPlaceholder(string? value) =>
            string.IsNullOrWhiteSpace(value) ||
            PlaceholderPrefixes.Any(p => value.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/Payment/publishable-key
        // Returns the Stripe publishable key so the frontend never needs it
        // hardcoded or injected at build time.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("publishable-key")]
        public IActionResult GetPublishableKey()
        {
            var key = _config["Stripe:PublishableKey"];
            if (IsPlaceholder(key))
            {
                _logger.LogError("Stripe:PublishableKey is missing or still has a placeholder value.");
                return StatusCode(500, "Stripe PublishableKey is not configured on the server.");
            }

            return Ok(new { publishableKey = key });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/Payment/create-intent
        // Creates a Stripe PaymentIntent and stores a pending transaction.
        // Returns the clientSecret so the frontend can confirm the payment.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("create-intent")]
        public async Task<IActionResult> CreateIntent([FromBody] CreateIntentRequest request)
        {
            // Validate Stripe secret key before making any API call
            var secretKey = _config["Stripe:SecretKey"];
            if (IsPlaceholder(secretKey))
            {
                _logger.LogError("Stripe:SecretKey is missing or still has a placeholder value.");
                return StatusCode(500, "Stripe SecretKey is not configured on the server. Update appsettings.Development.json.");
            }

            if (!Packages.TryGetValue(request.PackageId?.ToLowerInvariant() ?? "", out var pkg))
                return BadRequest("Unknown package.");

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            if (player == null)
                return NotFound("Player not found.");

            var intentService = new PaymentIntentService();

            var intentOptions = new PaymentIntentCreateOptions
            {
                Amount = pkg.Cents,
                Currency = "eur",
                // Explicit list guarantees iDEAL, Bancontact and card always appear.
                // Do NOT combine with AutomaticPaymentMethods when listing types explicitly.
                PaymentMethodTypes = new List<string> { "card", "bancontact", "ideal" },
                Metadata = new Dictionary<string, string>
                {
                    ["playerId"]   = request.PlayerId.ToString(),
                    ["packageId"]  = request.PackageId!,
                    ["colaAmount"] = pkg.Cola.ToString(),
                },
                Description = $"Wasteland Cola — {pkg.Cola} Cola ({request.PackageId}) for player {player.Username}",
            };

            PaymentIntent intent;
            try
            {
                intent = await intentService.CreateAsync(intentOptions);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Stripe error creating PaymentIntent. Player={PlayerId} Package={PackageId} | " +
                    "StripeCode={Code} StripeType={Type} HttpStatus={HttpStatus} Message={Message}",
                    request.PlayerId,
                    request.PackageId,
                    ex.StripeError?.Code,
                    ex.StripeError?.Type,
                    (int?)ex.HttpStatusCode,
                    ex.StripeError?.Message ?? ex.Message);

                // In development, surface the real Stripe error so you can debug it fast.
                // In production this remains a generic message.
                var userMessage = _env.IsDevelopment()
                    ? $"[DEV] Stripe error ({ex.StripeError?.Code ?? ex.HttpStatusCode.ToString()}): {ex.StripeError?.Message ?? ex.Message}"
                    : "Payment provider error. Please try again.";

                return StatusCode(502, userMessage);
            }

            // Persist a pending transaction record immediately
            var transaction = PaymentTransaction.Create(
                playerId: request.PlayerId,
                packageId: request.PackageId!,
                colaAmount: pkg.Cola,
                amountCents: pkg.Cents,
                stripePaymentIntentId: intent.Id);

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "PaymentIntent {IntentId} created for player {PlayerId}, package {PackageId}",
                intent.Id, request.PlayerId, request.PackageId);

            return Ok(new
            {
                clientSecret = intent.ClientSecret,
                paymentIntentId = intent.Id,
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/Payment/webhook
        // Stripe sends signed webhook events here.
        // Coins are ONLY granted here, never on the frontend.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // Read raw body — Stripe signature verification requires the exact bytes
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            var webhookSecret = _config["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogError("Stripe webhook secret is not configured.");
                return StatusCode(500);
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature validation failed.");
                return BadRequest("Invalid webhook signature.");
            }

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                if (intent == null)
                {
                    _logger.LogWarning("Webhook payload was not a PaymentIntent.");
                    return Ok(); // Still return 200 so Stripe doesn't retry
                }

                await HandlePaymentSucceeded(intent, stripeEvent.Id);
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                if (intent != null)
                    await HandlePaymentFailed(intent);
            }

            return Ok();
        }

        // ─────────────────────────────────────────────────────────────────────
        private async Task HandlePaymentSucceeded(PaymentIntent intent, string stripeEventId)
        {
            // Idempotency check: if this event ID was already processed, skip
            var alreadyProcessed = await _context.PaymentTransactions
                .AnyAsync(t => t.StripeEventId == stripeEventId);

            if (alreadyProcessed)
            {
                _logger.LogInformation(
                    "Stripe event {EventId} already processed — skipping.", stripeEventId);
                return;
            }

            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.StripePaymentIntentId == intent.Id);

            if (transaction == null)
            {
                _logger.LogWarning(
                    "No PaymentTransaction found for PaymentIntent {IntentId}.", intent.Id);
                return;
            }

            if (transaction.Status == "succeeded")
            {
                _logger.LogInformation(
                    "Transaction {TxId} already marked succeeded — skipping.", transaction.Id);
                return;
            }

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == transaction.PlayerId);
            if (player == null)
            {
                _logger.LogError(
                    "Player {PlayerId} not found when fulfilling transaction {TxId}.",
                    transaction.PlayerId, transaction.Id);
                return;
            }

            player.AddWastelandCoins(transaction.ColaAmount);
            transaction.MarkSucceeded(stripeEventId);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Granted {Cola} Wasteland Cola to player {PlayerId} (tx {TxId}).",
                transaction.ColaAmount, player.Id, transaction.Id);
        }

        private async Task HandlePaymentFailed(PaymentIntent intent)
        {
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.StripePaymentIntentId == intent.Id);

            if (transaction == null) return;

            transaction.MarkFailed();
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "PaymentIntent {IntentId} failed — transaction {TxId} marked failed.",
                intent.Id, transaction.Id);
        }
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    public class CreateIntentRequest
    {
        public Guid PlayerId { get; set; }
        public string? PackageId { get; set; }
    }
}
