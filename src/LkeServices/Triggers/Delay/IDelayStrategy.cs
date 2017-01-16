using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LkeServices.Triggers.Delay
{
	internal interface IDelayStrategy
	{
		TimeSpan GetNextDelay(bool executionSucceeded);
	}
}
