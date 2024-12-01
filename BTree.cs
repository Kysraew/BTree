using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            public long height;
        }

        private List<Node?> _nodes; 
        private FileSystem _fileSystem;
        private MetaData _metaData;
        private MainFile _mainFile;

        public BTree(string filePath, CreatingMode creatingMode)
        {
            _mainFile = new MainFile(filePath, creatingMode);
            _fileSystem = new FileSystem(filePath + ".bt", creatingMode);

            if (creatingMode == CreatingMode.Create)
            {


                _metaData = new MetaData();

                _metaData.areEmptySpaces = false;
                _metaData.numberOfEmptySpaces = 0;
                _metaData.numberOfPages = 1;
                _metaData.height = 0;

                // We create first page for metadata
                _fileSystem.AddPage();

                _nodes = new List<Node?>();
            }
            else
            {
                byte[] metaDataPageBytes = _fileSystem.LoadPage(0);

                _metaData = CastingHelper.CastToStruct<MetaData>(metaDataPageBytes);

                _nodes = new List<Node?>();

                for (int i = 0; i <= _metaData.height; i++)
                {
                    _nodes.Add(null);
                }
            }
        }

        public void EndTapeConnection()
        {
            foreach (Node? node in _nodes)
            {
                if (node != null)
                {
                    node.Persist();
                }

            }

            byte[] metaDataPageBytes = new byte[Consts.PageSize];

            CastingHelper.CastToArray<MetaData>(_metaData).CopyTo(metaDataPageBytes, 0);

            _fileSystem.SavePage(0, metaDataPageBytes);
        }




        public AddRecord(long)
        {

        }


        public char[]? GetRecord(long key)
        {
            throw new NotImplementedException();
        }

        public void ReconnectToTape()
        {
            //TODO
            throw new NotImplementedException();
        }
    }
}
