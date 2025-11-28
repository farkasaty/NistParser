using NistParser;
using NistParser.Editing;
using System.Net.WebSockets;

WriteInColor("=========== APPLICATION START ===========", ConsoleColor.Yellow);

//UpdateExistingUnknownField();
//ReadFileWithDuplicatUnknownField();
Type9Test();

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

void Type9Test()
{
	WriteInColor($"{nameof(Type9Test)}", ConsoleColor.DarkGray);

	var fileName = "orig_SIS_MMS_C_P25M.nist";
	var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

	Console.WriteLine($"Loading nist file: {filePath}");

	Dictionary<string, string> probeType_1Attributes = new Dictionary<string, string>
	{
		{ "1.004", "YYY" },
		{ "1.005", "20251113" },
		{ "1.007", "EU/SISII-AFIS-MOD" },
		{ "1.008", "BE/TEST/MOD" },
		{ "1.009", "2000000366Q" },
	};
	Dictionary<string, string> type_2Attributes = new Dictionary<string, string>
	{
		{ "2.003", "0101" },
		{ "2.007", "CS/2019/28302-2" },
	};

	var editor = NistTransactionEditor.FromFile(filePath);
	foreach(KeyValuePair<string, string> kv in probeType_1Attributes)
	{
		var result = editor.SetFieldValue(editor.Transaction.Header, kv.Key, kv.Value);
		if (!result.IsValid)
		{
			var message = "Modification of field '" + kv.Key + "' failed:\n" + result.ErrorMessage;
			WriteInColor(message, ConsoleColor.Red);
			return;
		}
	}

	WriteInColor("Validation was successful for type1 record!", ConsoleColor.Green);

	var type2Record = editor.Transaction.GetRecord<NistParser.Records.Type2Record>("0") ?? throw new Exception("type2Record error");
	foreach (KeyValuePair<string, string> kv in type_2Attributes)
	{
		var result = editor.SetFieldValue(type2Record, kv.Key, kv.Value);
		if (!result.IsValid)
		{
			var message = "Modification of field '" + kv.Key + "' failed:\n" + result.ErrorMessage;
			WriteInColor(message, ConsoleColor.Red);
			return;
		}
	}

	WriteInColor("Validation was successful for type2 record!", ConsoleColor.Green);

	var modifiedFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_modified.nist";
	var modifiedFilePath = Path.Combine(Directory.GetCurrentDirectory(), modifiedFileName);
	Console.WriteLine($"Saving as: {modifiedFilePath}");
	editor.SaveToFile(modifiedFilePath);

	WriteInColor("=========== APPLICATION END ===========", ConsoleColor.Yellow);
}

