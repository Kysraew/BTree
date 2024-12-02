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
        // | 8B number of elements | 8B parent page number || 8B child address|  8B key | 8B address ||...

        public BTree BTree;
        public byte[] nodeBytes;
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

        public long ParentPageNumber
        {
            get
            {
                return BitConverter.ToInt64(nodeBytes, sizeof(long) * 1);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(nodeBytes, sizeof(long) * 1);
            }
        }

        //public long PageSize = FileSystem.PageSize;



        public Node(byte[] nodeBytes, long pageNumber, BTree BTree)
        {

            this.nodeBytes = nodeBytes;
            this.pageNumber = pageNumber;
            this.BTree = BTree;

            if (nodeBytes.Length != FileSystem.PageSize)
            {
                throw new ArgumentException("Page have to have FileSystem size");
            }
        }

        private long GetKey(int keyIndex)
        {
            return BitConverter.ToInt64(nodeBytes, sizeof(long) * 3 + keyIndex * sizeof(long));
        }
        private void SetKey(int keyIndex, long keyValue)
        {
            BitConverter.GetBytes(keyValue).CopyTo(nodeBytes, sizeof(long) * 3 + keyIndex * sizeof(long));
        }

        private long GetRecordAddress(int addressIndex)
        {
            return BitConverter.ToInt64(nodeBytes, sizeof(long) * 4 + addressIndex * sizeof(long));
        }
        private void SetRecordAddress(int addressIndex, long addressValue)
        {
            BitConverter.GetBytes(addressValue).CopyTo(nodeBytes, sizeof(long) * 4 + addressIndex * sizeof(long));
        }

        private long GetChildAddress(int childIndex)
        {
            return BitConverter.ToInt64(nodeBytes, sizeof(long) * 2 + childIndex * sizeof(long));
        }
        private void SetChildAddress(int childIndex, long childAddressValue)
        {
            BitConverter.GetBytes(childAddressValue).CopyTo(nodeBytes, sizeof(long) * 2 + childIndex * sizeof(long));
        }

        public long GetChildPageAddressForKey(long key)
        {
            throw new NotImplementedException();
        }

        public bool AddElement(long key, long nodeMainFileAddress, long? recordMainFileAddress = null)
        {
            throw new NotImplementedException();

        }
        public bool DeleteElement(long key)
        {
            throw new NotImplementedException();

        }

        public bool IsLeaf()
        {
            if (GetChildAddress(0) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal long GetIntervalChildAddressWithKey(long key) //Return child that may contain this key
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetKey(i) > key)
                {
                    return GetRecordAddress(i);
                }
            }
            return GetRecordAddress((int)NumberOfElements); //special value
        }

        internal long GetMainFileAddressByKey(long key)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetKey(i) == key)
                {
                    return GetRecordAddress(i);
                }
            }
            return 0; //special value
        }

        internal long GetLeftChildAddressFrom(long pageNumber)
        {
            throw new NotImplementedException();
        }


        internal (long leftParentKey, long leftParentMainFileAddress) GetLeftKeyAddressPairToChild(long pageNumber)
        {
            throw new NotImplementedException();
        }


        internal (long newLeftParentKey, long newLeftParentMainFileAddress) EqualizedElementAddingWithLeftSibling(Node leftSiblingNode, long leftParentKey, long leftParentMainFileAddress, long key, long nodeMainFileAddress, long? newRightSplittedPageNumber)
        {
            throw new NotImplementedException();
        }

        internal void SetLeftKeyAddressPairToChild(long pageNumber, long newLeftParentKey, long newLeftParentMainFileAddress)
        {
            throw new NotImplementedException();
        }

        internal long GetRightChildAddressFrom(long pageNumber)
        {
            throw new NotImplementedException();
        }

        internal (long rightParentKey, long rightParentMainFileAddress) GetRightKeyAddressPairToChild(long pageNumber)
        {
            throw new NotImplementedException();
        }

        internal (long newRightParentKey, long newRightParentMainFileAddress) EqualizedElementAddingWithRightSibling(Node rightSiblingNode, long rightParentKey, long rightParentMainFileAddress, long key, long nodeMainFileAddress, long? newRightSplittedPageNumber)
        {
            throw new NotImplementedException();
        }

        internal void SetUpNewRootNode(long parentNodeNewKey, long parentNodeNewMainFileAddress, long pageNumber1, long pageNumber2)
        {
            throw new NotImplementedException();
        }

        internal void SetParentAddress(long pageNumber)
        {
            throw new NotImplementedException();
        }

        internal (long parentNodeNewKey, long parentNodeNewMainFileAddress) SplitWith(Node newNode, long key, long mainFileAddress, long? newRightSplittedNodePageNumber)
        {
            throw new NotImplementedException();
        }
    }
}
