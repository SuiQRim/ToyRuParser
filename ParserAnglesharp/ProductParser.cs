using AngleSharp.Dom;
using System.Globalization;
using System.Text.RegularExpressions;
using ToysRuParser.Exceptions;
using ToysRuParser.Models;

namespace ToysRuParser
{
    internal static class ProductParser
	{
		private const string _name = ".content-title";
		private const string _price = ".price";
		private const string _oldPrice = ".old-price";
		private const string _breadcrumbItems = ".breadcrumb-item a";
		private const char _separator = '>';

		public static Product Parse(IElement? doc)
		{

			if (doc is null)
				throw new DocumentNullException("Страница продукта не загрузилась");

			// С текущей ценой проблем не должно быть, а старой цены может не быть
			// Поэтому в случае, если исключение, которое возникает если не найдено значение
			// Присваиваем текущую цену

			decimal oldPrice;
			decimal price = ExtractPrices(doc, _price);
			try
			{
				oldPrice = ExtractPrices(doc, _oldPrice);
			}
			catch (KeyNotFoundException)
			{
				oldPrice = price;
			}

			//Создаем сам объект
			Product product = new()
			{
				LinkToProduct = doc.BaseUri,
				Title = ExtractTitle(doc),
				CurrentPrice = price,
				OldPrice = oldPrice,
				Breadcrumbs = ExtractBreadcrumb(doc),
			};
			return product;

		}

		private static string ExtractTitle(IElement doc)
		{
			IElement titleMarkup = doc.QuerySelector(_name) ??
				throw new KeyNotFoundException("Title of product is not Found");
			return titleMarkup.TextContent;

		}
		/// <summary>
		/// Находит цену
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="className"></param>
		/// <returns></returns>
		/// <exception cref="FormatException">Цена не найдена</exception>
		private static decimal ExtractPrices(IElement doc, string className)
		{
			IElement? priceMarkup = doc.QuerySelector(className) ??
				throw new KeyNotFoundException($"Product price with className {className} is not found");
			string price = priceMarkup.TextContent;

			// С помощью LINQ и регулярных выражений получаем только числа из строки
			price = string.Join(string.Empty, Regex.Matches(price, @"\d+").OfType<Match>().Select(m => m.Value));

			if (string.IsNullOrEmpty(price))
				throw new FormatException($"Price tag with class name '{className} is not found'");

			return Convert.ToDecimal(price, new CultureInfo("en-US"));
		}


		/// <summary>
		/// Находит "хлебные крошки" в документе
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		private static string ExtractBreadcrumb(IElement doc)
		{
			//Находим все заголовки разделов
			//С помощью LINQ получаем массив значений и потом объединяем разделяя все нужным знаком

			var breadcrumb = doc.QuerySelectorAll(_breadcrumbItems);
			string?[] titles = breadcrumb.Select(s => s.GetAttribute("title")).ToArray();
			return string.Join($" {_separator} ", titles);
		}
	}
}
