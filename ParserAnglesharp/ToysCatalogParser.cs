using AngleSharp;
using AngleSharp.Dom;
using CsvHelper;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ToysRuParser
{
	public class ToysCatalogParser
	{

		private const string _product = "main .container";
		private const string _name = ".detail-name";
		private const string _price = ".price";
		private const string _oldPrice = ".old-price";
		private const string _available = ".net-v-nalichii";
		private const string _breadcrumbItems = "a.breadcrumb-item";
		private const string _toyLink = "meta";

		public async Task Parse(string catalogLink)
		{
			var config = Configuration.Default.WithDefaultLoader();
			using var context = BrowsingContext.New(config);

			using var catalogDoc = await context.OpenAsync(catalogLink);
			string? [] links = GetProductLinks(catalogDoc);

			List<Product> products = new ();
			foreach (var link in links)
			{
				using var doc = await context.OpenAsync(link) ?? throw new DocumentNullException();

				var mainDoc = doc.QuerySelector(_product);

				products.Add(ParseProduct(mainDoc));
			}		

			await RecordCSV(products);
		}

		/// <summary>
		/// Собирает все ссылки на страницы с продуктами
		/// </summary>
		/// <param name="catalogLink">Ссылка на раздел в каталоге</param>
		/// <param name="context"></param>
		/// <returns></returns>
		private string?[] GetProductLinks(IDocument doc) {

			var productImages = doc.QuerySelectorAll(_toyLink);

			var productsLinks = productImages.
				Where(s => s.GetAttribute("itemprop") == "url").
				Select(g => g.GetAttribute("content")).
				ToArray();

			return productsLinks;
		}

		private static int _count = 0;
		/// <summary>
		/// Парсит документ с продуктом
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		/// <exception cref="DocumentNullException"></exception>
		private Product ParseProduct(IElement? doc) {

			if (doc is null)
				throw new DocumentNullException();

			string title = doc.QuerySelector(_name).TextContent;

			// С текущей ценой проблем не должно быть, а старой цены может не быть
			// Поэтому в случае, если исключение, которое возникает если не найдено значение
			// Присваиваем текущую цену	
			decimal price = ExtractPrices(doc, _price);
			decimal oldPrice;
			try
			{
				oldPrice = ExtractPrices(doc, _oldPrice);
			}
			catch (FormatException)
			{
				oldPrice = price;
			}		


			//Создаем сам объект
			Product product = new()
			{
				Title = title,
				CurrentPrice = price,
				OldPrice = oldPrice,
				IsAvailable = ChekingToyAvailible(doc),
				Breadcrumbs = ExtractBreadcrumb(doc),
			};

			Console.WriteLine(_count++);
			return product;

		}

		/// <summary>
		/// Находит цену
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="className"></param>
		/// <returns></returns>
		/// <exception cref="FormatException">Цена не найдена</exception>
		private decimal ExtractPrices(IElement doc, string className)
		{
			string price = doc.QuerySelector(className).TextContent;

			// С помощью LINQ и регулярных выражений получаем только числа из строки
			price = string.Join(string.Empty, Regex.Matches(price, @"\d+").OfType<Match>().Select(m => m.Value));

			if (string.IsNullOrEmpty(price))
				throw new FormatException($"Price tag with class name '{className} is not found'");

			return Convert.ToDecimal(price, new CultureInfo("en-US"));
		}

		/// <summary>
		/// Ищет наличие товара и возвращает булевое значение
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		private bool ChekingToyAvailible(IElement doc)
		{
			// Я решил что лучше искать наличие по его отсутсвию, поэтому идет поиск 
			// по html тегу, который появляется если товар отсутсвует
			IElement? availible = doc.QuerySelector(_available);
			return availible is null;
		}

		/// <summary>
		/// Находит "хлебные крошки" в документе
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		private string ExtractBreadcrumb(IElement doc) {

			//Находим все заголовки разделов
			//С помозью LINQ получаем массив значений и потом объединяем разделяя все нуным знаком
			//добавляем в конце название товара (не уверен что название товара так нужно поэтому закоментировал)

			var breadcrumb = doc.QuerySelectorAll(_breadcrumbItems);
			string?[] titles = breadcrumb.Select(s => s.GetAttribute("title")).ToArray();
			return string.Join(" > ", titles) /* + " > " + doc.QuerySelector(_name).TextContent  */; 
		}

		/// <summary>
		/// Записывает товары в csv файл
		/// </summary>
		/// <param name="Product"></param>
		/// <returns></returns>
		private async Task RecordCSV(List<Product> Product) {

			using var writer = new StreamWriter("Toy.csv");
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			await csv.WriteRecordsAsync(Product);
		}
	};
}
