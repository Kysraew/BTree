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

        public long GetKey(int keyIndex)
        {
            return BitConverter.ToInt64(nodeBytes, sizeof(long) * 3 + 3 * keyIndex * sizeof(long));
        }
        public void SetKey(int keyIndex, long keyValue)
        {
            BitConverter.GetBytes(keyValue).CopyTo(nodeBytes, sizeof(long) * 3 + 3 * keyIndex * sizeof(long));
        }

        public long GetRecordAddress(int addressIndex)
        {
            return BitConverter.ToInt64(nodeBytes, sizeof(long) * 4 + 3 * addressIndex * sizeof(long));
        }
        public void SetRecordAddress(int addressIndex, long addressValue)
        {
            BitConverter.GetBytes(addressValue).CopyTo(nodeBytes, sizeof(long) * 4 + 3 * addressIndex * sizeof(long));
        }

        public long GetChildAddress(int childIndex)
        {
            return BitConverter.ToInt64(nodeBytes, sizeof(long) * 2 + 3 * childIndex * sizeof(long));
        }
        public void SetChildAddress(int childIndex, long childAddressValue)
        {
            BitConverter.GetBytes(childAddressValue).CopyTo(nodeBytes, sizeof(long) * 2 + 3 * childIndex * sizeof(long));
        }

        public long GetChildPageAddressForKey(long key)
        {
            throw new NotImplementedException();
        }

        public bool AddElement(long key, long nodeMainFileAddress, long? newRightSplittedPageNumber = null)
        {
            int newElementIndex = GetNewIndexForKey(key);
            int newElementBytePosition = 2 * sizeof(long) + newElementIndex * 3 * sizeof(long);

            Array.Copy(nodeBytes, newElementBytePosition, nodeBytes, newElementBytePosition + 3 * sizeof(long), ((NumberOfElements + 1) - newElementIndex) * 3 * sizeof(long)); // We also have to move the most right child pointer

            SetKey(newElementIndex, key);
            SetRecordAddress(newElementIndex, nodeMainFileAddress);

            //We have to swap child addresses
            long tempChildAddress = GetChildAddress(newElementIndex + 1);
            SetChildAddress(newElementIndex + 1, newRightSplittedPageNumber ?? 0);
            SetChildAddress(newElementIndex, tempChildAddress);

            NumberOfElements++;

            return true;
            //throw new NotImplementedException();
            //throw new NotImplementedException();

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
                    return GetChildAddress(i);
                }
            }
            return GetChildAddress((int)NumberOfElements); //special value
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
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetChildAddress(i + 1) == pageNumber)
                {
                    return GetChildAddress(i);
                }
            }
            return 0; // special value meaning not found
        }
        internal long GetRightChildAddressFrom(long pageNumber)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetChildAddress(i) == pageNumber)
                {
                    return GetChildAddress(i + 1);
                }
            }
            return 0; // special value meaning not found
        }

        internal (long leftKey, long leftMainFileAddress) GetLeftKeyAddressPairToChild(long pageNumber)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetChildAddress(i + 1) == pageNumber)
                {
                    return (GetKey(i), GetChildAddress(i));
                }
            }
            throw new Exception("This page don't have left values to this page");
        }

        internal (long rightParentKey, long rightParentMainFileAddress) GetRightKeyAddressPairToChild(long pageNumber)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetChildAddress(i) == pageNumber)
                {
                    return (GetKey(i + 1), GetRecordAddress(i + 1));
                }
            }
            throw new Exception("This page don't have right values to this page");
        }

        private int GetNewIndexForKey(long key)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetKey(i) > key)
                {
                    return i;
                }
            }
            return (int)NumberOfElements;
        }

        internal (long newLeftParentKey, long newLeftParentMainFileAddress) EqualizedElementAddingWithLeftSibling(Node leftSiblingNode, long leftParentKey, long leftParentMainFileAddress, long key, long nodeMainFileAddress, long? newRightSplittedPageNumber)
        {
            this.AddElement(key, nodeMainFileAddress, newRightSplittedPageNumber);

            long numberOfElementsOnTwoNodes = this.NumberOfElements + leftSiblingNode.NumberOfElements;
            long newRightNodeNumberOfElements = numberOfElementsOnTwoNodes / 2;
            long newLeftNodeNumberOfElements = numberOfElementsOnTwoNodes - newRightNodeNumberOfElements;

            int numberOfMovedRecords = (int)(this.NumberOfElements - newRightNodeNumberOfElements);

            long newParentKey = this.GetKey((int)numberOfMovedRecords - 1);
            long newParentMainFileAddress = this.GetRecordAddress((int)numberOfMovedRecords - 1);



            //We move parent record to the right node
            leftSiblingNode.SetKey((int)leftSiblingNode.NumberOfElements, leftParentKey);
            leftSiblingNode.SetRecordAddress((int)leftSiblingNode.NumberOfElements, leftParentMainFileAddress);
            
            // We move records form right node to left node
            Array.Copy(this.nodeBytes, 2 * sizeof(long), leftSiblingNode.nodeBytes, 2 * sizeof(long) + (NumberOfElements+1)*3*sizeof(long), 3 * sizeof(long) * ((this.NumberOfElements + 1) - numberOfMovedRecords) + sizeof(long)); // We also have to move the most right child pointer


            //We shift to left right node
            Array.Copy(this.nodeBytes, 2 * sizeof(long) + numberOfMovedRecords*3*sizeof(long), this.nodeBytes, 2 * sizeof(long), 3 * sizeof(long) * (newRightNodeNumberOfElements + 1)); // We also have to move the most right child pointer

            this.NumberOfElements = newRightNodeNumberOfElements;
            leftSiblingNode.NumberOfElements = newLeftNodeNumberOfElements;

            return (newParentKey, newParentMainFileAddress);
        }

        internal void SetLeftKeyAddressPairToChild(long pageNumber, long newLeftParentKey, long newLeftParentMainFileAddress)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if(GetChildAddress(i+1) == pageNumber)
                {
                    SetKey(i, newLeftParentKey);
                    SetRecordAddress(i, newLeftParentMainFileAddress);
                    return;
                }
            }
            throw new Exception("On the left side to page number there is no Key address pair");
        }

        internal void SetRightKeyAddressPairToChild(long pageNumber, long newRightParentKey, long newRightParentMainFileAddress)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetChildAddress(i) == pageNumber)
                {
                    SetKey(i, newRightParentKey);
                    SetRecordAddress(i, newRightParentMainFileAddress);
                    return;
                }
            }
        }



        internal (long newRightParentKey, long newRightParentMainFileAddress) EqualizedElementAddingWithRightSibling(Node rightSiblingNode, long rightParentKey, long rightParentMainFileAddress, long key, long nodeMainFileAddress, long? newRightSplittedPageNumber)
        {

            this.AddElement(key, nodeMainFileAddress, newRightSplittedPageNumber);

            long numberOfElementsOnTwoNodes = this.NumberOfElements + rightSiblingNode.NumberOfElements;
            long newRightNodeNumberOfElements = numberOfElementsOnTwoNodes / 2;
            long newLeftNodeNumberOfElements = numberOfElementsOnTwoNodes - newRightNodeNumberOfElements;

            long newParentKey = this.GetKey((int)newLeftNodeNumberOfElements);
            long newParentMainFileAddress = this.GetRecordAddress((int)newLeftNodeNumberOfElements);

            int numberOfMovedRecords = (int)(this.NumberOfElements - newLeftNodeNumberOfElements);

            //We make space for moved records
            Array.Copy(rightSiblingNode.nodeBytes, 2 * sizeof(long), rightSiblingNode.nodeBytes, 2 * sizeof(long) + 3 * numberOfMovedRecords * sizeof(long), 3 * sizeof(long) * (this.NumberOfElements + 1)); // We also have to move the most right child pointer

            //We move parent record to the right node
            rightSiblingNode.SetKey(numberOfMovedRecords - 1, rightParentKey);
            rightSiblingNode.SetRecordAddress(numberOfMovedRecords - 1, rightParentMainFileAddress);

            // We move records form left node to right node
            Array.Copy(this.nodeBytes, 2 * sizeof(long) + 3 * sizeof(long) * (newLeftNodeNumberOfElements + 1), rightSiblingNode.nodeBytes, 2 * sizeof(long),  3 * sizeof(long) * ((this.NumberOfElements + 1) - numberOfMovedRecords) + sizeof(long)); // We also have to move the most right child pointer

            this.NumberOfElements = newLeftNodeNumberOfElements;
            rightSiblingNode.NumberOfElements = newRightNodeNumberOfElements;

            return (newParentKey, newParentMainFileAddress);
        }

        internal void SetUpNewRootNode(long parentNodeNewKey, long parentNodeNewMainFileAddress, long leftNodePageNumber, long rightNodePageNumber)
        {
            NumberOfElements = 1;
            ParentPageNumber = 0;

            SetChildAddress(0, leftNodePageNumber);
            SetChildAddress(1, rightNodePageNumber);
            SetKey(0, parentNodeNewKey);
            SetRecordAddress(0, parentNodeNewMainFileAddress);
        }

        internal void SetParentAddress(long pageNumber)
        {
            ParentPageNumber = pageNumber;
        }

        internal (long parentNodeNewKey, long parentNodeNewMainFileAddress) AddedSplitWith(Node newNode, long key, long mainFileAddress, long? newRightSplittedNodePageNumber)
        {
            this.AddElement(key, mainFileAddress, newRightSplittedNodePageNumber);
            long parentNodeNewKey = GetKey((int)NumberOfElements / 2);
            long parentNodeNewMainFileAddress = GetRecordAddress((int)this.NumberOfElements / 2);
            
            long newNumberOfElements = NumberOfElements / 2;

            Array.Copy(this.nodeBytes, 2*sizeof(long) + (((int)newNumberOfElements) + 1) * 3 * sizeof(long), newNode.nodeBytes, 2 * sizeof(long), newNumberOfElements * 3*sizeof(long) + sizeof(long) );

            this.NumberOfElements = newNumberOfElements;
            newNode.NumberOfElements = newNumberOfElements;

            newNode.ParentPageNumber = this.ParentPageNumber;

            return (parentNodeNewKey, parentNodeNewMainFileAddress);
        }


    }
}
