using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTree
{
    public class Node
    {
        //Node structure in memory
        // | 8B number of elements | 8B height | 8B parent page number || 8B child | 8B key | 8B address ||...

        public BTree BTree;
        public byte[] nodeBytes;
        public int h;
        public long pageNumber;
        public long NumberOfElements
        {
            get
            {
                return BitConverter.ToInt64(nodeBytes, 0);
            }
            set 
            {
                BitConverter.GetBytes(value).CopyTo(nodeBytes, 0);
            }
        }
        public long Height
        {
            get
            {
                return BitConverter.ToInt64(nodeBytes, sizeof(long));
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(nodeBytes, sizeof(long));
            }
        }
        public long ParentPageNumber
        {
            get
            {
                return BitConverter.ToInt64(nodeBytes, sizeof(long) * 2);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(nodeBytes, sizeof(long) * 2);
            }
        }

        //public long PageSize = FileSystem.PageSize;

        public Node(byte[] nodeBytes, int h, long address, BTree BTree)
        {

            this.nodeBytes = nodeBytes;
            this.h = h;
            this.address = address;
            this.BTree = BTree;

            if (nodeBytes.Length != FileSystem.PageSize)
            {
                throw new ArgumentException("Page have to have FileSystem size");
            }
        }


        public long GetChildPageAddressForKey(long key)
        {
            throw new NotImplementedException();
        }

        public bool AddElement(long key, char[] record)
        {
            throw new NotImplementedException();

        }
        public bool IsLeaf()
        {
            throw new NotImplementedException();

        }

        public bool Persist()
        {
            throw new NotImplementedException();
        }
    }
}
