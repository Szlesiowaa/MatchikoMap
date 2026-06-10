using System.Threading.Channels;

namespace MatchikoMap.Services.EmailService
{
    public interface IBackgroundEmailQueue
    {
        void QueueEmail(string to, string subject, string body);
        ValueTask<(string to, string subject, string body)> DequeueAsync(CancellationToken ct);
    }

    public class BackgroundEmailQueue : IBackgroundEmailQueue
    {
        private readonly Channel<(string to, string subject, string body)> _queue;

        public BackgroundEmailQueue()
        {
            _queue = Channel.CreateUnbounded<(string, string, string)>();
        }

        public void QueueEmail(string to, string subject, string body)
        {
            _queue.Writer.TryWrite((to, subject, body));
        }

        public ValueTask<(string to, string subject, string body)> DequeueAsync(CancellationToken ct)
        {
            return _queue.Reader.ReadAsync(ct);
        }
    }
}
