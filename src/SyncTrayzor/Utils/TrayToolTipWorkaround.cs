using System;
using System.Windows;
using System.Reflection;
using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;

namespace SyncTrayzor.Utils
{
    public static class TrayToolTipWorkaround {
        private static bool hasFixed = false;
        private static bool legacyMode = false;

        private static void setTipTextMode(TaskbarIcon taskbarIcon) {
            if (!hasFixed || taskbarIcon == null) {
                return;
            }

            if (taskbarIcon.TrayToolTip == null && taskbarIcon.TrayToolTipResolved != null) {
                // icon data
                FieldInfo iconDataField = typeof(TaskbarIcon).GetField("iconData", BindingFlags.NonPublic | BindingFlags.Instance);
                if (iconDataField == null) {
                    return;
                }

                NotifyIconData iconData = (NotifyIconData) iconDataField.GetValue(taskbarIcon);
                IconDataMembers flags = IconDataMembers.Tip;
                if (legacyMode) {
                    flags |= IconDataMembers.UseLegacyToolTips;
                }
                if (iconData.ValidMembers != flags) {
                    iconData.ValidMembers = flags;

                    // get util type
                    Type utilType = typeof(TaskbarIcon).GetTypeInfo().Assembly.GetType("Hardcodet.Wpf.TaskbarNotification.Util");
                    if (utilType == null)
                    {
                        return;
                    }

                    // get write method
                    MethodInfo writeIconDataMethod = utilType.GetMethod("WriteIconData", new Type[] { typeof(NotifyIconData).MakeByRefType(), typeof(NotifyCommand) });
                    if (writeIconDataMethod == null)
                    {
                        return;
                    }

                    writeIconDataMethod.Invoke(null, new object[] { iconData, NotifyCommand.Modify });
                }
            }
        }

        private static void doFix(TaskbarIcon taskbarIcon) {
            // register new DependencyProperty
            DependencyProperty newProperty = DependencyProperty.Register("NewToolTipText", typeof(string), typeof(TaskbarIcon),
                new FrameworkPropertyMetadata(string.Empty, (d, e) => {
                    // call the origin callback func
                    MethodInfo originCbMethod = typeof(TaskbarIcon).GetMethod("ToolTipTextPropertyChanged", BindingFlags.Static | BindingFlags.NonPublic);
                    originCbMethod?.Invoke(null, new object[] { d, e });

                    // try to refresh legacy mode
                    TaskbarIcon t = d as TaskbarIcon;
                    setTipTextMode(t);
                })
            );

            // back up tip text
            string tipTextBackup = string.Empty;
            if (taskbarIcon != null && !string.IsNullOrEmpty(taskbarIcon.ToolTipText)) { 
                tipTextBackup = taskbarIcon.ToolTipText;
            }

            // set new ToolTipTextProperty, every time update ToolTipText, will run own func
            var propertyFieldInfo = typeof(TaskbarIcon).GetField("ToolTipTextProperty", BindingFlags.Static | BindingFlags.Public);
            propertyFieldInfo.SetValue(null, newProperty);

            // set tip text
            if (taskbarIcon != null) {
                taskbarIcon.ToolTipText  = tipTextBackup;
            }
        }

		public static void SetLegacyMode(bool _legacyMode, TaskbarIcon taskbarIcon)
        {
            if(_legacyMode && !hasFixed) { 
                doFix(taskbarIcon);
                hasFixed = true;
            }
            legacyMode = _legacyMode;
            setTipTextMode(taskbarIcon);
        }
    }
}
