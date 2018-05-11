using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Hani.Utilities
{
    internal static class ListExtensions
    {
        internal static async Task<string> GetNewName(this SmartItem[] items, SmartItem item)
        {
            if (items.Length == 0) return item.ItemName;

            string oName = item.ItemName;
            string name = string.Empty;
            bool hasExtension = item.IsFile && !item.Extension.NullEmpty();

            if (hasExtension) oName = oName.Replace('.' + item.Extension, string.Empty);

            await Task.Run(() =>
            {
                int i = 0;

                while (i < items.Length)
                {
                    name = oName + '_' + ++i;
                    if (GetItemID(items, item.ItemFolder + name +
                        ((hasExtension) ? '.' + item.Extension : string.Empty)) == -1) break;
                }
            });

            if (hasExtension) name += '.' + item.Extension;

            return name;
        }

        internal static int GetItemID(SmartItem[] items, string fullName)
        {
            if (items.Length == 0) return -1;

            int id = -1;
            Parallel.For(0, items.Length, (i, loopState) =>
            {
                if (items[i].FullName == fullName)
                {
                    id = i;
                    loopState.Stop();
                }
            });

            return id;
        }

        internal static SmartItem[] SelectedItems(this ListView list, SmartCollection collection)
        {
            if (list.SelectedItems.Count == 0) return null;
            SmartItem[] items = new SmartItem[list.SelectedItems.Count];

            try { list.SelectedItems.CopyTo(items, 0); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            list.UnselectAll();
            if (items.Length == 0) return null;

            List<SmartItem> itemsList = new List<SmartItem>();
            for (int i = 0; i < items.Length; i++)
            {
                if ((items[i].Status == ItemStatus.CreateError) || ((items[i].Status == ItemStatus.UploadError) && (items[i].Length == 0)))
                {
                    AppMessage.Add("\"" + items[i].ItemName + "\" Dose not Exist.", MessageType.Warning);
                    collection.Remove(items[i]);
                }
                else itemsList.Add(items[i]);
            }

            return (itemsList.Count > 0) ? itemsList.ToArray() : null;
        }

        internal static SmartItem[] SelectedItems(this ListBox list)
        {
            if (list.SelectedItems.Count == 0) return null;
            SmartItem[] items = new SmartItem[list.SelectedItems.Count];

            try { list.SelectedItems.CopyTo(items, 0); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            list.UnselectAll();

            return (items.Length > 0) ? items : null;
        }

        internal static SmartItem SelectedItem(this ListBox list)
        {
            if (list.SelectedItems.Count != 1) return null;

            SmartItem item = null;

            try { item = list.SelectedItem as SmartItem; }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            list.UnselectAll();

            return item;
        }

        internal static SmartItem SelectedItem(this ListView list)
        {
            if (list.SelectedItems.Count != 1) return null;

            SmartItem item = null;

            try { item = list.SelectedItem as SmartItem; }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            list.UnselectAll();

            return item;
        }

        internal static MessageItem[] SelectedMessages(this ListView list)
        {
            if (list.SelectedItems.Count == 0) return null;
            MessageItem[] items = new MessageItem[list.SelectedItems.Count];

            try { list.SelectedItems.CopyTo(items, 0); }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
            if (items.Length == 0) return null;

            Array.Sort(items, delegate(MessageItem message1, MessageItem message2)
            {
                return message1.DateTicks.CompareTo(message2.DateTicks);
            });

            return items;
        }
    }
}