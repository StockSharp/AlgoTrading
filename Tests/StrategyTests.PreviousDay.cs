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
	private const string PreviousDay = "1184_Previous_Day_High_Low_Long";

	private sealed class PreviousDayReference(int adxLength)
	{
		private readonly List<(DateTime open, decimal high, decimal low, decimal close)> _bars = [];
		private readonly Dictionary<DateTime, List<(DateTime open, decimal high, decimal low, decimal close)>> _days = [];
		private decimal _atr;
		private decimal _plus;
		private decimal _minus;
		private decimal _adx;
		private int _dxCount;
		private decimal? _previousAdx;

		public bool IsHighCross { get; private set; }
		public bool IsLowCross { get; private set; }
		public bool IsRisingAdx { get; private set; }
		public int RejectedAdx { get; private set; }
		public int MissingPreviousDays { get; private set; }

		public void Add(ICandleMessage candle)
		{
			IsHighCross = IsLowCross = IsRisingAdx = false;
			var prior = _bars.Count == 0 ? ((DateTime open, decimal high, decimal low, decimal close)?)null : _bars[^1];
			_bars.Add((candle.OpenTime, candle.HighPrice, candle.LowPrice, candle.ClosePrice));
			if (!_days.TryGetValue(candle.OpenTime.Date, out var day))
				_days.Add(candle.OpenTime.Date, day = []);
			day.Add(_bars[^1]);
			var trueRange = candle.HighPrice - candle.LowPrice;
			if (prior is { } previous)
			{
				trueRange = Math.Max(trueRange, Math.Max(Math.Abs(candle.HighPrice - previous.close), Math.Abs(candle.LowPrice - previous.close)));
				var up = candle.HighPrice - previous.high;
				var down = previous.low - candle.LowPrice;
				var count = Math.Min(_bars.Count - 1, adxLength);
				_plus = (_plus * (count - 1) + (up > 0m && up > down ? up : 0m)) / count;
				_minus = (_minus * (count - 1) + (down > 0m && down > up ? down : 0m)) / count;
			}
			var atrCount = Math.Min(_bars.Count, adxLength);
			_atr = (_atr * (atrCount - 1) + trueRange) / atrCount;
			decimal? formedAdx = null;
			if (_bars.Count >= adxLength + 2)
			{
				var plus = _atr == 0m ? 0m : 100m * _plus / _atr;
				var minus = _atr == 0m ? 0m : 100m * _minus / _atr;
				var dx = plus + minus == 0m ? 0m : 100m * Math.Abs(plus - minus) / (plus + minus);
				var count = Math.Min(++_dxCount, adxLength);
				_adx = (_adx * (count - 1) + dx) / count;
				if (_dxCount >= adxLength)
					formedAdx = _adx;
			}
			IsRisingAdx = formedAdx is decimal current && _previousAdx is decimal old && current > old;
			if (formedAdx is not null)
				_previousAdx = formedAdx;
			var yesterday = candle.OpenTime.Date.AddDays(-1);
			if (prior is null || !_days.TryGetValue(yesterday, out var previousDay) || previousDay[0].open.TimeOfDay != TimeSpan.Zero)
			{
				MissingPreviousDays++;
				return;
			}
			var high = previousDay.Max(bar => bar.high);
			var low = previousDay.Min(bar => bar.low);
			IsHighCross = prior.Value.close <= high && candle.ClosePrice > high;
			IsLowCross = prior.Value.close <= low && candle.ClosePrice > low;
			if ((IsHighCross || IsLowCross) && !IsRisingAdx)
				RejectedAdx++;
		}
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S1184_ActualEntriesNeedPreviousDayBreakoutAndRisingAdx()
	{
		var reference = new PreviousDayReference(11);
		var invalid = 0;
		var entries = 0;
		await Replay(PreviousDay, (strategy, _) =>
		{
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State == CandleStates.Finished)
					reference.Add(candle);
			};
			strategy.OrderRegistering += order =>
			{
				if (strategy.Position == 0m)
				{
					entries++;
					if (order.Side != Sides.Buy || !reference.IsRisingAdx || !reference.IsHighCross && !reference.IsLowCross
						|| strategy.CurrentTime.TimeOfDay < TimeSpan.FromMinutes(570) || strategy.CurrentTime.TimeOfDay >= TimeSpan.FromHours(16))
						invalid++;
				}
			};
		}, TimeSpan.FromDays(31));
		IsTrue(entries > 0);
		AreEqual(0, invalid, "Flat EMA entries are not long-only previous-day high/low crosses with rising ADX inside the declared UTC session.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false, 11, 150.0, 15.0, 570, 960)]
	[DataRow(true, 7, 0.05, 0.02, 0, 1440)]
	[DataRow(false, 11, 100000.0, 100000.0, 570, 960)]
	[DataRow(false, 7, 150.0, 0.5, 0, 1440)]
	[DataRow(false, 11, 0.5, 150.0, 0, 1440)]
	public async Task S1184_EveryDayLevelEntryTrailingProfitAndSessionClose(bool secondary, int adxLength,
		double maxProfit, double maxStopLoss, int startMinutes, int endMinutes)
	{
		var reference = new PreviousDayReference(adxLength);
		var violations = new List<string>();
		var entries = 0;
		var highEntries = 0;
		var lowEntries = 0;
		var stops = 0;
		var takes = 0;
		var sessionExits = 0;
		var intrabarExits = 0;
		var rejectedSession = 0;
		var position = 0m;
		var basis = 0m;
		var extreme = 0m;
		var isExpectedEntry = false;
		string expectedExit = null;
		DateTime? entryDate = null;
		DateTime? lastCandleTime = null;
		Order pending = null;
		var start = TimeSpan.FromMinutes(startMinutes);
		var end = TimeSpan.FromMinutes(endMinutes);

		bool isPending() => pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed);
		bool isSessionOver(DateTime time) => entryDate is DateTime date && (time.Date > date || time.TimeOfDay >= end);
		void observePrice(DateTime time, decimal price)
		{
			if (expectedExit is not null || position <= 0m || isPending())
				return;
			if (isSessionOver(time))
				expectedExit = "Previous-day session close";
			else
			{
				extreme = Math.Max(extreme, price);
				if (price <= extreme - (decimal)maxStopLoss)
					expectedExit = "Previous-day trailing stop";
				else if (price >= basis + (decimal)maxProfit)
					expectedExit = "Previous-day profit target";
			}
		}

		await Replay(PreviousDay, (strategy, other) =>
		{
			if (secondary)
				strategy.Security = other;
			foreach (var (name, value) in new (string, object)[]
			{
				("MaxProfit", 150m), ("MaxStopLoss", 15m), ("AdxLength", 11),
				("SessionStart", TimeSpan.FromMinutes(570)), ("SessionEnd", TimeSpan.FromHours(16)),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"Previous-day breakout needs {name}.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			SetParam(strategy, "MaxProfit", (decimal)maxProfit);
			SetParam(strategy, "MaxStopLoss", (decimal)maxStopLoss);
			SetParam(strategy, "AdxLength", adxLength);
			SetParam(strategy, "SessionStart", start);
			SetParam(strategy, "SessionEnd", end);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (isExpectedEntry || expectedExit is not null)
					violations.Add("A qualifying breakout entry or triggered exit was missed.");
				isExpectedEntry = false;
				lastCandleTime = strategy.CurrentTime;
				reference.Add(candle);
				observePrice(strategy.CurrentTime, candle.ClosePrice);
				if (position != 0m || isPending() || expectedExit is not null || !reference.IsRisingAdx
					|| !reference.IsHighCross && !reference.IsLowCross)
					return;
				if (strategy.CurrentTime.TimeOfDay < start || strategy.CurrentTime.TimeOfDay >= end)
				{
					rejectedSession++;
					return;
				}
				isExpectedEntry = true;
			};
			strategy.Level1Received += (_, quote) =>
			{
				if (quote.TryGetDecimal(Level1Fields.BestBidPrice) is decimal price && price > 0m)
					observePrice(strategy.CurrentTime, price);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Type != OrderTypes.Market || strategy.Position != position)
					violations.Add("Orders must be market and match actual signed fills.");
				if (order.Side == Sides.Buy)
				{
					entries++;
					if (!isExpectedEntry || position != 0m || order.Volume != strategy.Volume || expectedExit is not null)
						violations.Add("A buy must be a session-qualified previous-day cross with rising independent ADX, from flat.");
					if (reference.IsHighCross)
						highEntries++;
					else if (reference.IsLowCross)
						lowEntries++;
					entryDate = strategy.CurrentTime.Date;
					isExpectedEntry = false;
				}
				else
				{
					if (expectedExit is null && isSessionOver(strategy.CurrentTime))
						expectedExit = "Previous-day session close";
					if (position <= 0m || order.Volume != position || order.Comment != expectedExit || expectedExit is null)
						violations.Add($"{strategy.CurrentTime:O}: {order.Comment}; expected {expectedExit} from actual fills, observed prices and UTC session.");
					if (order.Comment == "Previous-day trailing stop")
						stops++;
					else if (order.Comment == "Previous-day profit target")
						takes++;
					else if (order.Comment == "Previous-day session close")
						sessionExits++;
					if (strategy.CurrentTime != lastCandleTime)
						intrabarExits++;
					expectedExit = null;
				}
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				var priorPosition = position;
				var volume = trade.Trade.Volume;
				position += trade.Order.Side == Sides.Buy ? volume : -volume;
				if (position < 0m)
					violations.Add("No fill may open a short position.");
				if (trade.Order.Side == Sides.Buy)
				{
					basis = (basis * priorPosition + trade.Trade.Price * volume) / position;
					extreme = priorPosition == 0m ? trade.Trade.Price : Math.Max(extreme, trade.Trade.Price);
				}
				if (position == 0m)
					basis = extreme = 0m;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"{PreviousDay}: {entries} entries (high/low {highEntries}/{lowEntries}), {stops} trailing/{takes} profit/{sessionExits} session exits, {intrabarExits} intrabar, {reference.RejectedAdx} ADX and {rejectedSession} session rejections, {reference.MissingPreviousDays} bars without known previous day.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(!isExpectedEntry && expectedExit is null);
		IsTrue(entries > 0 && reference.RejectedAdx > 0 && reference.MissingPreviousDays > 0);
		if (maxStopLoss <= 15.0)
			IsTrue(stops > 0 && intrabarExits > 0);
		if (maxProfit <= 0.5)
			IsTrue(takes > 0);
		if (maxStopLoss >= 100000.0)
			IsTrue(sessionExits > 0 && stops == 0 && takes == 0);
		if (startMinutes != 0)
			IsTrue(rejectedSession > 0);
		if (!secondary && maxStopLoss <= 0.5)
			IsTrue(highEntries > 0 && lowEntries > 0, "Both previous-day resistance and low recrosses must be exercised.");
	}
}
