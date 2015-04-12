using Hardcodet.Wpf.TaskbarNotification;
using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.NotifyIcon
{
    public class BalloonConductor : IChildDelegate
    {
        private readonly TaskbarIcon taskbarIcon;
        private readonly object child;
        private readonly object view;
        private readonly TaskCompletionSource<bool?> tcs;

        public BalloonConductor(TaskbarIcon taskbarIcon, object child, object view, TaskCompletionSource<bool?> tcs)
        {
            this.taskbarIcon = taskbarIcon;
            this.child = child;
            this.view = view;
            this.tcs = tcs;

            var childAsIChild = this.child as IChild;
            if (childAsIChild != null)
                childAsIChild.Parent = this;
        }

        public void CloseItem(object item, bool? dialogResult = null)
        {
            if (item != this.child)
                return;

            if (this.taskbarIcon.CustomBalloon.Child != this.view)
                return;

            this.tcs.SetResult(dialogResult);
            this.taskbarIcon.CloseBalloon();
        }
    }
}
