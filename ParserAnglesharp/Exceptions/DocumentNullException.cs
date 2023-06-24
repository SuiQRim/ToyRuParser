using System.Runtime.Serialization;

namespace ToysRuParser
{
	[Serializable]
	internal class DocumentNullException : Exception
	{
		public DocumentNullException() : base("Html страница не загрузилась. Документ пуст.")
		{
		}

		public DocumentNullException(string? message) : base(message)
		{
		}

		public DocumentNullException(string? message, Exception? innerException) : base(message, innerException)
		{
		}

		protected DocumentNullException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}