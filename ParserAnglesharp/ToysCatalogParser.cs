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
				Product product = ProductParser.Parse(catalogMarkup);
				lock (products)
				{
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
		private static string?[] GetProductLinks(IDocument doc) {

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


		/// <summary>
		/// Записывает товары в csv файл
		/// </summary>
		/// <param name="Product"></param>
		/// <returns></returns>
		private static async Task RecordCSV(List<Product> Product) {

			using var writer = new StreamWriter("Toy.csv");
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			await csv.WriteRecordsAsync(Product);
		}
	};
}
