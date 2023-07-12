using ToysRuParser;

bool isCorrect = false;
Uri? uri = null;
while (!isCorrect)
{
	Console.Clear();
	Console.WriteLine("Введите полный адресс к разделу товаров с сайта toy.ru");
	string? url = Console.ReadLine();
	isCorrect = Uri.TryCreate(url, UriKind.Absolute, out uri)
		&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
	if (!isCorrect)
	{
		Console.WriteLine("\nАдресс не подходит");
		Console.WriteLine("Нажмие любую клавишу, чтобы повторить ввод...");
		Console.ReadKey();
	}
} 

if (uri == null)
	throw new UriFormatException($"При сборке URL возникла проблема ({nameof(uri)} == null)");
	

CatalogSetting cs = new(uri.OriginalString);
ToysCatalogParser toyParser = new (cs);


await toyParser.Parse();