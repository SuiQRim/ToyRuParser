using AngleSharp;
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
		private const string _pagination = ".pagination.justify-content-between li";

		public ToysCatalogParser(CatalogSetting cs)
        {
			_setting = cs;
			var config = Configuration.Default.WithDefaultLoader();
			_context = BrowsingContext.New(config);
        }

        private readonly IBrowsingContext _context;
		private readonly CatalogSetting _setting;

		public async Task Parse()
		{
			using var catalogDoc = await _context.OpenAsync(_setting.Link)
				?? throw new DocumentNullException("Страница каталога не загрузилась");

			var a = GetPagesCount(catalogDoc);
			//Ссылки на сайты которые нужно спарсить
			string? [] links = GetProductsLinksFromPaginator(a);
			links = await CombineProductsFromPages(links);

			//Для дебага: Ограничиваю чтобы удобнее дебажить

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




		/// <summary>
		/// Выполняет парсинг своего диапазона страниц
		/// </summary>
		/// <param name="iterationRange">Диапазон страниц который нужно парсить</param>
		/// <param name="linkPerThread">Кол-во страниц на один поток</param>
		/// <param name="links">Страницы (их ссылки)</param>
		/// <param name="products">Массив с спарсеными продуктами</param>
		/// <returns></returns>
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

		private string[] GetProductsLinksFromPaginator(int paginatorMaxPage) {

			string[] links = new string[paginatorMaxPage];
			for (int i = 0; i < paginatorMaxPage; i++)
			{
				links[i] = _setting.GetParametrizeUrl(i + 1);
			}
			return links;
		}

		private static int GetPagesCount(IDocument doc)
		{
			IEnumerable<IElement> a = doc.QuerySelectorAll(_pagination);
			string? pagesCount = a.ElementAt(a.Count() - 2).TextContent;

			if (string.IsNullOrEmpty(pagesCount))
				throw new KeyNotFoundException("Pagination numbering not found");
			pagesCount = Regex.Replace(pagesCount, @"\t|\n|\r", "");

			return Convert.ToInt32(pagesCount);
		}

		private async Task<string?[]> CombineProductsFromPages(string [] linkToPages)
		{
			string?[] links = Array.Empty<string>();
			foreach (string link in linkToPages)
			{
				Console.WriteLine(link);
				IDocument doc = await _context.OpenAsync(link);
				string?[] linksToProducts = GetProductsLinks(doc);
				links = links.Concat(linksToProducts).ToArray() ?? throw new Exception();
			}

			return links;
		}


		/// <summary>
		/// Собирает все ссылки на страницы с продуктами
		/// </summary>
		/// <param name="catalogLink">Ссылка на раздел в каталоге</param>
		/// <param name="context"></param>
		/// <returns></returns>
		private static string?[] GetProductsLinks(IDocument doc) {

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
