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

        public long GetRecordAddressForKey(long key)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetKey(i) == key)
                {
                    return GetRecordAddress(i);
                }
            }

            throw new Exception();
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
            int recordIndex = -1;
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetKey(i) == key)
                {
                    recordIndex = i;
                    break;
                }
            }
            if (recordIndex == -1)
                return false;

            Array.Copy(nodeBytes, 2 * sizeof(long) + 3 * sizeof(long) * (recordIndex + 1), nodeBytes, 2 * sizeof(long) + 3 * sizeof(long) * recordIndex, 3 * sizeof(long) * (NumberOfElements - (recordIndex +1)) + sizeof(long)); // We also have to move the most right child pointer

            NumberOfElements--;
            return true;
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
                    return (GetKey(i), GetRecordAddress(i));
                }
            }
            throw new Exception("This page don't have left values to this page");
        }

        internal (long rightParentKey, long rightParentMainFileAddres) GetRightKeyAddressPairToChild(long pageNumber)
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetChildAddress(i) == pageNumber)
                {
                    return (GetKey(i), GetRecordAddress(i));
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

        internal (long newLeftParentKey, long newLeftParentMainFileAddress) EqualizedWithSibling(Node leftSiblingNode, long parentKey, long parentMainFileAddress)
        {
            Node greaterNode;
            Node smallerNode;
            if (leftSiblingNode.GetKey(0) > this.GetKey(0))
            {
                greaterNode = leftSiblingNode;
                smallerNode = this;
            }
            else
            {
                greaterNode = this;
                smallerNode = leftSiblingNode;
            }

            long numberOfElementsOnTwoNodes = greaterNode.NumberOfElements + smallerNode.NumberOfElements;
            long newGreaterNodeNumberOfElements = numberOfElementsOnTwoNodes / 2;
            long newSmallerNodeNumberOfElements = numberOfElementsOnTwoNodes - newGreaterNodeNumberOfElements;
            int numberOfMovedToGreater = (int)(newGreaterNodeNumberOfElements - greaterNode.NumberOfElements);
            int numberOfMovedToSmaller = (int)(newSmallerNodeNumberOfElements - smallerNode.NumberOfElements);

            long newParentKey;
            long newParentMainFileAddress;

            if (numberOfMovedToGreater == 0)
            {
                return (parentKey, parentMainFileAddress);
            }
            else if (numberOfMovedToGreater > 0)
            {
                newParentKey = smallerNode.GetKey((int)newSmallerNodeNumberOfElements);
                newParentMainFileAddress = smallerNode.GetRecordAddress((int)newSmallerNodeNumberOfElements);

                //We make space for moved records
                Array.Copy(greaterNode.nodeBytes, 2 * sizeof(long), greaterNode.nodeBytes, 2 * sizeof(long) + 3 * numberOfMovedToGreater * sizeof(long), 3 * sizeof(long) * (greaterNode.NumberOfElements + 1)); // We also have to move the most right child pointer

                //We move parent record to the right node
                greaterNode.SetKey(0, parentKey);
                greaterNode.SetRecordAddress(0, parentMainFileAddress);

                // We move records form left node to right node
                Array.Copy(smallerNode.nodeBytes, 2 * sizeof(long) + 3 * sizeof(long) * (newSmallerNodeNumberOfElements + 1), greaterNode.nodeBytes, 2 * sizeof(long), 3 * sizeof(long) * (numberOfMovedToGreater - 1) + sizeof(long)); // We also have to move the most right child pointer
            }
            else
            {
                newParentKey = greaterNode.GetKey((int)numberOfMovedToSmaller - 1);
                newParentMainFileAddress = greaterNode.GetRecordAddress((int)numberOfMovedToSmaller - 1);

                //We move parent record to the smaller node
                smallerNode.SetKey((int)smallerNode.NumberOfElements, parentKey);
                smallerNode.SetRecordAddress((int)smallerNode.NumberOfElements, parentMainFileAddress);

                // We move records form right node to left node
                Array.Copy(greaterNode.nodeBytes, 2 * sizeof(long), smallerNode.nodeBytes, 2 * sizeof(long) + (smallerNode.NumberOfElements + 1) * 3 * sizeof(long), 3 * sizeof(long) * (numberOfMovedToSmaller - 1) + sizeof(long)); // We also have to move the most right child pointer

                //We shift to left right node
                Array.Copy(this.nodeBytes, 2 * sizeof(long) + numberOfMovedToSmaller * 3 * sizeof(long), this.nodeBytes, 2 * sizeof(long), 3 * sizeof(long) * (newGreaterNodeNumberOfElements + 1)); // We also have to move the most right child pointer
            }

            smallerNode.NumberOfElements = newSmallerNodeNumberOfElements;
            greaterNode.NumberOfElements = newGreaterNodeNumberOfElements;

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



        internal (long newRightParentKey, long newRightParentMainFileAddress) EqualizedElementAddingWithRightSibling(Node rightSiblingNode, long rightParentKey, long rightParentMainFileAddress)
        {
            long numberOfElementsOnTwoNodes = this.NumberOfElements + rightSiblingNode.NumberOfElements;
            long newRightNodeNumberOfElements = numberOfElementsOnTwoNodes / 2;
            long newLeftNodeNumberOfElements = numberOfElementsOnTwoNodes - newRightNodeNumberOfElements;

            long newParentKey = this.GetKey((int)newLeftNodeNumberOfElements);
            long newParentMainFileAddress = this.GetRecordAddress((int)newLeftNodeNumberOfElements);

            int numberOfMovedRecords = (int)(this.NumberOfElements - newLeftNodeNumberOfElements);

            //We make space for moved records
            Array.Copy(rightSiblingNode.nodeBytes, 2 * sizeof(long), rightSiblingNode.nodeBytes, 2 * sizeof(long) + 3 * numberOfMovedRecords * sizeof(long), 3 * sizeof(long) * (this.NumberOfElements + 1)); // We also have to move the most right child pointer

            //We move parent record to the right node
            rightSiblingNode.SetKey(0, rightParentKey);
            rightSiblingNode.SetRecordAddress(0, rightParentMainFileAddress);

            // We move records form left node to right node
            Array.Copy(this.nodeBytes, 2 * sizeof(long) + 3 * sizeof(long) * (newLeftNodeNumberOfElements + 1), rightSiblingNode.nodeBytes, 2 * sizeof(long),  3 * sizeof(long) * (numberOfMovedRecords - 1) + sizeof(long)); // We also have to move the most right child pointer

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

        internal void MergeWithSibling(Node siblingNode, long parentKey, long parentMainFileAddress)
        {
            Node greaterNode;
            Node smallerNode;
            if (siblingNode.GetKey(0) > this.GetKey(0))
            {
                greaterNode = siblingNode;
                smallerNode = this;
            }
            else
            {
                greaterNode = this;
                smallerNode = siblingNode;
            }

            //We make space in right node for elements from left node
            Array.Copy(greaterNode.nodeBytes, 2 * sizeof(long), greaterNode.nodeBytes, 2 * sizeof(long) + (smallerNode.NumberOfElements + 1) * 3 * sizeof(long), greaterNode.NumberOfElements * 3 * sizeof(long) + sizeof(long));

            //We insert parent element
            greaterNode.SetKey((int)smallerNode.NumberOfElements, parentKey);
            greaterNode.SetRecordAddress((int)smallerNode.NumberOfElements, parentMainFileAddress);

            //We move left node intro right
            Array.Copy(smallerNode.nodeBytes, 2 * sizeof(long), greaterNode.nodeBytes, 2 * sizeof(long), smallerNode.NumberOfElements * 3 * sizeof(long) + sizeof(long));

            greaterNode.NumberOfElements += smallerNode.NumberOfElements + 1;
        }

        internal bool ReplaceRecord(long nodeKey, long repleacedKey, long repleacedRecordAddress)
        {
            int recordIndex = -1;
            for (int i = 0; i < NumberOfElements; i++)
            {
                if (GetKey(i) == nodeKey)
                {
                    recordIndex = i;
                    break;
                }
            }
            if (recordIndex == -1)
                return false;

            SetKey(recordIndex, repleacedKey);
            SetRecordAddress(recordIndex, repleacedRecordAddress);
            return true;
        }
    }
}
