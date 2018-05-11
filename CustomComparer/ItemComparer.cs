using System;
using Hani.Utilities;

namespace CustomComparer
{
    internal sealed class FTPItemComparer : ListViewCustomComparer<SmartItem>
    {
        internal override int Compare(SmartItem x, SmartItem y)
        {
            if ((x == null) || (y == null)) return 0;
            try
            {
                switch (SortBy)
                {
                    case "Size":
                        {
                            if (x.IsFile && y.IsFile) return x.Length.CompareTo(y.Length) * DirectionDig;
                            return x.IsFile.CompareTo(y.IsFile) * DirectionDig;
                        }
                    case "Type":
                        {
                            if (!x.IsFile && !y.IsFile) return x.ItemName.CompareC(y.ItemName) * DirectionDig;
                            if (!x.IsFile || !y.IsFile) return x.IsFile.CompareTo(y.IsFile);
                            return x.Extension.CompareC(y.Extension) * DirectionDig;
                        }
                    case "Permissions": return x.Permissions.CompareC(y.Permissions) * DirectionDig;
                    case "Date": return x.Modified.CompareTo(y.Modified) * DirectionDig;
                    case "Name":
                        {
                            if (x.IsFile == y.IsFile)
                                return x.ItemName.CompareC(y.ItemName) * DirectionDig;

                            if (x.IsFile) return DirectionDig;
                            else return -1 * DirectionDig;
                        }
                }
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
            return 0;
        }
    }

    internal sealed class FTPLogsComparer : ListViewCustomComparer<MessageItem>
    {
        internal override int Compare(MessageItem x, MessageItem y)
        {
            if ((x == null) || (y == null)) return 0;
            try
            {
                switch (SortBy)
                {
                    case "Message": return x.MText.CompareC(y.MText) * DirectionDig;
                    case "Date": return x.DateTicks.CompareTo(y.DateTicks) * DirectionDig;
                }
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
            return 0;
        }
    }

    internal sealed class HistoryItemComparer : ListViewCustomComparer<HistoryItem>
    {
        internal override int Compare(HistoryItem x, HistoryItem y)
        {
            bool xe;
            bool ye;
            //bool ie;
            if ((x == null) || (y == null)) return 0;
            try
            {
                switch (SortBy)
                {
                    case "Name": return x.ItemName.CompareC(y.ItemName) * DirectionDig;
                    case "OldValue": return x.OldValue.CompareC(y.OldValue) * DirectionDig;
                    case "NewValue": return x.NewValue.CompareC(y.NewValue) * DirectionDig;
                    case "Status":
                        {
                            xe = x.Item_Status.Ends("Error");
                            ye = y.Item_Status.Ends("Error");

                            if (xe == ye)
                            {
                                /*if (!xe)
                                {
                                    ie = (x.Status == "Ignored");
                                    if (ie != (y.Status == "Ignored"))
                                    {
                                        if (ie) return DirectionDig;
                                        else return -1 * DirectionDig;
                                    }
                                }*/
                                return x.Item_Status.CompareC(y.Item_Status) * DirectionDig;
                            }

                            if (xe) return DirectionDig;
                            else return -1 * DirectionDig;
                        }
                    case "Date": return x.Ticks.CompareTo(y.Ticks) * DirectionDig;
                }
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
            return 0;
        }
    }
}