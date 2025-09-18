namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

public class MorningPullbackCorridorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _pullbackPoints;
	private readonly StrategyParam<decimal> _corridorOpenClosePoints;
	private readonly StrategyParam<decimal> _corridorPullbackPoints;
	private readonly StrategyParam<decimal> _longTakeProfitExtraPoints;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _tradeMinuteLimit;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _closeMinuteThreshold;
	private readonly StrategyParam<TimeSpan> _candleType;

	// Rolling storage for the latest finished candles.
	private readonly List<ICandleMessage> _candles = new();

	// Cached targets are managed manually to reflect asymmetric exits.
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	public MorningPullbackCorridorStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 5m)
			.SetDisplay("Take Profit (pts)", "Take profit distance measured in points.", "Risk")
			.SetCanOptimize(true, 1m, 1m, 20m);

		_stopLossPoints = Param(nameof(StopLossPoints), 55m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance measured in points.", "Risk")
			.SetCanOptimize(true, 5m, 5m, 200m);

		_pullbackPoints = Param(nameof(PullbackPoints), 18m)
			.SetDisplay("Pullback (pts)", "Target pullback depth in points.", "Logic")
			.SetCanOptimize(true, 5m, 1m, 60m);

		_corridorOpenClosePoints = Param(nameof(CorridorOpenClosePoints), 33m)
			.SetDisplay("Open/Close Corridor (pts)", "Required distance between open and close 29 bars apart.", "Logic")
			.SetCanOptimize(true, 5m, 1m, 80m);

		_corridorPullbackPoints = Param(nameof(CorridorPullbackPoints), 4m)
			.SetDisplay("Pullback Corridor (pts)", "Tolerance added or subtracted from the pullback level.", "Logic")
			.SetCanOptimize(true, 1m, 0.5m, 20m);

		_longTakeProfitExtraPoints = Param(nameof(LongTakeProfitExtraPoints), 3m)
			.SetDisplay("Long TP Extra (pts)", "Additional points added to long take profit compared to shorts.", "Risk")
			.SetCanOptimize(true, 1m, 0m, 10m);

		_tradeHour = Param(nameof(TradeHour), 5)
			.SetDisplay("Trade Hour", "Hour of the day (platform time) when entries are allowed.", "Schedule")
			.SetCanOptimize(false, 0, 0, 23);

		_tradeMinuteLimit = Param(nameof(TradeMinuteLimit), 3)
			.SetDisplay("Trade Minute Limit", "Last minute within the trade hour that can trigger entries.", "Schedule")
			.SetCanOptimize(false, 0, 0, 59);

		_closeHour = Param(nameof(CloseHour), 22)
			.SetDisplay("Close Hour", "Hour of the day after which open positions are force-closed.", "Schedule")
			.SetCanOptimize(false, 0, 0, 23);

		_closeMinuteThreshold = Param(nameof(CloseMinuteThreshold), 45)
			.SetDisplay("Close Minute Threshold", "Minute threshold inside the close hour that triggers forced exit.", "Schedule")
			.SetCanOptimize(false, 0, 0, 59);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations.", "General");
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal PullbackPoints
	{
		get => _pullbackPoints.Value;
		set => _pullbackPoints.Value = value;
	}

	public decimal CorridorOpenClosePoints
	{
		get => _corridorOpenClosePoints.Value;
		set => _corridorOpenClosePoints.Value = value;
	}

	public decimal CorridorPullbackPoints
	{
		get => _corridorPullbackPoints.Value;
		set => _corridorPullbackPoints.Value = value;
	}

	public decimal LongTakeProfitExtraPoints
	{
		get => _longTakeProfitExtraPoints.Value;
		set => _longTakeProfitExtraPoints.Value = value;
	}

	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}

	public int TradeMinuteLimit
	{
		get => _tradeMinuteLimit.Value;
		set => _tradeMinuteLimit.Value = value;
	}

	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	public int CloseMinuteThreshold
	{
		get => _closeMinuteThreshold.Value;
		set => _closeMinuteThreshold.Value = value;
	}

	public TimeSpan CandleType
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
		_candles.Clear();
		ResetTargets();
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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_candles.Add(candle);
		const int maxBuffer = 60;
		if (_candles.Count > maxBuffer)
		_candles.RemoveAt(0);

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		ManageOpenPosition(candle);
		// Check whether the current candle violated the active protection levels.

		ForceCloseAtEndOfDay(candle);
		// Enforce the daily flat rule before looking for new entries.

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m)
		return;

		const int referenceOffset = 29;
		if (_candles.Count <= referenceOffset + 1)
		return;

		var previousIndex = _candles.Count - 2;
		if (previousIndex < 0)
		return;

		var referenceIndex = _candles.Count - referenceOffset - 1;
		if (referenceIndex < 0)
		return;

		var previous = _candles[previousIndex];
		var reference = _candles[referenceIndex];

		// Derive the corridor boundaries from the rolling window without additional indicators.
		decimal lowestLow = decimal.MaxValue;
		decimal highestHigh = decimal.MinValue;
		for (var i = Math.Max(0, _candles.Count - referenceOffset); i < _candles.Count; i++)
		{
			var item = _candles[i];
			if (item.LowPrice < lowestLow)
			lowestLow = item.LowPrice;
			if (item.HighPrice > highestHigh)
			highestHigh = item.HighPrice;
		}

		// Replicate the original MQL price differences (Open[29], Close[1], etc.).
		var opCl = reference.OpenPrice - previous.ClosePrice;
		var clOp = previous.ClosePrice - reference.OpenPrice;
		var clLo = previous.ClosePrice - lowestLow;
		var hiCl = highestHigh - previous.ClosePrice;

		var corridorOc = CorridorOpenClosePoints * step;
		var corridorPullback = CorridorPullbackPoints * step;
		var pullback = PullbackPoints * step;

		var openTime = candle.OpenTime;
		var inTradeWindow = openTime.Hour == TradeHour && openTime.Minute <= TradeMinuteLimit;
		var allowedDay = openTime.DayOfWeek is not DayOfWeek.Monday and not DayOfWeek.Friday;

		if (!inTradeWindow || !allowedDay)
		return;

		var volume = Volume;
		if (volume <= 0m)
		return;

		var longSignal = (opCl > corridorOc && clLo < pullback - corridorPullback)
			|| (clOp > corridorOc && hiCl > pullback + corridorPullback);

		var shortSignal = (clOp > corridorOc && hiCl < pullback - corridorPullback)
			|| (opCl > corridorOc && clLo > pullback + corridorPullback);

		// Enter only once per day when all corridor conditions are met.
		if (longSignal)
		{
			BuyMarket(volume);
			SetTargets(candle.OpenPrice, step, true);
		}
		else if (shortSignal)
		{
			SellMarket(volume);
			SetTargets(candle.OpenPrice, step, false);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopLossPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Position);
				ResetTargets();
				return;
			}

			if (_takeProfitPrice is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				ResetTargets();
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);
			if (_stopLossPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(absPosition);
				ResetTargets();
				return;
			}

			if (_takeProfitPrice is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(absPosition);
				ResetTargets();
			}
		}
		else
		{
			ResetTargets();
		}
	}

	private void ForceCloseAtEndOfDay(ICandleMessage candle)
	{
		if (Position == 0m)
		// Nothing to close when the portfolio is already flat.
		return;

		var openTime = candle.OpenTime;
		if (openTime.Hour != CloseHour)
		return;

		if (openTime.Minute <= CloseMinuteThreshold)
		return;

		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		ResetTargets();
	}

	private void SetTargets(decimal entryPrice, decimal step, bool isLong)
	{
		var stopOffset = StopLossPoints * step;
		var takeOffset = (TakeProfitPoints + (isLong ? LongTakeProfitExtraPoints : 0m)) * step;

		_stopLossPrice = stopOffset > 0m
			? entryPrice + (isLong ? -stopOffset : stopOffset)
			: null;

		_takeProfitPrice = takeOffset > 0m
			? entryPrice + (isLong ? takeOffset : -takeOffset)
			: null;
	}

	private void ResetTargets()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}
}
