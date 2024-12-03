using static System.Net.Mime.MediaTypeNames;

namespace BTree
{
    internal class Program
    {
        static void Main(string[] args)
        {

            BTree BTree1 = new BTree("btreeTest1", CreatingMode.Create);

            BTree1.AddRandomRecords(27);

            BTree1.PrintAllInfo();

            BTree1.AddRecord(27, new char[] {'a' });



            //using FileStream fileStream = new FileStream("test.txt", FileMode.OpenOrCreate ,FileAccess.ReadWrite);
            //string text = "Hej, to test czy to dziala";
            //string secondText = "Paa, ha ha ha";
            //fileStream.Write(System.Text.Encoding.ASCII.GetBytes(text),0,text.Length);
            //fileStream.Flush();

            //fileStream.Position = 0;
            //fileStream.Write(System.Text.Encoding.ASCII.GetBytes(secondText), 0, secondText.Length);
            //fileStream.Flush();

        }
    }
}
