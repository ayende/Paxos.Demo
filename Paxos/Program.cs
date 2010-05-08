using System;
using System.Diagnostics;
using System.Threading;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using Paxos.Commands;
using System.Linq;

namespace Paxos
{
	class Program
	{
		static void Main()
		{
			//BasicConfigurator.Configure(new ConsoleAppender
			//{
			//    Layout = new PatternLayout(PatternLayout.DetailConversionPattern)
			//});

			var learners = new[]
			{
				new Learner(acceptorsCount:3),
				new Learner(acceptorsCount:3),
				new Learner(acceptorsCount:3),
			};
			var acceptors = new[]
			{
				new Acceptor(learners),
				new Acceptor(learners),
				new Acceptor(learners)
			};
			var proposers = new[]
			{
				new Proposer(acceptors[0], acceptors, 1),
				new Proposer(acceptors[1], acceptors, 2),
				new Proposer(acceptors[2], acceptors, 3),
			};

			var agents = acceptors.OfType<Agent>().Union(proposers).Union(learners).ToArray();

			proposers[0].Propose(new Add { Value = 5 });
			proposers[1].Propose(new Multiply { Value = 3 });
			proposers[2].Propose(new Add { Value = 2 });

			ConsumeAllMessages(agents);

			foreach (var learner in learners)
			{
				Debug.Assert(learner.Commands.Count == 3);
				Console.WriteLine(learner.AppliedValue);
			}

			proposers[2].Propose(new Multiply { Value = 3 });
			proposers[1].Propose(new Add { Value = 4 });
			ConsumeAllMessages(agents);

			foreach (var learner in learners)
			{
				Debug.Assert(learner.Commands.Count == 5);
				Console.WriteLine(learner.AppliedValue);
			}
		}

		private static void ConsumeAllMessages(Agent[] agents)
		{
			bool hadMessages = true;
			while (hadMessages)
			{
				hadMessages = false;
				foreach (var agent in agents)
				{
					if (agent.ConsumeAllMessages())
						hadMessages = true;
				}
				//if (hadMessages) 
				//    continue;
				//Thread.Sleep(TimeSpan.FromSeconds(2));//larger than the timeout period
				//foreach (var agent in agents)
				//{
				//    if (agent.ProcessTimeouts())
				//        hadMessages = true;
				//}
			}
			
		}
	}
}
