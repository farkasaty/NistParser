using NistParser;
using NistParser.Editing;
using System.Net.WebSockets;

WriteInColor("=========== APPLICATION START ===========", ConsoleColor.Yellow);

UpdateExistingUnknownField();
//ReadFileWithDuplicatUnknownField();

void WriteInColor(string message, ConsoleColor color)
{
	var origColor = Console.ForegroundColor;
	Console.ForegroundColor = color;
	Console.WriteLine(message);
	Console.ForegroundColor = origColor;
}

void UpdateExistingUnknownField()
{
	WriteInColor($"{nameof(UpdateExistingUnknownField)}", ConsoleColor.DarkGray);

	var fileName = "orig_SIS_LRT_01_02.nist";
	var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

	Console.WriteLine($"Loading nist file: {filePath}");
	var editor = NistTransactionEditor.FromFile(filePath);

	var recordToEdit = editor.Transaction.Records.First(x => x.RecordType == NistParser.Constants.RecordType.Type2_UserDefinedText);
	var result = editor.SetFieldValue(recordToEdit, "2.003", "NEW VALUE");

	if (!result.IsValid)
	{
		var message = "Modification failed:\n" + result.ErrorMessage;
		WriteInColor(message, ConsoleColor.Red);
		return;
	}

	WriteInColor("Modification was successful!", ConsoleColor.Green);

	var resultValidation = editor.ValidateAllChanges();
	if (!resultValidation.IsValid)
	{
		var message = $"Validation failed:\n{string.Join("\n\t - ", resultValidation.ErrorMessages)}";
		WriteInColor(message, ConsoleColor.Red);
		return;
	}
	WriteInColor("Validation was successful!", ConsoleColor.Green);

	var modifiedFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_modified.nist";
	var modifiedFilePath = Path.Combine(Directory.GetCurrentDirectory(), modifiedFileName);
	Console.WriteLine($"Saving as: {modifiedFilePath}");
	editor.SaveToFile(modifiedFilePath);

	WriteInColor("=========== APPLICATION END ===========", ConsoleColor.Yellow);
}

void ReadFileWithDuplicatUnknownField()
{
	WriteInColor($"{nameof(ReadFileWithDuplicatUnknownField)}", ConsoleColor.DarkGray);

	var fileName = "orig_SIS_LRT_01_02.nist";
	var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

	Console.WriteLine($"Loading nist file: {filePath}");

	var transaction = NistTransactionParser.ParseFile(filePath);
	var record = transaction.Records.First(x => x.RecordType == NistParser.Constants.RecordType.Type2_UserDefinedText);
	var field = record.GetField("2.003");

	Console.WriteLine($" {field.FieldNumber} = {field.FirstValue}");
}