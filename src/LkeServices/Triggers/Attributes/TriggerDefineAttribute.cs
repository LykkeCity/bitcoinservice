using System;

namespace LkeServices.Triggers.Attributes
{
	public class TriggerDefineAttribute : Attribute
	{
		public Type Type { get; }

		public TriggerDefineAttribute(Type type)
		{
			Type = type;
		}
	}
}
