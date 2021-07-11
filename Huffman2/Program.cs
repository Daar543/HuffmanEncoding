using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Huffman2
{
    public class Node
    {
        public byte Character;
        public long Frequency;
        public long Age;
        public Node Left;
        public Node Right;

        public bool IsLeaf()
        {
            return (this.Left == null && this.Right == null);
        }
    }
    /// <summary>
    /// Compares nodes by frequency/age
    /// </summary>
    public class CompareNodes : Comparer<Node>
    {
        public override int Compare(Node x, Node y)
        {
            if (true)
            {
                //compares: frequency>leaf>character>age
                int c = x.Frequency.CompareTo(y.Frequency);
                if (c != 0)
                    return c;
                if (x.IsLeaf() && y.IsLeaf())
                {
                    return x.Character.CompareTo(y.Character);
                }
                else if ((!x.IsLeaf()) && (!y.IsLeaf()))
                {
                    return x.Age.CompareTo(y.Age);
                }
                else return y.IsLeaf().CompareTo(x.IsLeaf()); //invert comparison, as leaves are lower
            }
        }
    }
    public class HTree
    {
        SortedSet<Node> Nodes = new SortedSet<Node>(new CompareNodes());
        public Node Root;
        public long[] Frequencies = new long[256];
        public int[,] CharCodes = new int[256,2];
        public void BuildTree(FileStream fs)
        {
            int aging = 0;
            //Makes a dictionary of bytes/frequencies in text
            const int bufferSize = 4096;
            int byteCount;
            while (true)
            {
                byte[] buffer = new byte[bufferSize];
                byteCount = fs.Read(buffer, 0, bufferSize);
                if (byteCount == 0)
                {
                    fs.Dispose();
                    break;
                }
                    
                for (int i = 0; i < byteCount; ++i)
                {
                    int by = buffer[i];
                    if (by == -1)
                        break;
                    byte nextByte = (byte)by;
                    Frequencies[nextByte]++;
                }
            }
            //Creates a node for every character
            for (int i = 0; i < Frequencies.Length; ++i)
            {
                if (Frequencies[(byte)i] == 0)
                    continue;
                Node n = new Node()
                {
                    Character = (byte)i,
                    Frequency = Frequencies[(byte)i],
                    Left = null,
                    Right = null,
                };
                Nodes.Add(n);
            }
            while (Nodes.Count > 1)
            {   //sorts nodes by frequency
                if (Nodes.Count >= 2)
                {
                    // Pop nodes with lowest frequency
                    Node pop1 = Nodes.Min;
                    Nodes.Remove(Nodes.Min);
                    Node pop2 = Nodes.Min;
                    Nodes.Remove(Nodes.Min);
                    // Creates a parent node by adding the frequencies, adds "Age" to every new node in order to establish comparison
                    Node parent = new Node()
                    {
                        Character = 0,
                        Frequency = pop1.Frequency + pop2.Frequency,
                        Left = pop1,
                        Right = pop2,
                        Age = aging++,
                    };
                    Nodes.Add(parent);
                }
            }
            this.Root = Nodes.Min;

        }
        /// <summary>
        /// Recursively writes the content of Huffman's tree as text
        /// </summary>
        /// <param name="tw">Text writer for printing the tree</param>
        /// <param name="thisNode">Root of current subtree</param>
        public void TraverseInOrder(TextWriter tw, Node thisNode)
        {
            if (thisNode is null)
            {
                return;
            }
            if (thisNode.IsLeaf())
            {
                tw.Write("*{0}:{1} ", thisNode.Character, thisNode.Frequency);
            }
            else
            {
                tw.Write("{0} ", thisNode.Frequency);
            }
            TraverseInOrder(tw, thisNode.Left);
            TraverseInOrder(tw, thisNode.Right);
        }
        /// <summary>
        /// Recursively writes the content of Huffman's tree into byte sequences
        /// </summary>
        /// <param name="bytes">The resulting byte sequence</param>
        /// <param name="thisNode">Root of subtree</param>
        public void TraverseInOrder(List<byte>bytes, Node thisNode)
        {
            if (thisNode is null)
            {
                return;
            }
            else
            {
                byte[] bytearr = FormatNode(thisNode);
                foreach(byte b in bytearr)
                {
                    bytes.Add(b);
                }
            }
            TraverseInOrder(bytes, thisNode.Left);
            TraverseInOrder(bytes, thisNode.Right);
        }
        /// <summary>
        /// Codes characters based on their position in the tree; 
        /// the result is being displayed in CharCodes array
        /// </summary>
        public void IncodeChars(Node currentNode, int currentDepth, int currentCode)
        {
            if(currentNode is null)
            {
                return;
            }
            else if (currentNode.IsLeaf())
            {
                CharCodes[currentNode.Character, 0] = currentCode;
                CharCodes[currentNode.Character, 1] = currentDepth;
            }
            int leftCode = currentCode; //redundant, just for readability
            int rightCode = currentCode;
            //add 1 on the way right
            rightCode |= (1 << (currentDepth ));
            currentDepth += 1;

            IncodeChars(currentNode.Left, currentDepth, leftCode);
            IncodeChars(currentNode.Right, currentDepth, rightCode);
        }
        /// <summary>
        /// Formates node: 0 - leaf, 1-55 - frequency, 56-63 - zero
        /// </summary>
        /// <param name="node">Node to be formatted</param>
        /// <returns></returns>
        public byte[] FormatNode(Node node)
        {
            ulong temp = 0;
            ulong truncateFrequency = (ulong)node.Frequency & (((ulong)1 << 55) - 1);
            temp |= (truncateFrequency << 1);
            byte[] result = new byte[8];
            if (node.IsLeaf())
            {
                temp |= 1;
                temp |= ((ulong)node.Character) << 56;
            }
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (byte)temp;
                temp >>= 8;
            }
            
            return result;
        }
    }

    public class HTreeOutput
    {
        readonly byte[] Header = { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66, };
        List<byte> Tree;
        List<byte> OutData;
        HTree huffTree;

        public HTreeOutput(HTree HT)
        {
            huffTree = HT;
            Tree = new List<byte>();
            OutData = new List<byte>();
        }

        public void GoThroughTree()
        {
            huffTree.TraverseInOrder(this.Tree, huffTree.Root);
        }

        public void Compress(FileStream fsIn, FileStream fsOut)
        {
            //First write the header and tree contents
            fsOut.Write(Header, 0, Header.Length);
            GoThroughTree();
            fsOut.Write(Tree.ToArray(),0,Tree.Count);
            fsOut.Write(new byte[8], 0, 8);


            //Initialize
            int[,] CodingMap = huffTree.CharCodes;
            const int inputBufferSize = 4096;
            const int outputBufferSize = 4096;
            int byteCount;
            int bitsInCurrentByte = 0;
            byte outputByte = 0;

            //Read until end of file
            while (true)
            {
                byte[] buffer = new byte[inputBufferSize];
                byteCount = fsIn.Read(buffer, 0, inputBufferSize);
                if (byteCount == 0)
                    break;
                for (int i = 0; i < byteCount; ++i)
                {
                    int by = buffer[i];
                    byte nextByte = (byte)by;
                    //Translate the nextByte into coded bitstream using CodingMap
                    //and concatenate with the previous one
                    int characterCode = CodingMap[nextByte, 0];
                    int characterBits = CodingMap[nextByte, 1];
                    //split the current bit sequence into bytes
                    while (characterBits > 0)
                    {
                        int freeBits = 8 - bitsInCurrentByte;

                        
                        if (characterBits < freeBits)
                        //the character is coded with less bits than are needed for current byte
                        {
                            outputByte |= (byte)((byte)characterCode << bitsInCurrentByte);
                            bitsInCurrentByte += characterBits;
                            characterBits = 0;
                        }
                        else
                        //bits fill current byte -> send the next byte into buffer 
                        //and keep reading remaining bits from current character
                        {
                            //nullify all except (fill) lower bits
                            byte mask = (byte)(((ulong)1 << freeBits) - 1);
                            byte added = (byte)(mask & characterCode);
                            //write the new bit sequence to upper bits
                            outputByte |= (byte)(added<<bitsInCurrentByte);
                            characterCode >>= freeBits;
                            characterBits -= freeBits;
                            //add new byte to output stream
                            OutData.Add(outputByte);
                            outputByte = 0;
                            if (OutData.Count == outputBufferSize)
                            {
                                fsOut.Write(OutData.ToArray(),0, outputBufferSize);
                                OutData = new List<byte>();
                            }
                            //new byte
                            bitsInCurrentByte = 0;
                        }
                        
                    }

                }
            }
            //flush out last byte, automatic padding of higher bits
            if(bitsInCurrentByte>0)
                OutData.Add(outputByte);
            fsOut.Write(OutData.ToArray(), 0, OutData.Count);
            fsOut.Flush();
            fsOut.Dispose();
            
            /*foreach (var d in CharData)
            {
                Console.WriteLine(d);
            }*/
        }
        
    }
    class Program
    {

        static void Main(string[] args)
        {
            /**/if (args.Length != 1)
            {
                Console.WriteLine("Argument Error");
                return;
            }
            string FileIn = args[0];
            string FileOut = FileIn + ".huff";
            
            try {
                HTree huffmanTree = new HTree();
                huffmanTree.BuildTree(File.OpenRead(args[0]));
                huffmanTree.IncodeChars(huffmanTree.Root, 0, 0);
                var huffmanTreeOut = new HTreeOutput(huffmanTree);
                huffmanTreeOut.Compress(File.OpenRead(FileIn), File.OpenWrite(FileOut));
            }
            catch(Exception ex)
            {
                if (ex is IOException || ex is FileNotFoundException)
                {
                    Console.WriteLine("File Error");
                    return;
                }
                else throw;
            }/**/

            /*/
            string[] FileInputs = new string[]
            {
                "binary.in",
                "simple.in",
                "simple2.in",
                "simple3.in",
                "simple4.in",
            };
            string[] FileOutputs = new string[]
            {
                "binary.out",
                "simple.out",
                "simple2.out",
                "simple3.out",
                "simple4.out",
            };
            var sw = new StreamWriter("megasoubor.txt",false);
            for(long j = 0; j < 5_000_000; ++j)
            {
                sw.Write("abcbcbac");
            }
            sw.Close();
            for (int i = 0; i < FileInputs.Length; ++i)
            {
                var wat = new Stopwatch();
                wat.Start();
                Console.WriteLine(i);
                HTree huffmanTree = new HTree();
                huffmanTree.BuildTree(File.OpenRead(FileInputs[i]));
                Console.WriteLine(wat.Elapsed);
                //huffmanTree.TraverseInOrder(Console.Out, huffmanTree.Root);
                huffmanTree.IncodeChars(huffmanTree.Root, 0, 0);
                var Otpt = new HTreeOutput(huffmanTree);
                Otpt.IncodeInput(File.OpenRead(FileInputs[i]),File.OpenWrite(FileOutputs[i]));
                Console.WriteLine();
            }/**/
        }
    }
}
