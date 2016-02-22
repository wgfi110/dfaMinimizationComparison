﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DfaMinComparisonCSharp.CSharp
{
	public class DFA
	{
		private readonly List<Transition> transitions = new List<Transition>();
		private readonly BitArray stateIsFinal;

		public DFA(int stateCount, int startState)
		{
			StateCount = stateCount;
			stateIsFinal = new BitArray(stateCount);
			StartState = startState;
		}

		public IReadOnlyList<Transition> Transitions => transitions;
		public int StateCount { get; }
		public int StartState { get; }
		public IEnumerable<int> States => Enumerable.Range(0, StateCount).Select(i => i);

		public void AddTransition(int fromState, int onInput, int toState)
		{
			transitions.Add(new Transition(fromState, onInput, toState));
		}

		public DFA Minimize()
		{
			var blocks = new Partition(StateCount);

			// Reachable from start
			blocks.Mark(StartState);

			DiscardNotReachable(blocks, transitions, t => t.From, t => t.To);

			// Reachable from final
			var finalStates = States.Where(IsFinal).ToList();
			foreach(var finalState in finalStates)
				blocks.Mark(finalState);

			DiscardNotReachable(blocks, transitions, t => t.To, t => t.From);

			// Split final states from non-final
			foreach(var finalState in finalStates)
				blocks.Mark(finalState);

			blocks.SplitSets();

			// Cords partition to manage transitions
			var cords = new Partition(transitions.Count);

			// Sort transitions by input
			cords.PartitionBy(transition => transitions[transition].OnInput);

			//Split blocks and cords
			var adjacentTransitions = new AdjacentTransitions(StateCount, transitions, t => t.To);
			var blockSet = 1;
			for(var cordSet = 0; cordSet < cords.SetCount; cordSet++)
			{
				foreach(var transition in cords.Set(cordSet))
					blocks.Mark(transitions[transition].From);

				blocks.SplitSets();

				for(; blockSet < blocks.SetCount; blockSet++)
				{
					foreach(var state in blocks.Set(blockSet))
						foreach(var transition in adjacentTransitions[state])
							cords.Mark(transition);

					cords.SplitSets();
				}
			}

			// Generate minimized DFA
			var minDFA = new DFA(blocks.SetCount, blocks.SetOf(StartState));

			// Set Final States
			for(var set = 0; set < blocks.SetCount; set++)
				// Sets are either all final or non-final states
				if(IsFinal(blocks.SomeElementOf(set)))
					minDFA.SetFinal(set);

			// Create transitions
			for(var set = 0; set < cords.SetCount; set++)
			{
				var transition = transitions[cords.SomeElementOf(set)];
				var @from = blocks.SetOf(transition.From);
				var to = blocks.SetOf(transition.To);
				minDFA.AddTransition(@from, transition.OnInput, to);
			}

			return minDFA;
		}

		private void DiscardNotReachable(Partition blocks, List<Transition> transitionList, Func<Transition, int> getFrom, Func<Transition, int> getTo)
		{
			var adjacentTransitions = new AdjacentTransitions(StateCount, transitionList, getFrom);

			foreach(var state in blocks.Marked(0))
				foreach(var transition in adjacentTransitions[state])
					blocks.Mark(getTo(transitionList[transition]));

			blocks.DiscardUnmarked();

			transitionList.RemoveAll(transition => blocks.SetOf(getFrom(transition)) == -1);
		}

		public void SetFinal(int state, bool isFinal = true)
		{
			stateIsFinal[state] = isFinal;
		}

		public bool IsFinal(int state)
		{
			return stateIsFinal[state];
		}
	}
}
