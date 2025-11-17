using System;
using System.IO;
using NistParser;
using NistParser.Writing;

// Get project root directory (go up from bin/Debug/net9.0)
string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string originalPath = Path.Combine(projectRoot, "example files", "hibas", "orig_SIS_ATM_C_P25.nist");
string copyPath = Path.Combine(projectRoot, "example files", "hibas", "orig_SIS_ATM_C_P25 - Copy.nist");

// Read original file
byte[] originalBytes = File.ReadAllBytes(originalPath);
Console.WriteLine($"Original: {originalBytes.Length} bytes");

// Parse and serialize
var transaction = NistTransactionParser.Parse(originalBytes);
byte[] savedBytes = NistTransactionWriter.WriteTraditional(transaction);

// Save copy
File.WriteAllBytes(copyPath, savedBytes);
Console.WriteLine($"Saved: {savedBytes.Length} bytes");
Console.WriteLine($"Difference: {savedBytes.Length - originalBytes.Length} bytes");

if (savedBytes.Length == originalBytes.Length)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\nSUCCESS: File sizes match!");
    Console.ResetColor();
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\nERROR: File sizes differ!");
    Console.ResetColor();
}
