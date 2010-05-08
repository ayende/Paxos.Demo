using System;
using System.Collections.Generic;
using System.Linq;
using Paxos.Commands;
using Paxos.Messages;

namespace Paxos.Agents
{
	public class Proposer : Agent
	{
		public const int BallotStep = 1000;
		private readonly Acceptor[] acceptors; // this include our acceptor;
		private readonly int ballotBase;
		private readonly Acceptor myAcceptor;
		private readonly Dictionary<int, ProposalState> proposalsState = new Dictionary<int, ProposalState>();
		private int proposalNumber;

		public Proposer(Acceptor myAcceptor, Acceptor[] acceptors, int ballotBase)
		{
			this.myAcceptor = myAcceptor;
			this.ballotBase = ballotBase;
			this.acceptors = acceptors;

			Register<StartProposing>(OnStartProposing);
			Register<Promise>(OnPromise);
			Register<ProposalSubsumed>(OnProposalSubsumed);
			Register<Accepted>(OnAccepted);
		}

		private void OnAccepted(Accepted accepted)
		{
			ProposalState state;
			if (proposalsState.TryGetValue(accepted.ProposalNumber, out state) == false)
				return;
			state.NumberOfAccepts += 1;
			if (state.NumberOfAccepts <= acceptors.Length/2)
				return;
			proposalsState.Remove(state.ProposalNumber);
		}

		private void OnStartProposing(StartProposing startProposing)
		{
			var state = new ProposalState
			{
				InitialValue = startProposing.Value,
				BallotNumber = BallotStep + ballotBase,
				ProposalNumber = GenerateNextProposalNumber(),
				LastMessage = DateTime.Now
			};
			proposalsState[state.ProposalNumber] = state;
			foreach (var acceptor in acceptors)
			{
				acceptor.SendMessage(new Propose
				{
					Originator = this,
					ProposalNumber = state.ProposalNumber,
					BallotNumber = state.BallotNumber
				});
			}
		}

		private void OnProposalSubsumed(ProposalSubsumed proposalSubsumed)
		{
			ProposalState state;
			if (proposalsState.TryGetValue(proposalSubsumed.ProposalNumber, out state) == false)
				return; // delayed / duplicate message, probably
			if (state.BallotNumber > proposalSubsumed.BallotNumber)
				return; // probably already suggested higher number
			state.BallotNumber += BallotStep;
			state.NumberOfPromises = 0;
			state.NumberOfAccepts = 0;
			state.LastMessage = DateTime.Now;
			state.QuorumReached = false;
			foreach (var acceptor in acceptors)
			{
				acceptor.SendMessage(new Propose
				{
					Originator = this,
					ProposalNumber = state.ProposalNumber,
					BallotNumber = state.BallotNumber
				});
			}
		}

		private void OnPromise(Promise promise)
		{
			ProposalState state;
			if (proposalsState.TryGetValue(promise.ProposalNumber, out state) == false)
				return; // delayed / duplicate message, probably

			if (state.BallotNumber != promise.BallotNumber)
				return;

			if (promise.AcceptedValue != null)
				state.ValuesToBeChoosen.Add(promise.AcceptedValue);

			state.NumberOfPromises++;
			state.LastMessage = DateTime.Now;

			if (state.QuorumReached)
				return;
			if (state.NumberOfPromises <= acceptors.Length/2)
				return;
			state.QuorumReached = true;
			state.ChosenValue = state.ValuesToBeChoosen.FirstOrDefault() ?? state.InitialValue;
			foreach (var acceptor in acceptors)
			{
				acceptor.SendMessage(new Accept
				{
					Originator = this,
					ProposalNumber = state.ProposalNumber,
					Value = state.ChosenValue,
					BallotNumber = state.BallotNumber
				});
			}
			// if we selected a different value
			if (state.ChosenValue != state.InitialValue)
			{
				//restart the whole things
				proposalsState.Remove(state.ProposalNumber);
				Propose(state.InitialValue);
			}
		}

		private int GenerateNextProposalNumber()
		{
			if (myAcceptor.AcceptedProposalNumber > proposalNumber)
				proposalNumber = (myAcceptor.AcceptedProposalNumber + 1);
			else
				proposalNumber = (proposalNumber + 1);

			return proposalNumber;
		}

		public void Propose(ICommand command)
		{
			SendMessage(new StartProposing
			{
				Originator = this,
				Value = command
			});
		}

		public override bool ProcessTimeouts()
		{
			var suggested = false;
			foreach (var timedOutPropsalState in proposalsState.Values.Where(x => x.LastMessage.AddSeconds(1) < DateTime.Now))
			{
				suggested = true;
				foreach (var acceptor in acceptors)
				{
					acceptor.SendMessage(new Propose
					{
						Originator = this,
						ProposalNumber = timedOutPropsalState.ProposalNumber,
						BallotNumber = timedOutPropsalState.BallotNumber
					});
				}
			}
			return suggested;
		}

		#region Nested type: ProposalState

		private class ProposalState
		{
			public ProposalState()
			{
				ValuesToBeChoosen = new List<ICommand>();
			}

			public bool QuorumReached { get; set; }
			public int ProposalNumber { get; set; }
			public int BallotNumber { get; set; }
			public int NumberOfPromises { get; set; }
			public List<ICommand> ValuesToBeChoosen { get; set; }
			public ICommand InitialValue { get; set; }
			public DateTime LastMessage { get; set; }

			public ICommand ChosenValue { get; set; }

			public int NumberOfAccepts { get; set; }
		}

		#endregion

		#region Nested type: StartProposing

		private class StartProposing : Message
		{
			public ICommand Value { get; set; }
		}

		#endregion
	}
}