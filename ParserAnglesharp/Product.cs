﻿namespace ToysRuParser
{
	public class Product
	{
		public string? Title { get; set; }

		public string? Breadcrumbs { get; set; }
		
		public decimal CurrentPrice { get; set; }

		public decimal OldPrice { get; set; }

		public bool IsAvailable { get; set; }

		public string? LinkToProduct { get; set; }

		public string []? LinksToImages { get; set; }
	}
}
