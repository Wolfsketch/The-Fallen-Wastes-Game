using System;

namespace TheFallenWastes_Domain.Entities
{
    public class Message
    {
        public Guid Id { get; private set; }
        public Guid SenderPlayerId { get; private set; }
        public Guid ReceiverPlayerId { get; private set; }
        public string Subject { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        public DateTime SentAtUtc { get; private set; }
        public bool IsRead { get; private set; }

        private Message() { }

        public Message(Guid senderPlayerId, Guid receiverPlayerId, string subject, string body)
        {
            Id = Guid.NewGuid();
            SenderPlayerId = senderPlayerId;
            ReceiverPlayerId = receiverPlayerId;
            Subject = subject;
            Body = body;
            SentAtUtc = DateTime.UtcNow;
            IsRead = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}