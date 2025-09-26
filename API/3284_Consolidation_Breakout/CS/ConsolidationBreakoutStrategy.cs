namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;

public class ConsolidationBreakoutStrategy : Strategy
{
	private StrategyParam<TimeFrame> _candleType = null!;
	private StrategyParam<TimeFrame> _macdCandleType = null!;
	private StrategyParam<int> _fastMaPeriod = null!;
	private StrategyParam<int> _slowMaPeriod = null!;
	private StrategyParam<int> _momentumLength = null!;
	private StrategyParam<decimal> _momentumBuyThreshold = null!;
	private StrategyParam<decimal> _momentumSellThreshold = null!;
	private StrategyParam<decimal> _stopLossPips = null!;
	private StrategyParam<decimal> _takeProfitPips = null!;
	private StrategyParam<decimal> _tradeVolume = null!;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private ICandleMessage _previousCandle;
	private ICandleMessage _previousPreviousCandle;
	private decimal? _macdLine;
	private decimal? _signalLine;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	public ConsolidationBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for the breakout logic", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("MACD Timeframe", "Timeframe used for confirming trades with MACD", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetRange(1, 200)
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Trend");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetRange(1, 500)
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Trend");

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetRange(1, 200)
			.SetDisplay("Momentum Length", "Lookback used for the momentum filter", "Momentum");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetRange(0m, 10m)
			.SetDisplay("Momentum Buy Threshold", "Minimum momentum required for long trades", "Momentum");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetRange(0m, 10m)
			.SetDisplay("Momentum Sell Threshold", "Minimum momentum required for short trades", "Momentum");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetRange(0m, 1000m)
			.SetDisplay("Stop Loss (pips)", "Protective stop size expressed in price steps", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetRange(0m, 2000m)
			.SetDisplay("Take Profit (pips)", "Profit target size expressed in price steps", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetRange(0m, 100m)
			.SetDisplay("Trade Volume", "Order volume submitted on each breakout", "General");
	}

	public TimeFrame CandleType => _candleType.Value;
	public TimeFrame MacdCandleType => _macdCandleType.Value;
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public int MomentumLength { get => _momentumLength.Value; set => _momentumLength.Value = value; }
	public decimal MomentumBuyThreshold { get => _momentumBuyThreshold.Value; set => _momentumBuyThreshold.Value = value; }
	public decimal MomentumSellThreshold { get => _momentumSellThreshold.Value; set => _momentumSellThreshold.Value = value; }
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	yield return (Security, CandleType);
	yield return (Security, MacdCandleType);
	}

	protected override void OnReseted()
	{
	base.OnReseted();

	_previousCandle = null;
	_previousPreviousCandle = null;
	_macdLine = null;
	_signalLine = null;
	_stopLossPrice = null;
	_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	// Use the configured volume for market orders.
	Volume = TradeVolume;

	_fastMa = new LinearWeightedMovingAverage
	{
	Length = FastMaPeriod,
	CandlePrice = CandlePrice.Typical
	};

	_slowMa = new LinearWeightedMovingAverage
	{
	Length = SlowMaPeriod,
	CandlePrice = CandlePrice.Typical
	};

	_momentum = new Momentum
	{
	Length = MomentumLength
	};

	_macd = new MovingAverageConvergenceDivergenceSignal
	{
	Macd =
	{
	ShortMa = { Length = 12 },
	LongMa = { Length = 26 }
	},
	SignalMa = { Length = 9 }
	};

	// Subscribe to the primary timeframe and bind indicators to the handler.
	var primarySubscription = SubscribeCandles(CandleType);
	primarySubscription
	.Bind(_fastMa, _slowMa, _momentum, ProcessPrimaryCandle)
	.Start();

	var macdSubscription = SubscribeCandles(MacdCandleType);
	macdSubscription
	.BindEx(_macd, ProcessMacdCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, primarySubscription);
	DrawIndicator(area, _fastMa);
	DrawIndicator(area, _slowMa);
	DrawIndicator(area, _momentum);
	DrawOwnTrades(area);
	}
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
	if (!macdValue.IsFinal)
	return;

	if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
	return;

	if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
	return;

	_macdLine = macdLine;
	_signalLine = signalLine;
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update protective exits before evaluating fresh signals.
		ManageOpenPosition(candle);

		var hasHistory = _previousCandle is not null && _previousPreviousCandle is not null;
		// Make sure we have enough data and trading permissions before generating orders.
		var canTrade = hasHistory && IsFormedAndOnlineAndAllowTrading() && _macd.IsFormed && _macdLine is not null && _signalLine is not null;

		if (canTrade)
		{
			// Recreate the consolidation, trend, momentum, and MACD filters from the source EA.
			var consolidationBullish = _previousPreviousCandle!.LowPrice < _previousCandle!.HighPrice;
			var consolidationBearish = _previousCandle!.LowPrice < _previousPreviousCandle!.HighPrice;
			var fastAboveSlow = fastMaValue > slowMaValue;
			var fastBelowSlow = fastMaValue < slowMaValue;
			var macdBullish = (_macdLine > 0m && _macdLine > _signalLine) || (_macdLine < 0m && _macdLine > _signalLine);
			var macdBearish = (_macdLine > 0m && _macdLine < _signalLine) || (_macdLine < 0m && _macdLine < _signalLine);
			var momentumBullish = momentumValue >= MomentumBuyThreshold;
			var momentumBearish = -momentumValue >= MomentumSellThreshold;

			if (Position <= 0 && consolidationBullish && fastAboveSlow && momentumBullish && macdBullish)
			{
				EnterLong(candle.ClosePrice);
			}
			else if (Position >= 0 && consolidationBearish && fastBelowSlow && momentumBearish && macdBearish)
			{
				EnterShort(candle.ClosePrice);
			}
		}

		_previousPreviousCandle = _previousCandle;
		_previousCandle = candle;
	}
	private void EnterLong(decimal referencePrice)
	{
		// Submit a long breakout order and attach protection.
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		var currentPosition = Position;
		BuyMarket(volume + Math.Abs(currentPosition));

		var pipSize = ResolvePipSize();
		if (StopLossPips > 0m)
			_stopLossPrice = referencePrice - StopLossPips * pipSize;
		else
			_stopLossPrice = null;

		if (TakeProfitPips > 0m)
			_takeProfitPrice = referencePrice + TakeProfitPips * pipSize;
		else
			_takeProfitPrice = null;
	}
	private void EnterShort(decimal referencePrice)
	{
		// Submit a short breakout order and attach protection.
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		var currentPosition = Position;
		SellMarket(volume + Math.Abs(currentPosition));

		var pipSize = ResolvePipSize();
		if (StopLossPips > 0m)
			_stopLossPrice = referencePrice + StopLossPips * pipSize;
		else
			_stopLossPrice = null;

		if (TakeProfitPips > 0m)
			_takeProfitPrice = referencePrice - TakeProfitPips * pipSize;
		else
			_takeProfitPrice = null;
	}
	private void ManageOpenPosition(ICandleMessage candle)
	{
		// Monitor existing positions for stop-loss or take-profit hits.
		if (Position > 0)
		{
			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetProtection();
			}
		}
		else if (Position < 0)
		{
			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(-Position);
				ResetProtection();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(-Position);
				ResetProtection();
			}
		}
	}
	private void ResetProtection()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal ResolvePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			// Fallback pip size when the instrument does not expose a price step.
			return 0.0001m;
		}

		return step;
	}
}
