using System;
using System.Linq;
using System.Threading;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using Paxos.Agents;
using Paxos.Commands;

namespace Paxos
{
	internal class Program
	{
		private static void Main()
		{
			//BasicConfigurator.Configure(new ConsoleAppender
			//{
			//    Layout = new PatternLayout("%logger - %message%newline")
			//});

			var learners = new[]
			{
				new Learner(acceptorsCount: 3),
				new Learner(acceptorsCount: 3),
				new Learner(acceptorsCount: 3),
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

			foreach (var agent in agents)
			{
				new Thread(agent.ExecuteMultiThreaded)
				{
					Name = agent.ToString(),
					IsBackground = true
				}.Start();
			}

			proposers[0].Propose(new Add {Value = 5});
			proposers[1].Propose(new Multiply {Value = 3});
			proposers[2].Propose(new Add {Value = 2});

			WaitForNewValues(learners);

			proposers[2].Propose(new Multiply {Value = 3});
			proposers[1].Propose(new Add {Value = 4});

			WaitForNewValues(learners);
		}

		private static void WaitForNewValues(Learner[] learners)
		{
			Console.WriteLine("Waiting for new values");
			var shouldStop = 0;
			ThreadPool.QueueUserWorkItem(state =>
			{
				while (Thread.VolatileRead(ref shouldStop) == 0)
				{
					foreach (var learner in learners)
					{
						Console.WriteLine("# of commands: {0}. Value: {1}", learner.Commands.Count, learner.AppliedValue);
					}
					Console.WriteLine("- - - - - - - - -");
					Thread.Sleep(1000);
				}
			});

			Console.ReadLine();
			Thread.VolatileWrite(ref shouldStop, 1);
		}

		private static void ConsumeAllMessages(Agent[] agents)
		{
			var hadMessages = true;
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