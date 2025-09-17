using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 5 expert advisor "Open Close" (ticket 23090).
/// Recreates the open/close relationship checks and adaptive lot sizing.
/// </summary>
public class OpenClose23090Strategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRiskPercent;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _historyDays;
	private readonly StrategyParam<decimal> _fallbackVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal Open, decimal Close)> _recentCandles = new(capacity: 2);
	private readonly List<TradeResult> _closedResults = new();

	private decimal _signedPosition;
	private Sides? _entrySide;
	private decimal _entryVolume;
	private decimal _entryCost;

	private struct TradeResult
	{
		public TradeResult(DateTimeOffset time, decimal profit)
		{
			Time = time;
			Profit = profit;
		}

		public DateTimeOffset Time { get; }

		public decimal Profit { get; }
	}

	public OpenClose23090Strategy()
	{
		_maximumRiskPercent = Param(nameof(MaximumRiskPercent), 0.02m)
			.SetDisplay("Maximum Risk Percent", "Fraction of equity committed per trade (0.02 = 2%).", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
			.SetDisplay("Decrease Factor", "Divider used when shrinking the lot after losing streaks.", "Risk");

		_historyDays = Param(nameof(HistoryDays), 60)
			.SetDisplay("History Days", "Lookback window for counting consecutive losses.", "Risk");

		_fallbackVolume = Param(nameof(FallbackVolume), 0.1m)
			.SetDisplay("Fallback Volume", "Order size used when risk-based sizing is unavailable.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame that supplies the open/close sequence.", "Data");
	}

	/// <summary>
	/// Fraction of account equity risked on each entry.
	/// </summary>
	public decimal MaximumRiskPercent
	{
		get => _maximumRiskPercent.Value;
		set => _maximumRiskPercent.Value = value;
	}

	/// <summary>
	/// Divisor applied to the lot when multiple losses occur without a break-even trade.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Number of calendar days inspected when counting the latest losing streak.
	/// </summary>
	public int HistoryDays
	{
		get => _historyDays.Value;
		set => _historyDays.Value = value;
	}

	/// <summary>
	/// Safety volume applied whenever the risk model cannot produce a value.
	/// </summary>
	public decimal FallbackVolume
	{
		get => _fallbackVolume.Value;
		set => _fallbackVolume.Value = value;
	}

	/// <summary>
	/// Candle data source that feeds the pattern recognition block.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_recentCandles.Clear();
		_closedResults.Clear();

		_signedPosition = 0m;
		_entrySide = null;
		_entryVolume = 0m;
		_entryCost = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (previousPosition == 0m && _signedPosition != 0m)
		{
			// Record entry details for profit computation.
			_entrySide = trade.Order.Side;
			_entryVolume = volume;
			_entryCost = trade.Trade.Price * volume;
		}
		else if (_signedPosition != 0m && trade.Order.Side == _entrySide)
		{
			// Average the entry price if the same-side order gets filled in parts.
			_entryCost += trade.Trade.Price * volume;
			_entryVolume += volume;
		}
		else if (previousPosition != 0m && _signedPosition == 0m)
		{
			if (_entrySide != null && _entryVolume > 0m)
			{
				var averageEntryPrice = _entryCost / _entryVolume;
				var exitPrice = trade.Trade.Price;
				var direction = _entrySide == Sides.Buy ? 1m : -1m;
				var profit = (exitPrice - averageEntryPrice) * _entryVolume * direction;

				_closedResults.Add(new TradeResult(trade.Trade.Time, profit));
			}

			_entrySide = null;
			_entryVolume = 0m;
			_entryCost = 0m;

			CleanupHistory(trade.Trade.Time);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		CleanupHistory(candle.CloseTime);

		if (_recentCandles.Count < 2)
		{
			_recentCandles.Add((candle.OpenPrice, candle.ClosePrice));
			return;
		}

		var older = _recentCandles[0];
		var previous = _recentCandles[1];

		if (Position == 0m)
		{
			TryEnter(previous, older);
		}
		else
		{
			TryExit(previous, older);
		}

		_recentCandles.RemoveAt(0);
		_recentCandles.Add((candle.OpenPrice, candle.ClosePrice));
	}

	private void TryEnter((decimal Open, decimal Close) previous, (decimal Open, decimal Close) older)
	{
		if (Security == null)
			return;

		if (previous.Open > older.Open && previous.Close < older.Close)
		{
			var volume = CalculateOrderVolume(previous.Close);
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (previous.Open < older.Open && previous.Close > older.Close)
		{
			var volume = CalculateOrderVolume(previous.Close);
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private void TryExit((decimal Open, decimal Close) previous, (decimal Open, decimal Close) older)
	{
		if (Position > 0m && previous.Open < older.Open && previous.Close < older.Close)
			SellMarket(Position);
		else if (Position < 0m && previous.Open > older.Open && previous.Close > older.Close)
			BuyMarket(-Position);
	}

	private decimal CalculateOrderVolume(decimal referencePrice)
	{
		var volume = FallbackVolume;
		var accountValue = Portfolio?.CurrentValue ?? 0m;

		if (accountValue > 0m && referencePrice > 0m && MaximumRiskPercent > 0m)
		{
			volume = accountValue * MaximumRiskPercent / referencePrice;
		}

		var lossStreak = GetRecentLossStreak();
		if (DecreaseFactor > 0m && lossStreak > 1)
		{
			var decrease = volume * lossStreak / DecreaseFactor;
			volume -= decrease;
		}

		if (volume <= 0m)
			return 0m;

		volume = Math.Round(volume, 2, MidpointRounding.AwayFromZero);

		var volumeStep = Security?.VolumeStep ?? 0m;
		if (volumeStep > 0m)
		{
			var steps = Math.Floor(volume / volumeStep);
			if (steps < 1m)
				steps = 1m;
			volume = steps * volumeStep;
		}

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security?.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private int GetRecentLossStreak()
	{
		if (_closedResults.Count == 0)
			return 0;

		var limit = DateTimeOffset.MinValue;
		if (HistoryDays > 0)
			limit = CurrentTime - TimeSpan.FromDays(HistoryDays);

		var losses = 0;

		for (var i = _closedResults.Count - 1; i >= 0; i--)
		{
			var result = _closedResults[i];
			if (result.Time < limit)
				break;

			if (result.Profit > 0m)
				break;

			if (result.Profit < 0m)
				losses++;
		}

		return losses;
	}

	private void CleanupHistory(DateTimeOffset referenceTime)
	{
		if (_closedResults.Count == 0)
			return;

		if (HistoryDays <= 0)
			return;

		var limit = referenceTime - TimeSpan.FromDays(HistoryDays);
		_closedResults.RemoveAll(r => r.Time < limit);
	}
}
