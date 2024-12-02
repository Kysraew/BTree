using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BTree
{

    public class MainFile
    {
        //Metadata struct takes whole first page of file
        struct MetaData
        {
            public bool areEmptySpaces;
            public long numberOfEmptySpaces;
            public long fileLengh;
        }

        private const int _recordLenght = 30;
        private FileSystem fileSystem;
        private long _pageSize;                 //page size have to be devidable by sizeof(long) because of emptySapcesTablePointer 
        private MetaData _metaData;
        private byte[] _buffor;
        private long _loadedPageNumber; //use to determin which page is loaded in the _buffer

        public MainFile(string filePath, CreatingMode creatingMode)
        {
            _pageSize = FileSystem.PageSize;
            _buffor = new byte[_pageSize];
            _loadedPageNumber = 0;

            fileSystem = new FileSystem(filePath+".mf", creatingMode);
            
            if (creatingMode == CreatingMode.Create)
            {
                _metaData = new MetaData();
                _metaData.areEmptySpaces = false;
                _metaData.numberOfEmptySpaces = 0;
                _metaData.fileLengh = _pageSize;

                // We create first page for metadata
                fileSystem.AddPage(_buffor);
            }
            else
            {
                _buffor = fileSystem.LoadPage(0);

                _metaData = CastingHelper.CastToStruct<MetaData>(_buffor);
            }

        }

        public void EndTapeConnection()
        {
            fileSystem.SavePage(_loadedPageNumber, _buffor);

            _buffor = new byte[_pageSize];
            _loadedPageNumber = 0;

            byte[] metaDataBytes = CastingHelper.CastToArray<MetaData>(_metaData);
            metaDataBytes.CopyTo(_buffor, 0);

            fileSystem.SavePage(0, _buffor);
        }

        public void ReconnectToTape()
        {
            //TODO
            throw new NotImplementedException();
        }

        private long CalculateLastRecordAddress()
        {
            if (_metaData.areEmptySpaces)
            {
                return _metaData.fileLengh - _metaData.numberOfEmptySpaces * sizeof(long) - _recordLenght;
            }
            else
            {
                return _metaData.fileLengh - _recordLenght;
            }
        }

        private bool IsRecordAddressValid(long address)
        {
            if (!(address <= CalculateLastRecordAddress())
               || address < 1 * _pageSize // We cant read from metadata page
               || address % _pageSize + _recordLenght > _pageSize // Records are only on one page
               || (address % _pageSize) % _recordLenght != 0) // Records are on specified locations on pages
            {
                return false;
            }
            return true;
        }
        public bool DeleteRecord(long address)
        {

            if (!IsRecordAddressValid(address))
            {
                throw new ArgumentException("There is no record under this address!");
            }

            //if we have to create new page
            if (GetNumberOfPages(_metaData.fileLengh) != GetNumberOfPages(_metaData.fileLengh + sizeof(long)))
            {
                fileSystem.SavePage(_loadedPageNumber, _buffor);
                _buffor = new byte[_pageSize];
                byte[] addressBytes =  BitConverter.GetBytes(address);
                addressBytes.CopyTo(_buffor, 0);

                fileSystem.AddPage(_buffor);
                _loadedPageNumber = GetNumberOfPages(_metaData.fileLengh) + 1;
            }
            else
            {
                LoadPageWithAddress(_metaData.fileLengh);
                byte[] addressBytes = BitConverter.GetBytes(address);
                addressBytes.CopyTo(_buffor, GetBufferIndexByAddress(_metaData.fileLengh));
            }

            _metaData.areEmptySpaces = true;
            _metaData.numberOfEmptySpaces++;
            _metaData.fileLengh += sizeof(byte);

            return true;
        }


        private long PopLastEmptySpaceAddress()
        {
            if (_metaData.numberOfEmptySpaces == 0)
            {
                throw new Exception("There are not empty spaces!");
            }

            long emptySpaceAddress = 0;

            LoadPageWithAddress(_metaData.fileLengh - sizeof(long));

            emptySpaceAddress = BitConverter.ToInt64(_buffor, (int)(GetBufferIndexByAddress(_metaData.fileLengh - sizeof(long))));

            if (GetBufferIndexByAddress(emptySpaceAddress) == 0) // If address is only thing on the last page we delete this page
            {
                fileSystem.DeleteLastPage();
            }
            
            _metaData.fileLengh -= sizeof(long);
            _metaData.numberOfEmptySpaces--;
            _metaData.areEmptySpaces = (_metaData.numberOfEmptySpaces > 0) ? true : false;

            return emptySpaceAddress;
        }

        public long AddRecord(char[] record)
        {

            if (record.Length + 1 > _recordLenght) // We have to add null at the end of array
            {
                throw new ArgumentException("Record lenght is too big!");
            }
            if (record == null)
            {
                throw new ArgumentException("Record is null!");
            }

            long newRecordAddress = 0;

            if (_metaData.areEmptySpaces)
            {
                newRecordAddress = PopLastEmptySpaceAddress();

                LoadPageWithAddress(newRecordAddress);

                Encoding.ASCII.GetBytes(record, 0, (int)record.Length, _buffor, (int)(GetBufferIndexByAddress(newRecordAddress)));
                _buffor[(GetBufferIndexByAddress(newRecordAddress)) + record.Length] = (byte)'\0';
            }
            else
            {
                if (_metaData.fileLengh + _recordLenght > GetNumberOfPages(_metaData.fileLengh) * _pageSize)
                {
                    fileSystem.SavePage(_loadedPageNumber, _buffor);
                    _buffor = new byte[_pageSize];
                    Encoding.ASCII.GetBytes(record, 0, (int)record.Length, _buffor, 0);
                    _buffor[record.Length] = (byte)'\0';

                    fileSystem.AddPage();

                    newRecordAddress = GetNumberOfPages(_metaData.fileLengh) * _pageSize;

                    _loadedPageNumber = GetPageNumberByAddress(newRecordAddress);

                    _metaData.fileLengh = newRecordAddress + _recordLenght;
                }
                else
                {
                    LoadPageWithAddress(_metaData.fileLengh);

                    Encoding.ASCII.GetBytes(record, 0, (int)record.Length, _buffor, (int)(GetBufferIndexByAddress(_metaData.fileLengh)));
                    _buffor[(GetBufferIndexByAddress(_metaData.fileLengh)) + record.Length] = (byte)'\0';

                    _metaData.fileLengh += _recordLenght;

                    newRecordAddress = _metaData.fileLengh - _recordLenght;
                }
            }

            return newRecordAddress;
        }
        private long GetBufferIndexByAddress(long address)
        {
            return address % _pageSize;
        }
        private long GetPageNumberByAddress(long address)
        {
            return address / _pageSize;
        }
        private long GetNumberOfPages(long fileLength)
        {
            return GetPageNumberByAddress(_metaData.fileLengh - 1) + 1;
        }

        public char[] GetRecord(long address)
        {
            LoadPageWithAddress(address);

            long bufforStartingIndex = GetBufferIndexByAddress(address);

            char[] record = new char[_recordLenght];
            Encoding.ASCII.GetChars(_buffor, (int)bufforStartingIndex, _recordLenght, record, 0);

            for (int i = 0; i < _recordLenght; i++)
            {
                if (_buffor[bufforStartingIndex + i] == '\0')
                {
                    return record.Take(i).ToArray();
                }
            }

            throw new Exception("File format error!");
        }


        private void LoadPageWithAddress(long addressPosition)
        {
            if (addressPosition < 0 || addressPosition > _metaData.fileLengh)
            {
                throw new Exception("File doesn't have this address");
            }

            if (GetPageNumberByAddress(addressPosition) != _loadedPageNumber)
            {
                fileSystem.SavePage(_loadedPageNumber, _buffor);
                _buffor = fileSystem.LoadPageByAddress(addressPosition);
                _loadedPageNumber = GetPageNumberByAddress(addressPosition);
            }
        }
    }
}
