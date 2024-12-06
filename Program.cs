using System;
using static System.Net.Mime.MediaTypeNames;

namespace BTree
{

    internal class Program
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
        static void Main(string[] args)
        {
            Random random = new Random();


            BTree bTree = new BTree("btreeTest1", CreatingMode.Create);

            bTree.AddRandomRecords(30);

            //bTree.AddRecord(1, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(79, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(92, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(13, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            //bTree.AddRecord(89, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(4, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(41, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.PrintAllInfo();

            //bTree.AddRecord(51, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            //BTree1.AddRecord(27, new char[] {'a' });


            //bTree.AddRecord(26, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(3, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(2, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(1, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            //bTree.AddRecord(8, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(6, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(7, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(5, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            //bTree.AddRecord(25, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(10, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(11, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(12, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));



            //bTree.AddRecord(8, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            //bTree.PrintAllInfo();
            //Console.WriteLine("\n\n\n\n");

            //bTree.AddRecord(163, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));

            //bTree.PrintAllInfo();
            //Console.WriteLine("\n\n\n\n");

            //bTree.AddRecord(7, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(5, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));
            //bTree.AddRecord(142, GenerateRecord(random.Next(1, (int)(Consts.RecordLength - 1))));


            bTree.PrintAllInfo();


        }
    }
}
