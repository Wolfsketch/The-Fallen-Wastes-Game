using System;

namespace TheFallenWastes_Domain.Entities
{
    /// <summary>
    /// Records every Stripe PaymentIntent created for a Wasteland Cola purchase.
    /// Coins are only granted after the webhook confirms payment_intent.succeeded.
    /// StripeEventId is used for idempotency: a given Stripe event is never processed twice.
    /// </summary>
    public class PaymentTransaction
    {
        public Guid Id { get; private set; }
        public Guid PlayerId { get; private set; }

        /// <summary>Package identifier: starter | scout | commander | warlord | overlord</summary>
        public string PackageId { get; private set; }

        public int ColaAmount { get; private set; }

        /// <summary>Amount in EUR cents that was charged (e.g. 499 = €4.99).</summary>
        public long AmountCents { get; private set; }

        public string StripePaymentIntentId { get; private set; }

        /// <summary>pending | succeeded | failed</summary>
        public string Status { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? ProcessedAtUtc { get; private set; }

        /// <summary>
        /// The Stripe event ID that triggered the successful processing.
        /// Used to ensure idempotent webhook handling.
        /// </summary>
        public string? StripeEventId { get; private set; }

        // EF Core parameterless constructor
        private PaymentTransaction()
        {
            PackageId = string.Empty;
            StripePaymentIntentId = string.Empty;
            Status = "pending";
        }

        public static PaymentTransaction Create(
            Guid playerId,
            string packageId,
            int colaAmount,
            long amountCents,
            string stripePaymentIntentId)
        {
            return new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                PackageId = packageId,
                ColaAmount = colaAmount,
                AmountCents = amountCents,
                StripePaymentIntentId = stripePaymentIntentId,
                Status = "pending",
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        public void MarkSucceeded(string stripeEventId)
        {
            Status = "succeeded";
            StripeEventId = stripeEventId;
            ProcessedAtUtc = DateTime.UtcNow;
        }

        public void MarkFailed()
        {
            Status = "failed";
            ProcessedAtUtc = DateTime.UtcNow;
        }
    }
}
