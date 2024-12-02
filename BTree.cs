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
        }

        private bool LoadPageToNodesBuffer(long pageNumber, long height)
        {
            if (height > _metaData.height || height < 0)
            {
                throw new ArgumentException("Heigh not in the BTree");
            }

            if ( _nodesBuffers[(int)height] != null)
            {
                if (_nodesBuffers[(int)height].pageNumber == pageNumber)
                {
                    return true;
                }
                PersistNode(_nodesBuffers[(int)height]);
            }

            _nodesBuffers[(int)height] = new Node(_fileSystem.LoadPage(pageNumber), pageNumber, this);
            
            return true;
        }

        //private long LoadNodeByKey() // return height Node will be loaded to the _nodesBuffers[height]
        //{

        //}

        public bool AddRecord(long key, char[] record)
        {
            if (record.Length != Consts.RecordLength)
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

        

        private bool SplitNode(long splitingPageNumber, long key, long mainFileAddress, long height, long? newRightSplittedNodePageNumber = null)
        {
            LoadPageToNodesBuffer(splitingPageNumber, height);

            Node NewNode = AddNewNode();
            (long parentNodeNewKey, long parentNodeNewMainFileAddress) = _nodesBuffers[(int)height].SplitWith(NewNode, key, mainFileAddress, newRightSplittedNodePageNumber); // NewNode is right part

            PersistNode(NewNode);


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

            Node? AddingNode = _nodesBuffers[(int)height];

            if (AddingNode == null)
                throw new Exception();

            if (AddingNode.NumberOfElements < maxRecordsInNode)
            {
                AddingNode.AddElement(nodeKey, nodeMainFileAddress, newRightSplittedNodePageNumber);
                return true;
            }


            //If there is not enought space first we try to compensate 

            if (height != 0)
            {
                // Parent page
                LoadPageToNodesBuffer(AddingNode.ParentPageNumber, height - 1);

                //First we try to compensate from left side

                long leftSiblingPageNumber = _nodesBuffers[(int)height - 1].GetLeftChildAddressFrom(AddingNode.pageNumber); // 0 is special value when not found
                if (leftSiblingPageNumber != 0)
                {
                    Node LeftSiblingNode = new Node(_fileSystem.LoadPage(leftSiblingPageNumber), leftSiblingPageNumber, this);
                    (long leftParentKey, long leftParentMainFileAddress) = _nodesBuffers[(int)height - 1].GetLeftKeyAddressPairToChild(AddingNode.pageNumber);

                    if (LeftSiblingNode.NumberOfElements < maxRecordsInNode)
                    {
                        (long newLeftParentKey, long newLeftParentMainFileAddress) = AddingNode.EqualizedElementAddingWithLeftSibling(LeftSiblingNode, leftParentKey, leftParentMainFileAddress, nodeKey, nodeMainFileAddress, newRightSplittedNodePageNumber);

                        _nodesBuffers[(int)height - 1].SetLeftKeyAddressPairToChild(AddingNode.pageNumber, newLeftParentKey, newLeftParentMainFileAddress);

                        PersistNode(LeftSiblingNode);

                        return true;
                    }
                }

                //If we can't we try to compensate form right

                long rightSiblingPageNumber = _nodesBuffers[(int)height - 1].GetRightChildAddressFrom(AddingNode.pageNumber); // 0 is special value when not found
                if (rightSiblingPageNumber != 0)
                {
                    Node RightSiblingNode = new Node(_fileSystem.LoadPage(rightSiblingPageNumber), rightSiblingPageNumber, this);
                    (long rightParentKey, long rightParentMainFileAddress) = _nodesBuffers[(int)height - 1].GetRightKeyAddressPairToChild(AddingNode.pageNumber);

                    if (RightSiblingNode.NumberOfElements < maxRecordsInNode)
                    {
                        (long newRightParentKey, long newRightParentMainFileAddress) = AddingNode.EqualizedElementAddingWithRightSibling(RightSiblingNode, rightParentKey, rightParentMainFileAddress, nodeKey, nodeMainFileAddress, newRightSplittedNodePageNumber);

                        _nodesBuffers[(int)height - 1].SetLeftKeyAddressPairToChild(AddingNode.pageNumber, newRightParentKey, newRightParentMainFileAddress);

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

            }
            else
            {

            }
            throw new NotImplementedException();
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
