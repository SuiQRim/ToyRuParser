using AngleSharp;
using AngleSharp.Dom;
using CsvHelper;
using Microsoft.VisualBasic;
using System.Globalization;

namespace ParserAnglesharp
{
	public class ToysCatalogParser
	{

		private const string _name = ".detail-name";
		private const string _price = ".price";
		private const string _oldPrice = ".old-price";
		private const string _available = ".net-v-nalichii";
		private const string _breadcrumbItems = "a.breadcrumb-item";


		public async Task Parse()
		{
			var config = Configuration.Default.WithDefaultLoader();
			using var context = BrowsingContext.New(config);

			var url = "https://www.toy.ru/catalog/toys-spetstekhnika/wow_wee_5569_power_treads_vezdekhod_s_avtotrekom/";

			using var doc = await context.OpenAsync(url);
			Product toy = ParseProduct(doc);

			await RecordCSV(new List<Product>() {toy});
		}


		private Product ParseProduct(IDocument? doc) {

			if (doc is null)
				throw new DocumentNullException();

			Product product = new()
			{
				Title = doc.QuerySelector(_name).TextContent,
				CurrentPrice = Convert.ToDecimal(doc.QuerySelector(_price).TextContent),
				OldPrice = Convert.ToDecimal(doc.QuerySelector(_oldPrice).GetAttribute("content")),
				IsAvailable = ChekingToyAvailible(doc),
				Breadcrumbs = ExtractBreadcrumb(doc),
			};

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
