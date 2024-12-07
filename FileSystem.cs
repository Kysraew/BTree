using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTree
{
    public enum CreatingMode
    {
        Open,
        Create
    }

    public class FileSystem
    {
        private FileStream _fileStream;
        public string filePath;
        private long _numberOfPages;
        public const long PageSize = 600;

        public FileSystem(string filePath, CreatingMode creatingMode) 
        {
            this.filePath = filePath;

            if (creatingMode == CreatingMode.Create)
            {
                _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
                _numberOfPages = 0;
            }
            else
            {
                _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
                _numberOfPages = (_fileStream.Length + PageSize - 1)  / PageSize;
            }
        }
        public byte[] LoadPage(long pageNumber) //Pages are numered from 0
        {
            if (pageNumber < 0 || pageNumber > _numberOfPages)
            {
                throw new ArgumentOutOfRangeException("Page with this number doesn't exist!");
            }

            _fileStream.Position = pageNumber * PageSize;
            byte[] pageBytes = new byte[PageSize];
            _fileStream.Read(pageBytes);

            return pageBytes;
        }
        public byte[] LoadPageByAddress(long address)
        {
            return LoadPage(address / PageSize);
        }

        public bool SavePage(long pageNumber, byte[] pageBytes)
        {
            if (pageBytes.Length != PageSize)
            {
                throw new ArgumentOutOfRangeException("Page have to have FileSystem page size");
            }
            if (pageNumber < 0 || pageNumber > _numberOfPages)
            {
                throw new ArgumentOutOfRangeException("Page with this number doesn't exist!");
            }

            _fileStream.Position = pageNumber * PageSize;
            _fileStream.Write(pageBytes);
            _fileStream.Flush();

            return true;
        }

        public bool AddPage(byte[]? pageBytes = null)
        {
            if (pageBytes != null && pageBytes.Length != PageSize)
            {
                throw new ArgumentOutOfRangeException("Page have to have FileSystem page size");
            }

            if (pageBytes == null)
            {
                pageBytes = new byte[PageSize];
            }

            _fileStream.Position = _numberOfPages * PageSize;
            _fileStream.Write(pageBytes);
            _fileStream.Flush();
            _numberOfPages++;

            return true;
        }

        public bool DeleteLastPage()
        {
            if (_numberOfPages == 0)
            {
                throw new Exception("There is no pages to delete");
            }

            _numberOfPages--;
            _fileStream.SetLength(_numberOfPages * PageSize);
            _fileStream.Flush();

            return true;
        }

        public void EndConnection()
        {
            _fileStream.Close();
        }
    }
}
