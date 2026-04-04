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

            // Log expected payment methods for this package before creating the intent
            LogExpectedMethods(request.PackageId!, pkg.Cents);

            var intentOptions = new PaymentIntentCreateOptions
            {
                Amount = pkg.Cents,
                Currency = "eur",
                // AutomaticPaymentMethods lets Stripe + your dashboard configuration determine which
                // methods are eligible based on currency, amount, browser, and country.
                // Covers: card, bancontact, ideal, paypal, sepa_debit, klarna, link.
                // Plus Express Checkout wallet methods (apple_pay, google_pay) — browser/device dependent.
                //
                // NOT implemented:
                //   bank_transfer — requires creating a Stripe Customer object first (customer_balance flow).
                //   alma          — not a native Stripe payment method type; requires the Alma API directly.
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
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
        // GET /api/Payment/status/{paymentIntentId}
        // Called by the payment-return page to check whether the webhook already
        // processed the transaction. Returns only status — never grants coins here.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("status/{paymentIntentId}")]
        public async Task<IActionResult> GetStatus(string paymentIntentId)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
                return BadRequest("paymentIntentId is required.");

            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntentId);

            if (transaction == null)
                return NotFound(new { status = "not_found" });

            return Ok(new
            {
                status       = transaction.Status,          // pending | succeeded | failed
                packageId    = transaction.PackageId,
                colaAmount   = transaction.ColaAmount,
                amountCents  = transaction.AmountCents,
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

            // ── Diagnostic: log presence of Stripe-Signature header ────────────
            var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogError(
                    "Webhook received WITHOUT Stripe-Signature header. " +
                    "This request did not come from Stripe or the Stripe CLI.");
                return BadRequest("Missing Stripe-Signature header.");
            }

            _logger.LogInformation("Webhook: Stripe-Signature header is present (header length={Len}).", signatureHeader.Length);

            // ── Check WebhookSecret is configured ─────────────────────────────
            var webhookSecret = _config["Stripe:WebhookSecret"];
            bool secretIsMissing = string.IsNullOrWhiteSpace(webhookSecret);
            bool secretIsPlaceholder = !secretIsMissing && IsPlaceholder(webhookSecret!);

            _logger.LogInformation(
                "Webhook: WebhookSecret status — empty={IsEmpty} placeholder={IsPlaceholder} length={Len}.",
                secretIsMissing, secretIsPlaceholder, webhookSecret?.Length ?? 0);

            if (secretIsMissing || secretIsPlaceholder)
            {
                _logger.LogError(
                    "Webhook: returning HTTP 500 — Stripe:WebhookSecret is {Reason}. " +
                    "Run `stripe listen` and copy the whsec_... secret it prints into appsettings.Development.json, then restart the backend.",
                    secretIsMissing ? "empty" : "still a placeholder");
                return StatusCode(500, "WebhookSecret not configured.");
            }

            // ── Signature validation ──────────────────────────────────────────
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signatureHeader,
                    webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Webhook: returning HTTP 400 — signature validation FAILED (StripeError={StripeError}). " +
                    "Most likely cause: Stripe:WebhookSecret (length={SecretLen}) does not match the whsec_... secret " +
                    "shown by the currently running `stripe listen` session. " +
                    "Every time you restart `stripe listen`, a new secret is issued — copy it into appsettings.Development.json and restart the backend.",
                    ex.StripeError?.Message ?? ex.Message, webhookSecret!.Length);

                return BadRequest("Invalid webhook signature.");
            }

            _logger.LogInformation(
                "Webhook signature validated. EventType={EventType} EventId={EventId}",
                stripeEvent.Type, stripeEvent.Id);

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

        // ─────────────────────────────────────────────────────────────────────
        // Logs which payment methods are expected for a given package amount.
        // This is informational only — Stripe enforces eligibility at render time.
        // ─────────────────────────────────────────────────────────────────────
        private void LogExpectedMethods(string packageId, long amountCents)
        {
            var eur = amountCents / 100m;

            // Payment Element methods (all enabled in Stripe dashboard, Stripe checks limits at runtime)
            const string paymentElementMethods = "card, bancontact, ideal, sepa_debit, paypal, klarna";

            // Express Checkout Element — shown only if browser/device/platform supports them
            const string expressMethods = "apple_pay, google_pay, link (browser/device-dependent)";

            // Klarna: no explicit backend restriction added — Stripe enforces amount/eligibility at render time.
            // Klarna is typically available for EUR amounts from ~€1 to ~€2,500 depending on plan.

            // Alma: NOT a native Stripe payment method type.
            // It requires direct integration with Alma's own API (https://docs.getalma.eu).
            // The overlord package (€79.99) would be the eligible tier, but this cannot be
            // enabled via AutomaticPaymentMethods or PaymentMethodTypes — it needs a separate flow.
            var almaNote = packageId.Equals("overlord", StringComparison.OrdinalIgnoreCase)
                ? "Alma would be eligible (€79.99) but is NOT a native Stripe method — requires Alma API integration."
                : $"Alma not applicable for '{packageId}' (€{eur:F2}) and not implemented.";

            // bank_transfer: Stripe supports EUR bank transfers via the customer_balance payment method,
            // but this requires first creating a Stripe Customer object and a separate funding flow.
            // NOT implemented in this intent creation.

            _logger.LogInformation(
                "PaymentIntent for '{PackageId}' (€{Amount:F2} EUR) — " +
                "Payment Element: [{PaymentMethods}] | " +
                "Express Checkout: [{ExpressMethods}] | " +
                "Skipped: bank_transfer (requires Stripe Customer object) | {AlmaNote}",
                packageId, eur, paymentElementMethods, expressMethods, almaNote);
        }
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    public class CreateIntentRequest
    {
        public Guid PlayerId { get; set; }
        public string? PackageId { get; set; }
    }
}
