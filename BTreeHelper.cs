using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTree
{
    public static class BTreeHelper
    {
        private static char[] GenerateRecord(int lenght)
        {
            //Generate char sets without duplicates

            if (lenght < 1)
            {
                throw new ArgumentException("Record lenght too short");
            }
            else if ((Consts.MaxCharValue - Consts.MinCharValue) + 1 < lenght)
            {
                throw new ArgumentException("Record too long");
            }

            Random rnd = new Random();

            char[] record = new char[lenght];

            for (int i = 0; i < lenght; i++)
            {
                do
                {
                    record[i] = (char)rnd.Next((int)40, (int)120);
                } while (record.Take(i).Contains(record[i]));
            }
            return record;
        }

        public static void AddRandomRecords(this BTree bTree, int numberOfRandomRecords)
        {
            Random random = new Random();
            //for (int i = 0; i < numberOfRandomRecords; i++) 
            //{
            //    bTree.AddRecord(random.NextInt64(), GenerateRecord(random.Next(1,(int)(Consts.RecordLength - 1))));
            //}
            //bTree.AddRecord(1, GenerateRecord(random.Next(1,(int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(2, GenerateRecord(random.Next(1,(int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(3, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(4, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            //bTree.AddRecord(5, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(6, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(7, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(8, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            //bTree.AddRecord(9, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(10, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(11, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(12, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            for(int i = 0; i < numberOfRandomRecords; i++)
            {
                bTree.AddRecord(i, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            }


        }
    }
}
