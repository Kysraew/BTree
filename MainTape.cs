using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

    public class MainTape
    {
        //Metadata struct takes whole first page of file
        struct MetaData
        {
            public bool areEmptySpaces;
            public long numberOfEmptySpaces;
            public long emptySpacesTablePointer; //useless, can be calculated with numberOfEmptySpaces and fileLength
            public long fileLengh;
        }

        public string FileName { get; private set; }
        private FileStream _fileStream;
        private const long _pageSize = 600; //page size have to be devidable by sizeof(long) because of emptySapcesTablePointer 
        private const int _recordLenght = 30;
        private MetaData _metaData;
        private byte[] _buffor;
        private long _actualPosition; //use to determin which page is loaded in the _buffer

        public MainTape(string filePath, bool createNewFile)
        {
            FileName = filePath;
            _buffor = new byte[_pageSize];

            if (createNewFile)
            {
                _fileStream = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite);

                _metaData = new MetaData();
                _metaData.areEmptySpaces = false;
                _metaData.numberOfEmptySpaces = 0;
                _metaData.emptySpacesTablePointer = 0;
                _metaData.fileLengh = _pageSize;

                // We create first page for metadata
                _fileStream.Position = 0;
                _fileStream.Write(_buffor);
                _fileStream.Flush();

            }
            else
            {
                _fileStream = new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite);
                _fileStream.Position = 0;
                _fileStream.Read(_buffor);
                DeserializeMetaData(_buffor);

                _actualPosition = 0;
            }

        }

        public void EndTapeConnection()
        {
            _fileStream.Position = _actualPosition;
            _fileStream.Write(_buffor);
            _fileStream.Flush();

            _fileStream.Position = 0;
            _fileStream.Read(_buffor);

            _fileStream.Position = 0;
            byte[] metaDataBytes =  SerializeMetaData(_metaData);
            metaDataBytes.CopyTo(_buffor, 0);
            _fileStream.Write(_buffor);
            _fileStream.Flush();
            
            _fileStream.Close();
        }

        public void ReconnectToTape()
        {
            //TODO
            throw new NotImplementedException();
        }
        private byte[] SerializeMetaData(MetaData metaData)
        {
            byte[] metaDataBytes = new byte[0 + 1 * sizeof(bool) + 3 * sizeof(long)];
            BitConverter.GetBytes(metaData.areEmptySpaces).CopyTo(metaDataBytes, 0);
            BitConverter.GetBytes(metaData.numberOfEmptySpaces).CopyTo(metaDataBytes, 0 + 1 * sizeof(bool));
            BitConverter.GetBytes(metaData.emptySpacesTablePointer).CopyTo(metaDataBytes, 0 + 1 * sizeof(bool) + 1 * sizeof(long));
            BitConverter.GetBytes(metaData.fileLengh).CopyTo(metaDataBytes, 0 + 1 * sizeof(bool) + 2 * sizeof(long));

            return metaDataBytes;
        }
        private MetaData DeserializeMetaData(byte[] byteArray)
        {
            if (byteArray.Length < (0 + 1 * sizeof(bool) + 2 * sizeof(long))) // lenght of all MetaData params
            {
                throw new Exception("Byte array is too short!");
            }

            MetaData metaData = new MetaData();
            metaData.areEmptySpaces = BitConverter.ToBoolean(byteArray, 0);
            metaData.numberOfEmptySpaces = BitConverter.ToInt64(byteArray, 0 + 1 * sizeof(bool));
            metaData.emptySpacesTablePointer = BitConverter.ToInt64(byteArray, 0 + 1 * sizeof(bool) + 1 * sizeof(long));
            metaData.fileLengh = BitConverter.ToInt64(byteArray, 0 + 1 * sizeof(bool) + 2 * sizeof(long));

            return metaData;
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
            if ((_metaData.fileLengh % _pageSize) + sizeof(long) <= _pageSize)
            {
                long newPageAddress = CalculatePageAddressByAddress(_metaData.fileLengh) + _pageSize;
                _fileStream.Position = newPageAddress;
                _fileStream.Write(new byte[_pageSize]);
                _fileStream.Flush();

                LoadPageWithAddress(newPageAddress);

                BitConverter.GetBytes(address).CopyTo(_buffor, 0);

            }
            else
            {
                LoadPageWithAddress(address);
                BitConverter.GetBytes(address).CopyTo(_buffor, _metaData.fileLengh % _pageSize);

            }

            _metaData.fileLengh += sizeof(long);
            _metaData.numberOfEmptySpaces += 1;
            _metaData.areEmptySpaces = true;

            return true;

        }
        private long GetFreeSpaceAddress(long address)
        {
            LoadPageWithAddress(address);

            return BitConverter.ToInt64(_buffor, (int)(address % _pageSize));

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


            if (_metaData.areEmptySpaces)
            {   
                long freeSpaceAddress = GetFreeSpaceAddress(_metaData.fileLengh - sizeof(long));

                LoadPageWithAddress(freeSpaceAddress);
                Encoding.ASCII.GetBytes(record, 0, (int)record.Length, _buffor, (int)(freeSpaceAddress % _pageSize));
                _buffor[(freeSpaceAddress % _pageSize) + record.Length] = (byte)'\0';

                _metaData.numberOfEmptySpaces--;
                if (_metaData.numberOfEmptySpaces <= 0)
                {
                    _metaData.areEmptySpaces = false;
                }

                // if freeSpaceAddress was the only thing on the page we delete it
                if (CalculatePageAddressByAddress(_metaData.fileLengh) != CalculatePageAddressByAddress(_metaData.fileLengh - sizeof(long)))
                {
                    _fileStream.SetLength(CalculatePageAddressByAddress(_metaData.fileLengh - sizeof(long)));
                    _fileStream.Flush();
                }
                _metaData.fileLengh -= sizeof(long);

                return freeSpaceAddress;
            }
            else
            {
                if ((_metaData.fileLengh % _pageSize) + _recordLenght <= _pageSize)
                {
                    long newRecordAddress = _metaData.fileLengh;
                    LoadPageWithAddress(newRecordAddress);
                    Encoding.ASCII.GetBytes(record, 0, (int)record.Length, _buffor, (int)(newRecordAddress % _pageSize));
                    _buffor[(newRecordAddress % _pageSize) + record.Length] = (byte)'\0';

                    _metaData.fileLengh += _recordLenght;

                    return newRecordAddress;
                }
                //We have to create another page
                else
                {   
                    long newPageAddress = CalculatePageAddressByAddress(_metaData.fileLengh) + _pageSize;
                    _fileStream.Position = newPageAddress;
                    _fileStream.Write(new byte[_pageSize]);
                    _fileStream.Flush();

                    LoadPageWithAddress(_metaData.fileLengh % _pageSize);
                    Encoding.ASCII.GetBytes(record, 0, (int)record.Length, _buffor, 0);
                    _buffor[record.Length] = (byte)'\0';

                    _metaData.fileLengh = newPageAddress + _recordLenght;

                    return newPageAddress;
                }
            }
            
            //throw new NotImplementedException();

        }
        public char[] GetRecord(long address)
        {
            LoadPageWithAddress(address);

            long bufforStartingIndex = address % _pageSize;

            byte[] recordBytes = new byte[_recordLenght];
            char[] record = new char[_recordLenght];
            Encoding.ASCII.GetChars(_buffor, (int)bufforStartingIndex, _recordLenght, record, 0);
            
            for (int i = 0; i < _recordLenght; i++)
            {
                if (_buffor[bufforStartingIndex + i] == '\0') 
                {
                    return record.Take(i).ToArray();
                }
            }
            throw new Exception();
        }
        private long CalculatePageAddressByAddress(long address)
        {
            return (address / _pageSize) * _pageSize;
        }
        private void LoadPageWithAddress(long addressPosition)
        {
            if (addressPosition > _metaData.fileLengh)
            {
                throw new Exception("address doesn't exit in the file!");
            }

            if (_actualPosition != CalculatePageAddressByAddress(addressPosition))
            {   
                _fileStream.Position = _actualPosition;
                _fileStream.Write(_buffor);
                _fileStream.Flush();

                _fileStream.Position = CalculatePageAddressByAddress(addressPosition);
                _fileStream.Read(_buffor);

                _actualPosition = CalculatePageAddressByAddress(addressPosition);
            }

        }
    }
}
