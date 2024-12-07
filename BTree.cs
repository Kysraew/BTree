using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BTree
{
    public class BTree
    {
        //Metadata struct takes whole first page of file
        struct MetaData
        {
            public bool areEmptySpaces;
            public long numberOfEmptySpaces;
            public long numberOfPages;
            public long rootNodePage;
            public long height;

        }

        private List<Node?> _nodesBuffers;
        private FileSystem _fileSystem;
        private MetaData _metaData;
        private MainFile _mainFile;
        public string FilePath;
        public const long d = 2;
        private long minRecordsInNode
        {
            get { return d; }
        }
        private long maxRecordsInNode
        {
            get { return 2 * d; }
        }

        public BTree(string filePath, CreatingMode creatingMode)
        {
            _mainFile = new MainFile(filePath, creatingMode);
            _fileSystem = new FileSystem(filePath + ".bt", creatingMode);
            FilePath = filePath;
            if (creatingMode == CreatingMode.Create)
            {
                _metaData = new MetaData();

                _metaData.areEmptySpaces = false;
                _metaData.numberOfEmptySpaces = 0;
                _metaData.numberOfPages = 0;
                _metaData.height = 0;

                // We create first page for metadata
                _fileSystem.AddPage();
                _metaData.numberOfPages++;

                //We create buffers
                _nodesBuffers = new List<Node?>();
                
                _nodesBuffers.Add(AddNewNode());

                _metaData.rootNodePage = _nodesBuffers[0].pageNumber;
            }
            else
            {
                byte[] metaDataPageBytes = _fileSystem.LoadPage(0);

                _metaData = CastingHelper.CastToStruct<MetaData>(metaDataPageBytes);

                _nodesBuffers = new List<Node?>();

                for (int i = 0; i <= _metaData.height; i++)
                {
                    _nodesBuffers.Add(null);
                }
            }
        }

        public void ReconnectToTape()
        {
            //TODO
            throw new NotImplementedException();
        }

        public void EndTapeConnection()
        {
            foreach (Node? node in _nodesBuffers)
            {
                if (node != null)
                {
                    PersistNode(node);
                }

            }

            byte[] metaDataPageBytes = new byte[Consts.PageSize];

            CastingHelper.CastToArray<MetaData>(_metaData).CopyTo(metaDataPageBytes, 0);

            _fileSystem.SavePage(0, metaDataPageBytes);
            _mainFile.EndTapeConnection();
            _fileSystem.EndConnection();
        }

        private bool LoadPageToNodesBuffer(long pageNumber, long height)
        {
            if (height > _metaData.height || height < 0)
            {
                throw new ArgumentException("Heigh not in the BTree");
            }

            //We check if this page isnt already loaded on other height
            for (int i = 0; i < _nodesBuffers.Count; i++)
            {
                if (_nodesBuffers[i] != null && _nodesBuffers[i].pageNumber == pageNumber && i != height)
                {
                    PersistNode(_nodesBuffers[i]);
                    _nodesBuffers[i] = null;
                }

            }

            // If page is already loaded on this level we dont have to load anything
            if ( _nodesBuffers[(int)height] != null)
            {
                if (_nodesBuffers[(int)height].pageNumber == pageNumber)
                {
                    return true;
                }
                PersistNode(_nodesBuffers[(int)height]);
            }

            //We load page to buffers
            _nodesBuffers[(int)height] = new Node(_fileSystem.LoadPage(pageNumber), pageNumber, this);
            
            return true;
        }


        public bool AddRecord(long key, char[] record)
        {
            if (record.Length >= Consts.RecordLength) // We have to leave place for '\0'
            {
                throw new ArgumentException("Record length is diffrent!");
            }

            long mainFileRecordAddress = _mainFile.AddRecord(record);
            long height = 0;
            long nextPageAddress = _metaData.rootNodePage;

            while (true)
            {
                LoadPageToNodesBuffer(nextPageAddress, height);

                long recordAddress = _nodesBuffers[(int)height].GetMainFileAddressByKey(key); // 0 is a special value meaning not found

                if (recordAddress != 0)
                {
                    return false;
                }
                else if (_nodesBuffers[(int)height].IsLeaf())
                {
                    break;
                }
                else
                {
                    nextPageAddress = _nodesBuffers[(int)height].GetIntervalChildAddressWithKey(key);
                    height++;
                }
            }

            //add record to page

            AddElementToNode(_nodesBuffers[(int)height].pageNumber, key, mainFileRecordAddress, height);


            return true;
        }

        private void UpdateChildsParentReference(long pageAddress, long height)
        {
            LoadPageToNodesBuffer(pageAddress, height);

            long numberOfChilds = _nodesBuffers[(int)height].NumberOfElements + 1;
            long parentPageNumber = _nodesBuffers[(int)height].pageNumber;
            for (int i = 0; i < numberOfChilds; i++)
            {
                LoadPageToNodesBuffer(_nodesBuffers[(int)height].GetChildAddress(i), height + 1);
                _nodesBuffers[(int)height + 1].ParentPageNumber = parentPageNumber;
            }
        }

        private bool SplitNode(long splitingPageNumber, long key, long mainFileAddress, long height, long? newRightSplittedNodePageNumber = null)
        {
            LoadPageToNodesBuffer(splitingPageNumber, height);

            Node NewNode = AddNewNode();
            (long parentNodeNewKey, long parentNodeNewMainFileAddress) = _nodesBuffers[(int)height].AddedSplitWith(NewNode, key, mainFileAddress, newRightSplittedNodePageNumber); // NewNode is right part

            PersistNode(NewNode);

            if (!_nodesBuffers[(int)height].IsLeaf())
            {
                UpdateChildsParentReference(NewNode.pageNumber, height);
                UpdateChildsParentReference(_nodesBuffers[(int)height].pageNumber, height);
                LoadPageToNodesBuffer(splitingPageNumber, height);
            }


            if (height == 0)
            {
                Node NewRootNode = AddNewNode();

                NewRootNode.SetUpNewRootNode(parentNodeNewKey, parentNodeNewMainFileAddress, _nodesBuffers[(int)height].pageNumber, NewNode.pageNumber);
                PersistNode(NewRootNode);

                // We add to childs address of new parent
                NewNode.SetParentAddress(NewRootNode.pageNumber);
                PersistNode(NewNode);

                _nodesBuffers[(int)height].SetParentAddress(NewRootNode.pageNumber);

                _metaData.rootNodePage = NewRootNode.pageNumber;

                _metaData.height++;
                _nodesBuffers.Add(null);

                return true;
            }
            else
            {
                AddElementToNode(_nodesBuffers[(int)height].ParentPageNumber, parentNodeNewKey, parentNodeNewMainFileAddress, height - 1, NewNode.pageNumber);
            }


            return true;
        }

        private bool AddElementToNode(long pageNumber, long nodeKey, long nodeMainFileAddress, long height, long? newRightSplittedNodePageNumber = null)
        {
            LoadPageToNodesBuffer(pageNumber, height);

            if (_nodesBuffers[(int)height].NumberOfElements < maxRecordsInNode)
            {
                _nodesBuffers[(int)height].AddElement(nodeKey, nodeMainFileAddress, newRightSplittedNodePageNumber);
                return true;
            }

            //If there is not enought space first we try to compensate 

            if (height != 0)
            {
                // Parent page
                LoadPageToNodesBuffer(_nodesBuffers[(int)height].ParentPageNumber, height - 1);

                //First we try to compensate from left side
                long leftSiblingPageNumber = _nodesBuffers[(int)height - 1].GetLeftChildAddressFrom(_nodesBuffers[(int)height].pageNumber); // 0 is special value when not found
                if (leftSiblingPageNumber != 0)
                {
                    Node LeftSiblingNode = new Node(_fileSystem.LoadPage(leftSiblingPageNumber), leftSiblingPageNumber, this);
                    (long leftParentKey, long leftParentMainFileAddress) = _nodesBuffers[(int)height - 1].GetLeftKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber);

                    if (LeftSiblingNode.NumberOfElements < maxRecordsInNode)
                    {
                        _nodesBuffers[(int)height].AddElement(nodeKey, nodeMainFileAddress, newRightSplittedNodePageNumber);
                        (long newLeftParentKey, long newLeftParentMainFileAddress) = _nodesBuffers[(int)height].EqualizedWithSibling(LeftSiblingNode, leftParentKey, leftParentMainFileAddress);

                        _nodesBuffers[(int)height - 1].SetLeftKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber, newLeftParentKey, newLeftParentMainFileAddress);

                        PersistNode(LeftSiblingNode);

                        return true;
                    }
                }

                //If we can't we try to compensate form right
                long rightSiblingPageNumber = _nodesBuffers[(int)height - 1].GetRightChildAddressFrom(_nodesBuffers[(int)height].pageNumber); // 0 is special value when not found
                if (rightSiblingPageNumber != 0)
                {
                    Node RightSiblingNode = new Node(_fileSystem.LoadPage(rightSiblingPageNumber), rightSiblingPageNumber, this);
                    (long rightParentKey, long rightParentMainFileAddress) = _nodesBuffers[(int)height - 1].GetRightKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber);

                    if (RightSiblingNode.NumberOfElements < maxRecordsInNode)
                    {
                        _nodesBuffers[(int)height].AddElement(nodeKey, nodeMainFileAddress, newRightSplittedNodePageNumber);
                        (long newRightParentKey, long newRightParentMainFileAddress) = _nodesBuffers[(int)height].EqualizedWithSibling(RightSiblingNode, rightParentKey, rightParentMainFileAddress);

                        _nodesBuffers[(int)height - 1].SetRightKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber, newRightParentKey, newRightParentMainFileAddress);

                        PersistNode(RightSiblingNode);

                        return true;
                    }
                }
            }
            return SplitNode(pageNumber, nodeKey, nodeMainFileAddress, height, newRightSplittedNodePageNumber);

        }

        public char[]? GetRecord(long key)
        {

            long height = 0;

            long nextPageAddress = _metaData.rootNodePage;
            while (true)
            {
                LoadPageToNodesBuffer(nextPageAddress, height);

                long recordAddress = _nodesBuffers[(int)height].GetMainFileAddressByKey(key); // 0 is a special value meaning not found

                if (recordAddress != 0)
                {
                    return _mainFile.GetRecord(recordAddress);
                }
                else if (_nodesBuffers[(int)height].IsLeaf())
                {
                    return null;
                }
                else
                {
                    nextPageAddress = _nodesBuffers[(int)height].GetIntervalChildAddressWithKey(key);
                    height++;
                }
            }
        }


        private void LoadNodeBy()
        {

        }

        private Node AddNewNode()
        {

            if (_metaData.areEmptySpaces)
            {
                byte[] pageWithFreeAddresses = _fileSystem.LoadPage(_metaData.numberOfPages - 1);
                int addressBufferIndex = (int)(((_metaData.numberOfEmptySpaces - 1) * sizeof(long)) % Consts.PageSize);

                long pageAddress = BitConverter.ToInt64(pageWithFreeAddresses, addressBufferIndex);

                if (addressBufferIndex == 0)
                {
                    _fileSystem.DeleteLastPage();
                    _metaData.numberOfPages--;
                }
                _metaData.numberOfEmptySpaces--;
                _metaData.areEmptySpaces = (_metaData.numberOfEmptySpaces > 0) ? true : false;

                return new Node(new byte[Consts.PageSize], pageAddress, this);
            }
            else
            {
                _fileSystem.AddPage();
                return new Node(new byte[Consts.PageSize], _metaData.numberOfPages++, this);
            }
        }

        private bool DeleteNode(long pageNumber)
        {
            int addressBufferIndex = (int)(((_metaData.numberOfEmptySpaces) * sizeof(long)) % Consts.PageSize);

            if (addressBufferIndex == 0)
            {
                _fileSystem.AddPage();
                _metaData.numberOfPages++;
            }

            byte[] pageWithFreeAddresses = _fileSystem.LoadPage(_metaData.numberOfPages - 1);

            BitConverter.GetBytes(pageNumber).CopyTo(pageWithFreeAddresses, addressBufferIndex);

            _metaData.numberOfEmptySpaces++;
            _metaData.areEmptySpaces = true;

            _fileSystem.SavePage(_metaData.numberOfPages - 1, pageWithFreeAddresses);
            return true;
        }

        public bool DeleteRecord(long key)
        {
            long height = 0;
            long nextPageAddress = _metaData.rootNodePage;

            while (true)
            {
                LoadPageToNodesBuffer(nextPageAddress, height);

                long recordAddress = _nodesBuffers[(int)height].GetMainFileAddressByKey(key); // 0 is a special value meaning not found

                if (recordAddress != 0)
                {
                    break;
                }
                else if (_nodesBuffers[(int)height].IsLeaf())
                {
                   return false;
                }
                else
                {
                    nextPageAddress = _nodesBuffers[(int)height].GetIntervalChildAddressWithKey(key);
                    height++;
                }
            }

            _mainFile.DeleteRecord(_nodesBuffers[(int)height].GetRecordAddressForKey(key));
            if (!_nodesBuffers[(int)height].IsLeaf())
            {
                (long newPageNumber, long newHeight) = ReplaceWithClosestRight(key, height, nextPageAddress);
               // pageNumber = newPageNumber;
                height = newHeight;
                LoadPageToNodesBuffer(newPageNumber, height);

            }
            _nodesBuffers[(int)height].DeleteElement(key);

            DeleteElementCleanUp(_nodesBuffers[(int)height].pageNumber, height);
            return true;
        }

        private (long newPageNumber, long newHeight) ReplaceWithClosestRight(long nodeKey, long height, long pageNumber)
        {
            long nextPageAddress = pageNumber;
            long startHeight = height;

            while (true)
            {
                LoadPageToNodesBuffer(nextPageAddress, height);

                if (_nodesBuffers[(int)height].IsLeaf())
                {
                    break;
                }
                else
                {
                    nextPageAddress = _nodesBuffers[(int)height].GetIntervalChildAddressWithKey(nodeKey);
                    height++;
                }
            }

            long repleacedKey = _nodesBuffers[(int)height].GetKey(0);
            long repleacedRecordAddress = _nodesBuffers[(int)height].GetRecordAddress(0);

            _nodesBuffers[(int)height].ReplaceRecord(repleacedKey, nodeKey, _nodesBuffers[(int)startHeight].GetMainFileAddressByKey(nodeKey));

            _nodesBuffers[(int)startHeight].ReplaceRecord(nodeKey, repleacedKey, repleacedRecordAddress);

            return (nextPageAddress, height);
        }
        private bool MergeNode(long pageNumber, long height)
        {
            LoadPageToNodesBuffer(pageNumber, height);
            LoadPageToNodesBuffer(_nodesBuffers[(int)height].ParentPageNumber, height - 1);

            long leftSiblingPageNumber = _nodesBuffers[(int)height - 1].GetLeftChildAddressFrom(_nodesBuffers[(int)height].pageNumber); // 0 is special value when not found
            Node LeftSiblingNode = new Node(_fileSystem.LoadPage(leftSiblingPageNumber), leftSiblingPageNumber, this);

            if (LeftSiblingNode.NumberOfElements == minRecordsInNode)
            {
                (long leftParentKey, long leftParentMainFileAddress) = _nodesBuffers[(int)height - 1].GetLeftKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber);

                _nodesBuffers[(int)height].MergeWithSibling(LeftSiblingNode, leftParentKey, leftParentMainFileAddress);

                DeleteNode(LeftSiblingNode.pageNumber);

                _nodesBuffers[(int)height - 1].DeleteElement(leftParentKey);

                //PersistNode(_nodesBuffers[(int)height]);
            }
            else
            {
                long rightSiblingPageNumber = _nodesBuffers[(int)height - 1].GetLeftChildAddressFrom(_nodesBuffers[(int)height].pageNumber); // 0 is special value when not found
                Node RightSiblingNode = new Node(_fileSystem.LoadPage(rightSiblingPageNumber), rightSiblingPageNumber, this);

                (long rightParentKey, long rightParentMainFileAddress) = _nodesBuffers[(int)height - 1].GetLeftKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber);

                _nodesBuffers[(int)height].MergeWithSibling(LeftSiblingNode, rightParentKey, rightParentMainFileAddress);

                DeleteNode(_nodesBuffers[(int)height].pageNumber);

                _nodesBuffers[(int)height - 1].DeleteElement(rightParentKey);

            }

            if (height - 1 == 0 && _nodesBuffers[(int)height - 1].NumberOfElements == 0)
            {
                _metaData.height--;
                _metaData.rootNodePage = _nodesBuffers[(int)height - 1].GetChildAddress(0);
                _nodesBuffers[(int)height - 1].ParentPageNumber = 0;
                DeleteNode(_nodesBuffers[(int)height - 1].pageNumber);
            }
            else if ( height - 1 != 0 &&_nodesBuffers[(int)height - 1].NumberOfElements < minRecordsInNode)
            {
                DeleteElementCleanUp(_nodesBuffers[(int)height - 1].pageNumber, height - 1);
            }
            return true;
        }
        public bool DeleteElementCleanUp(long pageNumber, long height)
        {
            LoadPageToNodesBuffer(pageNumber, height);


            if (_nodesBuffers[(int)height].NumberOfElements >= minRecordsInNode || height == 0)
            {
                return true;
            }

            if (height != 0)
            {
                // Parent page
                LoadPageToNodesBuffer(_nodesBuffers[(int)height].ParentPageNumber, height - 1);

                //First we try to compensate from left side
                long leftSiblingPageNumber = _nodesBuffers[(int)height - 1].GetLeftChildAddressFrom(_nodesBuffers[(int)height].pageNumber); // 0 is special value when not found
                if (leftSiblingPageNumber != 0)
                {
                    Node LeftSiblingNode = new Node(_fileSystem.LoadPage(leftSiblingPageNumber), leftSiblingPageNumber, this);
                    (long leftParentKey, long leftParentMainFileAddress) = _nodesBuffers[(int)height - 1].GetLeftKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber);

                    if (LeftSiblingNode.NumberOfElements > minRecordsInNode)
                    {
                        (long newLeftParentKey, long newLeftParentMainFileAddress) = _nodesBuffers[(int)height].EqualizedWithSibling(LeftSiblingNode, leftParentKey, leftParentMainFileAddress);

                        _nodesBuffers[(int)height - 1].SetLeftKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber, newLeftParentKey, newLeftParentMainFileAddress);

                        PersistNode(LeftSiblingNode);

                        return true;
                    }
                }

                //If we can't we try to compensate form right
                long rightSiblingPageNumber = _nodesBuffers[(int)height - 1].GetRightChildAddressFrom(_nodesBuffers[(int)height].pageNumber); // 0 is special value when not found
                if (rightSiblingPageNumber != 0)
                {
                    Node RightSiblingNode = new Node(_fileSystem.LoadPage(rightSiblingPageNumber), rightSiblingPageNumber, this);
                    (long rightParentKey, long rightParentMainFileAddress) = _nodesBuffers[(int)height - 1].GetRightKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber);

                    if (RightSiblingNode.NumberOfElements > minRecordsInNode)
                    {
                        (long newRightParentKey, long newRightParentMainFileAddress) = _nodesBuffers[(int)height].EqualizedWithSibling(RightSiblingNode, rightParentKey, rightParentMainFileAddress);

                        _nodesBuffers[(int)height - 1].SetRightKeyAddressPairToChild(_nodesBuffers[(int)height].pageNumber, newRightParentKey, newRightParentMainFileAddress);

                        PersistNode(RightSiblingNode);

                        return true;
                    }
                }
            }

            return MergeNode(pageNumber, height);
        }

        public void PrintAllRecords()
        {
            Console.WriteLine("\n\n\n\n");
            PrintAllRecordsFrom(_metaData.rootNodePage, 0);
        }

        public void PrintAllInfo()
        {
            Console.WriteLine("\n\n\n\n");
            PrintAllInfoFrom(_metaData.rootNodePage, 0);
        }

        public void PrintAllRecordsFrom(long dataPageNumber, long height)
        {
            LoadPageToNodesBuffer(dataPageNumber, height);


            for (int i = 0; i < _nodesBuffers[(int)height].NumberOfElements; i++)
            {
                if (height < _metaData.height) // means that it is leaf
                {
                    PrintAllRecordsFrom(_nodesBuffers[(int)height].GetChildAddress(i), height + 1);
                }
                Console.WriteLine($"K: {_nodesBuffers[(int)height].GetKey(i)} V: {new string(_mainFile.GetRecord(_nodesBuffers[(int)height].GetRecordAddress(i)))}");
            }
            if (height < _metaData.height) // means that it is leaf
            {
                PrintAllRecordsFrom(_nodesBuffers[(int)height].GetChildAddress((int)_nodesBuffers[(int)height].NumberOfElements), height + 1);
            }
        }

        public void PrintAllInfoFrom(long dataPageNumber, long height)
        {
            LoadPageToNodesBuffer(dataPageNumber, height);

            for (int i = 0; i < _nodesBuffers[(int)height].NumberOfElements; i++)
            {
                if (height < _metaData.height) // means that it is leaf
                {
                    PrintAllInfoFrom(_nodesBuffers[(int)height].GetChildAddress(i), height + 1);
                    Console.WriteLine($"{new string('\t', (int)height * 4 + 1)} /--Ch:{_nodesBuffers[(int)height].GetChildAddress(i)}---");
                }
                Console.WriteLine($"{new string('\t', (int)height * 4)}[K: {_nodesBuffers[(int)height].GetKey(i)} N:{_nodesBuffers[(int)height].NumberOfElements} Par:{_nodesBuffers[(int)height].ParentPageNumber} This: {_nodesBuffers[(int)height].pageNumber}]"); //V: { new string(_mainFile.GetRecord(_nodesBuffers[(int)height].GetRecordAddress(i)))}
            }

            if (height < _metaData.height) // means that it is leaf
            {
                Console.WriteLine($"{new string('\t', (int)height * 4 + 1)} \\--Ch:{_nodesBuffers[(int)height].GetChildAddress((int)_nodesBuffers[(int)height].NumberOfElements)}---");
                PrintAllInfoFrom(_nodesBuffers[(int)height].GetChildAddress((int)_nodesBuffers[(int)height].NumberOfElements), height + 1);
            }
        }

        private bool DeleteNode(Node node)
        {
            throw new NotImplementedException();
        }

        public bool PersistNode(Node Node)
        {
            _fileSystem.SavePage(Node.pageNumber, Node.nodeBytes);
            return true;
        }
    }
}
