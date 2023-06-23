using AngleSharp;
using AngleSharp.Dom;
using CsvHelper;
using System.Globalization;

namespace ToysRuParser
{
	public class ToysCatalogParser
	{

		private const string _main = ".row";
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

			string? [] links = await GetProductLinks(catalogLink, context);

			List<Product> products = new ();
			foreach (var link in links)
			{
				using var doc = await context.OpenAsync(link);
				products.Add(ParseProduct(doc));
			}		

			await RecordCSV(products);
		}


		private async Task<string?[]> GetProductLinks(string catalogLink, IBrowsingContext context) {

			using var doc = await context.OpenAsync(catalogLink);
			var productImages = doc.QuerySelectorAll(_toyLink);

			var productsLinks = productImages.
				Where(s => s.GetAttribute("itemprop") == "url").
				Select(g => g.GetAttribute("content")).
				ToArray();

			return productsLinks;
		}

		private static int _count = 0;
		private Product ParseProduct(IDocument? doc) {

			if (doc is null)
				throw new DocumentNullException();

			var pdoductDoc = doc.QuerySelector(_main);
			string title = pdoductDoc.QuerySelector(_name).TextContent;
			Product product = new()
			{
				Title = pdoductDoc.QuerySelector(_name).TextContent,
				CurrentPrice = Convert.ToDecimal(pdoductDoc.QuerySelector(_price).TextContent),
				OldPrice = Convert.ToDecimal(pdoductDoc.QuerySelector(_oldPrice).GetAttribute("content")),
				IsAvailable = ChekingToyAvailible(doc),
				Breadcrumbs = ExtractBreadcrumb(doc),
			};

			Console.WriteLine($"Продукт спарсен {_count++}");
			return product;

		}


		private bool ChekingToyAvailible(IDocument doc)
		{
			IElement? availible = doc.QuerySelector(_available);
			return availible is not null;
		}

		private string ExtractBreadcrumb(IDocument doc) {

			//Находим все заголовки разделов
			//С помозью LINQ получаем массив значений и потом объединяем разделяя все нуным знаком
			//добавляем в конце название товара (не уверен что название товара так нужно поэтому закоментировал)

			var breadcrumb = doc.QuerySelectorAll(_breadcrumbItems);
			string?[] titles = breadcrumb.Select(s => s.GetAttribute("title")).ToArray();
			return string.Join(" > ", titles) /* + " > " + doc.QuerySelector(_name).TextContent  */; 
		}

		
		private async Task RecordCSV(List<Product> toys) {

			using var writer = new StreamWriter("Toy.csv");
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			await csv.WriteRecordsAsync(toys);
		}
	};
}
