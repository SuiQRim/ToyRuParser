using System.Text;
using ToysRuParser.Models;

namespace ToysRuParser.View
{
    public class ParserViewer : IParserUserInterface
	{
        private const char _progressBarSymbol = '█';

		public int ProductProgress { get; set; }
		public int ProductCount { get;  set; }

		public void PrintCatalogLink(string link)
		{
			throw new NotImplementedException();
		}

		public void PrintPaginationNumber(string paginationLink)
		{
			throw new NotImplementedException();
		}

		private int _poductHeight;

		private const int _marginHoriz = 10;
		public void PrintProduct(Product product)
		{
			int indicator = 8;
			int maxLeigth;

			string title = "";
			int length;
			string[] lines;

			maxLeigth = Console.WindowWidth - 1;
			
			if (ProductProgress > 0) {
				string a;
				a = "".PadRight(maxLeigth * Console.WindowHeight - indicator); 
				Console.SetCursorPosition(0, indicator);
				Console.WriteLine(a);
			}

			Console.SetCursorPosition(0, indicator);
			Console.Write(product.LinkToProduct);


			indicator += 4;
			
			length = maxLeigth - (_marginHoriz * 2);
			lines = SplitIntoLines(product.Breadcrumbs, length, '|').Split('|');
			Console.SetCursorPosition(_marginHoriz, indicator);
			Console.Write(title);
			PrintMultiLines(lines, _marginHoriz + title.Length,indicator, '|');

			indicator += lines.Length + 2;


			title = "Имя продукта: ";
			length = maxLeigth - title.Length - (_marginHoriz * 2);
			lines = SplitIntoLines(product.Title, length, '|').Split('|');
			Console.SetCursorPosition(_marginHoriz, indicator);
			Console.Write(title);
			PrintMultiLines(lines, _marginHoriz + title.Length, indicator, '|');

			indicator += lines.Length + 4;

			if (product.OldPrice != product.CurrentPrice)
			{
				title = "Старая цена: ";
				Console.SetCursorPosition(maxLeigth - title.Length - product.OldPrice.ToString().Length - _marginHoriz, indicator);
				Console.Write(title);
				Console.ForegroundColor= ConsoleColor.Green ;
				Console.Write(product.OldPrice);
				Console.ForegroundColor = ConsoleColor.White;

				indicator += 2;
			}

			

			title = "Цена: ";
			Console.SetCursorPosition(maxLeigth - title.Length - product.OldPrice.ToString().Length - _marginHoriz, indicator);
			Console.Write(title);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(product.CurrentPrice);
			Console.ForegroundColor = ConsoleColor.White;

			indicator += 2;

			ProductProgress++;
			_poductHeight = indicator;
			PrintProgressBar();
		}



		public void PrintProgressBar()
		{
			int indicator = _poductHeight + 4;

			Console.ForegroundColor = ConsoleColor.Blue;

			double ratio = 100d / ProductCount;
			double progress = (int)(ratio * ProductProgress);

			double symbolRatio = (double)(Console.WindowWidth - 1 - _marginHoriz * 2) / ProductCount;
			int symbolProgress = (int)(symbolRatio * ProductProgress);
			
			Console.SetCursorPosition(10, indicator);
			Console.Write($"Прогресс... {progress}/100% | {ProductProgress}/{ProductCount}");

			indicator++;

			Console.SetCursorPosition(10, indicator);
			Console.WriteLine("".PadRight(symbolProgress, _progressBarSymbol));
			Console.ForegroundColor = ConsoleColor.White;
		}
		
		private static string SplitIntoLines(string text, int lineMaxSize, char splitChar)
		{
			string[] words = text.Split(' ');
			StringBuilder sb = new();
			int currLength = 0;
			foreach (string word in words)
			{
				if (currLength + word.Length + 1 < lineMaxSize)
				{
					sb.AppendFormat(" {0}", word);
					currLength = (sb.Length % lineMaxSize);
				}
				else
				{
					sb.AppendFormat("{0}{1}", splitChar, word);
					currLength = 0;
				}
			}
			return sb.ToString().TrimStart();
		}

		private static void PrintMultiLines(string[] lines,int cursorPositionX, int indicator, char separator)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			for (int i = 0; i < lines.Length; i++)
			{
				Console.SetCursorPosition(cursorPositionX, indicator);
				Console.Write(lines[i].Trim(separator));
				indicator += 1;
			}
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}
