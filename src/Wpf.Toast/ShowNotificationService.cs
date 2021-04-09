using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf.Toast
{
    public class ShowNotificationService
    {
        List<Notification> _notifications;

        public ShowNotificationService()
        {
            _notifications = new List<Notification>
            {
                new TextNotification
                {
                    Id = Guid.NewGuid(), Text = "Notification 1"
                },
                new TextNotification
                {
                    Id = Guid.NewGuid(), Text = "Notification 2"
                },
                new TextNotification
                {
                    Id = Guid.NewGuid(), Text = "Notification 3"
                },
                new TextNotification
                {
                    Id = Guid.NewGuid(), Text = "Notification 4"
                },
            };
        }

        public Task MarkNotificationAsSeenAsync(Guid id)
        {
            var n = _notifications.FirstOrDefault(e => e.Id == id);

            if (n != null && n.IsSeen == false)
            {
                n.IsSeen = true;
            }

            return Task.CompletedTask;
        }
        public Task DismissNotificationAsync(Guid id)
        {
            var n = _notifications.FirstOrDefault(e => e.Id == id);

            if (n != null && n.IsDismissed == false)
            {
                n.IsDismissed = true;
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Notification>> GetNotificationsAsync()
        {
            return Task.FromResult(
                _notifications.Where(e => e.IsDismissed == false)
            );
        }

        public int QuietTimeInMinutes => 0;
    }
    public abstract class Notification
    {
        public Guid Id { get; set; }
        public string Type
        {
            get
            {
                var typeName = this.GetType().Name;

                if (typeName.ToLower().EndsWith("notification") && typeName.Length > "notification".Length)
                    return typeName.Substring(0, typeName.Length - "notification".Length);

                return typeName;
            }
        }

        public bool IsSeen { get; set; }
        public bool IsDismissed { get; set; }

        public abstract bool CanShowDetails { get; }
    }
    public class TextNotification : Notification
    {
        public string Text { get; set; }
        public override bool CanShowDetails => false;
    }
}
