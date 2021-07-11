# HuffmanEncoding

A solution (from scratch) of a homework to C# Programming.

1. Description of Huffman Tree

A Huffman tree is a binary tree whose leaf nodes contain all characters present in the input file at least once. 
The weight of a character refers to the number of occurrences of this character in the input file. 
For example, the data xxyz contain characters x, y and z, with x having the weight of 2 and both y and z having the weight of 1.
Inner nodes of the tree do not contain any characters and their weight is defined as the sum of the weights of both their child nodes 
(inner nodes always have two child nodes).

2. Specified output

The output file has the following format: header, encoding tree and encoded data. 

2.1 Header

The header consists of 8 bytes with the following values:
0x7B 0x68 0x75 0x7C 0x6D 0x7D 0x66 0x66

2.2 Tree

The tree is written in the prefix notation, with each node stored as a 64-bit number encoded in the Little Endian format 
(bit-ordering used e.g. on the IA-32/x86 platform). Each node therefore takes exactly 8B space in the file. 
The tree description is terminated via a special sequence consisting of 8 zero bytes (i.e. 64-bit 0 value)
, which is not used for encoding any node (serving only as a description terminator).

2.3 Nodes

The inner nodes have the following format:

bit 0: is set to 0, indicating that this node is an inner node

bits 1-55: contain lower 55 bits of the weight of the inner node

bits 56-63: are set to zero

And the leaf nodes have the following format:

bit 0: is set to 1, indicating that this node is a leaf node

bits 1-55: contain lower 55 bits of the weight of the leaf node

bits 56-63: 8-bit value of the symbol this leaf node corresponds to

3. Encoding

The encoding works as follows. Each character in input file is encoded as a bit sequence corresponding to the path from the Huffman tree root 
to the leaf containing this character. Edges towards left child nodes represent 0 bit value, while edges towards right child nodes represent 1. 
Data are encoded as a bit stream, as the various symbols can have bit sequence codes of different length (even a sequence much longer than 8 bits). 
The sequences for individual input characters are placed in the output bit stream next to each other (in the same order as the original characters in the input file). 
Because only full bytes can be written to an output file, zero bits are added to the output bit stream to achieve a total bit count of the nearest multiple of 8. 
When encoding, store the 0th bit of the stream in the 0 bit (i.e. lowest) of the first byte, etc. up to the 7th bit of the stream. 
The 8th bit is then stored in the 0 bit of the second byte, 16th bit in the 0 bit of the third byte, etc.

For example, the sequence 1101 0010 0001 1010 111 (spaces are used only for increased readability) will be encoded into three bytes: 0x4B 0x58 0x07.
