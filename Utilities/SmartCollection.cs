using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    internal sealed class SmartCollection : ObservableCollection<SmartItem>
    {
        public int Files { get; set; }
        public int Folders { get; set; }

        //private bool isBindingPaused = false;

        //Cache
        private string name = string.Empty;
        private int id = -1;

        internal int GetID(string fullName)
        {
            if (name != fullName)
            {
                name = fullName;
                id = getItemID(this.Items, fullName);
            }

            return id;
        }

        internal int GetLastFolderID()
        {
            int i = 0;
            for (; i < this.Items.Count; i++) if (this.Items[i].IsFile == true) break;
            return i;
        }

        internal void ClearCache()
        {
            name = string.Empty;
        }

        protected override void SetItem(int index, SmartItem item)
        {
            ClearCache();
            base.SetItem(index, item);
        }

        protected override void InsertItem(int index, SmartItem item)
        {
            ClearCache();
            base.InsertItem(index, item);
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            ClearCache();
            base.MoveItem(oldIndex, newIndex);
        }

        protected override void RemoveItem(int index)
        {
            ClearCache();
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            ClearCache();
            base.ClearItems();
        }

        /*protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!isBindingPaused) base.OnCollectionChanged(e);
        }*/

        internal void SetItems(SmartItem[] items)
        {
            this.Items.Clear();
            Files = 0;
            Folders = 0;
            ClearCache();

            //isBindingPaused = true;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].IsFile) Files++;
                else Folders++;

                this.Items.Add(items[i]);
            }
            //isBindingPaused = false;

            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        internal void SetItems(SmartCollection collection)
        {
            this.Items.Clear();
            ClearCache();
            Files = collection.Files;
            Folders = collection.Folders;

            for (int i = 0; i < collection.Items.Count; i++) this.Items.Add(collection.Items[i]);

            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        internal static int getItemID(IList<SmartItem> list, string fullName)
        {
            if (list.Count == 0) return -1;

            int id = -1;
            Parallel.For(0, list.Count, (i, loopState) =>
            {
                if (list[i].FullName == fullName)
                {
                    id = i;
                    loopState.Stop();
                }
            });

            return id;
        }
    }
}