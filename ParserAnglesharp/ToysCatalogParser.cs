using AngleSharp;
using AngleSharp.Common;
using AngleSharp.Dom;
using CsvHelper;
using System.Globalization;
using System.Text.RegularExpressions;
using ToysRuParser.Exceptions;

namespace ToysRuParser
{
    public class ToysCatalogParser
	{
		// Здесь значение 8 для удобства для разработки, а так будет Environment.ProcessorCount
		private int _threadCount = 8;


		private const string _product = "main .container";
		private const string _name = ".detail-name";
		private const string _price = ".price";
		private const string _oldPrice = ".old-price";
		private const string _available = ".net-v-nalichii";
		private const string _breadcrumbItems = "a.breadcrumb-item";
		private const string _toyLink = "meta";

        public ToysCatalogParser()
        {
			var config = Configuration.Default.WithDefaultLoader();
			_context = BrowsingContext.New(config);
        }

        private readonly IBrowsingContext _context;


		public async Task Parse(string catalogLink)
		{
			using var catalogDoc = await _context.OpenAsync(catalogLink);

			//Ссылки на сайты которые нужно спарсить
			string? [] links = GetProductLinks(catalogDoc);

			//Для дебага: Ограничиваю до 16 ссылок чтобы удобнее дебажить
			Array.Resize(ref links, 16);

		
			int linkPerThread = links.Length / (_threadCount);
			int lastLinkPool = links.Length % (_threadCount);

			#region ForDebug
			//Для дебага: Выводим всякие данные в консоль
			Console.WriteLine("\nПотоков " + _threadCount);
			Console.WriteLine("\nКол-во ссылок " + _threadCount);
			Console.WriteLine("\nКол-во ссылок на один поток " + linkPerThread);
			Console.WriteLine("\nКол-во ссылок на последний поток " + lastLinkPool);
			#endregion

			// TODO: Поток, который будет заниматся остаточными страницами

			// TODO: Предусмотреть ситуацию когда потоков больше, чем страниц

			// TODO: Чтение страниц пагинации

			// TODO: Вынести методы, которые заниматься чисто парсингом в отдельный класс,
			// а методы связанные с делением на потоки и т.п. оставить

			Task[] threads = new Task[_threadCount];
			Product [] products = new Product[links.Length];

			int threadCount = 0;
			for (int i = linkPerThread; i <= links.Length; i += linkPerThread)
			{
				threads[threadCount++] = ProductPoolParser(i, linkPerThread, links, products);
			}

			Task.WaitAll(threads);
			await RecordCSV(products.ToList());
		}



		private async Task ProductPoolParser(int iterationRange, int linkPerThread, string?[] links, Product[] products) {

			for (int i = 0; i < linkPerThread; i++)
			{
				IElement catalogMarkup = await GetProductMarkup(links[iterationRange - linkPerThread + i]);
				lock (products)
				{
					Product product = ParseProduct(catalogMarkup);
					products[iterationRange - linkPerThread + i] = product;
				}

			}
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

		private async Task<IElement> GetProductMarkup(string link)
		{
			using var doc = await _context.OpenAsync(link) ?? throw new DocumentNullException();
			return doc.QuerySelector(_product) ?? throw new ProductNotFountException();
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

			IElement titleMarkup = doc.QuerySelector(_name) ??
				throw new KeyNotFoundException("Title of product is not Found");
			string title = titleMarkup.TextContent;

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

			Console.WriteLine($"Спарсили {_count++}");
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
			IElement? priceMarkup = doc.QuerySelector(className) ?? 
				throw new KeyNotFoundException("Product price with className {} is not found");
			string price = priceMarkup.TextContent;

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
