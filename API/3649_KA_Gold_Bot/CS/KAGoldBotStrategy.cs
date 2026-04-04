using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "KA-Gold Bot".
/// Trades breakouts of a Keltner-style channel confirmed by trend filters from fast and slow EMA.
/// Buys when close breaks above upper Keltner band and fast EMA is above slow EMA.
/// Sells when close breaks below lower Keltner band and fast EMA is below slow EMA.
/// Exits when price crosses the opposite Keltner band or when EMA trend reverses.
/// </summary>
public class KAGoldBotStrategy : Strategy
{
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<decimal> _bandMultiplier;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private ExponentialMovingAverage _keltnerEma;
	private SimpleMovingAverage _rangeAverage;

	private bool _prevAboveUpper;
	private bool _prevBelowLower;
	private decimal _entryPrice;

	/// <summary>
	/// Keltner channel length used for the midline EMA and range average.
	/// </summary>
	public int KeltnerPeriod
	{
		get => _keltnerPeriod.Value;
		set => _keltnerPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for crossover signal.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for trend filter.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for Keltner channel band width.
	/// </summary>
	public decimal BandMultiplier
	{
		get => _bandMultiplier.Value;
		set => _bandMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public KAGoldBotStrategy()
	{
		_keltnerPeriod = Param(nameof(KeltnerPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fast EMA filter", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slow EMA trend filter", "Indicators");

		_bandMultiplier = Param(nameof(BandMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Band Multiplier", "Multiplier for Keltner band width", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		_keltnerEma = new ExponentialMovingAverage { Length = KeltnerPeriod };
		_rangeAverage = new SimpleMovingAverage { Length = KeltnerPeriod };

		_prevAboveUpper = false;
		_prevBelowLower = false;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Process Keltner EMA and range average manually (IsFinal=true for finished candles)
		var midResult = _keltnerEma.Process(new DecimalIndicatorValue(_keltnerEma, close, candle.OpenTime) { IsFinal = true });
		var rangeResult = _rangeAverage.Process(new DecimalIndicatorValue(_rangeAverage, candle.HighPrice - candle.LowPrice, candle.OpenTime) { IsFinal = true });

		if (!_keltnerEma.IsFormed || !_rangeAverage.IsFormed)
			return;

		var mid = midResult.GetValue<decimal>();
		var avgRange = rangeResult.GetValue<decimal>();
		var upper = mid + avgRange * BandMultiplier;
		var lower = mid - avgRange * BandMultiplier;

		var aboveUpper = close > upper;
		var belowLower = close < lower;

		// Exit logic: close crosses opposite band
		if (Position > 0 && close < lower)
		{
			SellMarket();
		}
		else if (Position < 0 && close > upper)
		{
			BuyMarket();
		}

		// Entry logic: Keltner breakout + EMA trend confirmation
		if (Position == 0)
		{
			// Buy: close breaks above upper band, fast EMA above slow EMA
			if (!_prevAboveUpper && aboveUpper && fastValue > slowValue)
			{
				BuyMarket();
				_entryPrice = close;
			}
			// Sell: close breaks below lower band, fast EMA below slow EMA
			else if (!_prevBelowLower && belowLower && fastValue < slowValue)
			{
				SellMarket();
				_entryPrice = close;
			}
		}

		_prevAboveUpper = aboveUpper;
		_prevBelowLower = belowLower;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_fastEma = null;
		_slowEma = null;
		_keltnerEma = null;
		_rangeAverage = null;
		_prevAboveUpper = false;
		_prevBelowLower = false;
		_entryPrice = 0;

		base.OnReseted();
	}
}
