using AngleSharp;
using AngleSharp.Dom;
using CsvHelper;
using System.Globalization;
using System.Text.RegularExpressions;
using ToysRuParser.Exceptions;
using ToysRuParser.Models;
using ToysRuParser.View;

namespace ToysRuParser
{
    public class ToysCatalogParser
	{
		private const string _product = "main .container";
		private const string _toyLink = "meta";
		private const string _pagination = ".pagination.justify-content-between li";

		public ToysCatalogParser(CatalogSetting cs, IParserUserInterface? ui = null)
        {
			_setting = cs;
			var config = Configuration.Default.WithDefaultLoader();
			_context = BrowsingContext.New(config);
			_userInterface = ui ?? new ParserViewer();
        }

        private readonly IBrowsingContext _context;
		private readonly CatalogSetting _setting;
		private readonly IParserUserInterface _userInterface;

		public async Task Parse()
		{
			using var catalogDoc = await _context.OpenAsync(_setting.Link)
				?? throw new DocumentNullException("Страница каталога не загрузилась");

			var chapterCount = GetChapterCount(catalogDoc);
			Print($"Собираю ссылки с {chapterCount} разделов");

			string? [] links = GetProductLinksFromChapters(chapterCount);
			links = await CombineChaptersProducts(links);

			_userInterface.ProductCount = links.Length;

			Product [] products = new Product[links.Length];

			for (int i = 0; i < products.Length; i++)
			{
				IElement catalogMarkup = await GetProductMarkup(links[i]);
				Product product = ProductParser.Parse(catalogMarkup);
				_userInterface.PrintProduct(product);
				products[i] = ProductParser.Parse(catalogMarkup);
			}

			await RecordCSV(products);

			Print("Парсинг выполнен");
		}

		private string[] GetProductLinksFromChapters(int paginatorMaxPage) {

			string[] links = new string[paginatorMaxPage];
			for (int i = 0; i < paginatorMaxPage; i++)
			{
				links[i] = _setting.GetParametrizeUrl(i + 1);
			}
			return links;
		}

		private static int GetChapterCount(IDocument doc)
		{
			IEnumerable<IElement> a = doc.QuerySelectorAll(_pagination);
			if (a.Count() == 0)
				return 1;
			
			string? pagesCount = a.ElementAt(a.Count() - 2).TextContent;

			if (string.IsNullOrEmpty(pagesCount))
				throw new KeyNotFoundException("Pagination numbering not found");
			pagesCount = Regex.Replace(pagesCount, @"\t|\n|\r", "");

			return Convert.ToInt32(pagesCount);
		}

		private async Task<string?[]> CombineChaptersProducts(string? [] linkToPages)
		{
			string?[] links = Array.Empty<string>();
			foreach (string? link in linkToPages)
			{
				if (string.IsNullOrEmpty(link))
					throw new ArgumentException("Link cannot be null or Empty");
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

			Print("Записываю в csv файл");
			using var writer = new StreamWriter("Toy.csv");
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			await csv.WriteRecordsAsync(products);
		}

		private static void Print(string message) {
		
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(message);
			Console.ForegroundColor = ConsoleColor.White;
		}
	};
}
