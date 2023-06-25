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
		private readonly int _threadCount = Environment.ProcessorCount;
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

			//Для дебага: Ограничиваю чтобы удобнее дебажить
			Array.Resize(ref links, 4);

			// TODO: Чтение страниц пагинации

			int linkPerThread = links.Length / (_threadCount);
			int lastLinkPool = links.Length % (_threadCount);
			int threadCount = _threadCount;
			if (linkPerThread == 0)
			{
				// Если ссылок на потоков 0, то значит что при разделении оказалось что потоков в процессоре больше
				// Значит мы можем для каждой страницы предоставить свой поток, а остаток убрать
				threadCount = lastLinkPool;
				linkPerThread = 1;
				lastLinkPool = 0;
			}

			#if DEBUG
			Console.WriteLine("\nПотоков " + _threadCount);
			Console.WriteLine("\nКол-во ссылок " + _threadCount);
			Console.WriteLine("\nКол-во ссылок на один поток " + linkPerThread);
			Console.WriteLine("\nКол-во ссылок на последний поток " + lastLinkPool);
			#endif

			// Если есть остаточные страницы, нужно добавить для них свой поток
			if (lastLinkPool != 0)
				threadCount++;

			Task[] threads = new Task[threadCount];
			Product [] products = new Product[links.Length];

			int streamCount = 0;
			for (int i = linkPerThread; i <= links.Length; i += linkPerThread)
			{
				threads[streamCount++] = ProductPoolParser(i, linkPerThread, links, products);
			}

			// Остаточные страницы
			if (lastLinkPool != 0)
				threads[^1] = ProductPoolParser(links.Length, lastLinkPool, links, products);

			Task.WaitAll(threads);
			await RecordCSV(products);
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
		private static async Task RecordCSV(IEnumerable<Product> products) {

			using var writer = new StreamWriter("Toy.csv");
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			await csv.WriteRecordsAsync(products);
		}
	};
}
