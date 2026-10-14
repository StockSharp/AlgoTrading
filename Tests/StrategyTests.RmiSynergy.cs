namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	private const string RmiSynergy = "1183_PresentTrend_RMI_Synergy";

	private sealed class RmiSynergyReference(int rmiPeriod, int momentum, int meanAtrPeriod, decimal multiplier)
	{
		private readonly List<decimal> _closes = [];
		private readonly List<decimal> _changes = [];
		private decimal _atr;

		public bool IsFormed { get; private set; }
		public bool IsPriceFormed { get; private set; }
		public decimal Rmi { get; private set; }
		public decimal Lower { get; private set; }
		public decimal Upper { get; private set; }
		public Sides? Entry { get; private set; }
		public int RejectedAlignment { get; private set; }
		public int RejectedMomentum { get; private set; }

		public void Add(ICandleMessage candle)
		{
			Entry = null;
			var trueRange = candle.HighPrice - candle.LowPrice;
			if (_closes.Count > 0)
				trueRange = Math.Max(trueRange, Math.Max(Math.Abs(candle.HighPrice - _closes[^1]), Math.Abs(candle.LowPrice - _closes[^1])));
			_closes.Add(candle.ClosePrice);
			var count = Math.Min(_closes.Count, meanAtrPeriod);
			_atr = (_atr * (count - 1) + trueRange) / count;
			if (_closes.Count > momentum)
				_changes.Add(candle.ClosePrice - _closes[_closes.Count - 1 - momentum]);
			IsPriceFormed = _closes.Count >= meanAtrPeriod;
			if (!IsPriceFormed)
				return;
			var mean = _closes.Skip(_closes.Count - meanAtrPeriod).Average();
			Lower = mean - multiplier * _atr;
			Upper = mean + multiplier * _atr;
			if (_changes.Count < rmiPeriod)
				return;
			var movements = _changes.Skip(_changes.Count - rmiPeriod).ToArray();
			var positive = movements.Where(value => value > 0m).Sum();
			var negative = movements.Where(value => value < 0m).Sum(value => -value);
			IsFormed = positive + negative > 0m;
			if (!IsFormed)
				return;
			Rmi = 100m * positive / (positive + negative);
			if (Rmi > 60m && candle.ClosePrice > mean)
				Entry = Sides.Buy;
			else if (Rmi < 40m && candle.ClosePrice < mean)
				Entry = Sides.Sell;
			else if (Rmi > 60m || Rmi < 40m)
				RejectedAlignment++;
			else
				RejectedMomentum++;
		}
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S1183_FlatEntriesNeedRealRmiAndPriceAlignment()
	{
		var reference = new RmiSynergyReference(21, 3, 5, 4m);
		var entries = 0;
		var invalid = 0;
		await Replay(RmiSynergy, (strategy, _) =>
		{
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State == CandleStates.Finished)
					reference.Add(candle);
			};
			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0m)
					return;
				entries++;
				if (order.Side != reference.Entry || order.Volume != strategy.Volume)
					invalid++;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(entries > 0);
		AreEqual(0, invalid, "Flat entries need native RMI21 with three-bar momentum and SMA5 price alignment, not an EMA crossover.");
	}

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(false, 21, 3, 5, 4.0, null)]
	[DataRow(true, 7, 2, 10, 2.0, null)]
	[DataRow(false, 21, 3, 5, 4.0, Sides.Buy)]
	[DataRow(true, 7, 2, 10, 2.0, Sides.Sell)]
	[DataRow(false, 21, 3, 5, 0.1, null)]
	[DataRow(false, 7, 1, 5, 0.2, Sides.Sell)]
	public async Task S1183_EveryRmiEntryAndCausalSmaAtrTrailingExit(bool secondary, int rmiPeriod, int momentum,
		int meanAtrPeriod, double multiplier, Sides? direction)
	{
		var reference = new RmiSynergyReference(rmiPeriod, momentum, meanAtrPeriod, (decimal)multiplier);
		var violations = new List<string>();
		var entries = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var longExits = 0;
		var shortExits = 0;
		var intrabarExits = 0;
		var ratchets = 0;
		var heldOppositeSignals = 0;
		var nearEntries = 0;
		var position = 0m;
		decimal? stop = null;
		Sides? expectedEntry = null;
		var isExpectedExit = false;
		DateTime? lastEntry = null;
		DateTime? currentCandleTime = null;
		Order pending = null;

		bool isPending() => pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed);
		void observePrice(decimal price)
		{
			if (isExpectedExit || position == 0m || stop is not decimal level || isPending())
				return;
			isExpectedExit = position > 0m ? price <= level : price >= level;
		}

		await Replay(RmiSynergy, (strategy, other) =>
		{
			if (secondary)
				strategy.Security = other;
			foreach (var (name, value) in new (string, object)[]
			{
				("RmiPeriod", 21), ("RmiMomentum", 3), ("SuperTrendLength", 5), ("SuperTrendMultiplier", 4m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"RMI Synergy needs the published {name} parameter.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			IsTrue(strategy.Parameters.ContainsKey("Direction"));
			IsTrue(strategy.Parameters["Direction"].Value is null, "Both directions is an absent Sides filter.");
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "RmiPeriod", rmiPeriod);
			SetParam(strategy, "RmiMomentum", momentum);
			SetParam(strategy, "SuperTrendLength", meanAtrPeriod);
			SetParam(strategy, "SuperTrendMultiplier", (decimal)multiplier);
			if (direction is Sides side)
				SetParam(strategy, "Direction", side);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedEntry is not null || isExpectedExit)
					violations.Add("A qualifying RMI entry or triggered trailing stop was missed.");
				expectedEntry = null;
				currentCandleTime = strategy.CurrentTime;
				reference.Add(candle);
				if (!reference.IsPriceFormed)
					return;
				if (position != 0m)
				{
					if (reference.Entry is Sides signal && (signal == Sides.Buy ? position < 0m : position > 0m))
						heldOppositeSignals++;
					var candidate = position > 0m ? reference.Lower : reference.Upper;
					var updated = stop is decimal previous ? position > 0m ? Math.Max(previous, candidate) : Math.Min(previous, candidate) : candidate;
					if (stop != updated)
						ratchets++;
					stop = updated;
					observePrice(candle.ClosePrice);
					return;
				}
				if (isPending() || reference.Entry is not Sides entry || direction is Sides allowed && entry != allowed)
					return;
				expectedEntry = entry;
				stop = entry == Sides.Buy ? reference.Lower : reference.Upper;
			};
			strategy.Level1Received += (_, quote) =>
			{
				var field = position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
				if (quote.TryGetDecimal(field) is decimal price && price > 0m)
					observePrice(price);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Type != OrderTypes.Market || strategy.Position != position)
					violations.Add("Every order must be market and agree with actual signed fills.");
				if (position == 0m)
				{
					entries++;
					if (order.Side != expectedEntry || order.Volume != strategy.Volume || isExpectedExit)
						violations.Add($"Unexpected entry {order.Side} {order.Volume}; expected {expectedEntry} on real RMI/SMA.");
					if (order.Side == Sides.Buy)
						buys++;
					else
						sells++;
					if (lastEntry is DateTime last && strategy.CurrentTime - last < TimeSpan.FromHours(6))
						nearEntries++;
					lastEntry = strategy.CurrentTime;
					expectedEntry = null;
				}
				else
				{
					exits++;
					if (!isExpectedExit || order.Side != (position > 0m ? Sides.Sell : Sides.Buy)
						|| order.Volume != Math.Abs(position) || order.Comment != "RMI ATR trailing stop")
						violations.Add($"{strategy.CurrentTime:O}: exit must close the whole held side at the independently calculated ratcheted SMA/ATR stop {stop}.");
					if (position > 0m)
						longExits++;
					else
						shortExits++;
					if (strategy.CurrentTime != currentCandleTime)
						intrabarExits++;
					isExpectedExit = false;
				}
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) => position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"{RmiSynergy}: {entries} entries ({buys}/{sells}), {exits} stops ({longExits}/{shortExits}), {intrabarExits} intrabar, {ratchets} stop ratchets, {heldOppositeSignals} opposite signals held, {nearEntries} entries <6h, {reference.RejectedAlignment}/{reference.RejectedMomentum} alignment/momentum rejections.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedEntry is null && !isExpectedExit);
		IsTrue(entries > 0 && exits > 0 && ratchets > 0 && nearEntries > 0
			&& reference.RejectedAlignment > 0 && reference.RejectedMomentum > 0);
		if (direction is null)
			IsTrue(buys > 0 && sells > 0 && longExits > 0 && shortExits > 0);
		else
			IsTrue(direction == Sides.Buy ? buys > 0 && sells == 0 : sells > 0 && buys == 0);
		if (multiplier >= 2.0)
			IsTrue(heldOppositeSignals > 0, "Opposite entry filters cannot replace the documented trailing-stop exit.");
		if (multiplier <= 0.2)
			IsTrue(intrabarExits > 0, "Tight trailing stops must react to actual executable quotes between finished candles.");
	}
}
