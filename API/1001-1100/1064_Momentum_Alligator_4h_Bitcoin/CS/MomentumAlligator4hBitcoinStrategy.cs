using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum Alligator 4h Bitcoin strategy.
/// Goes long when the Awesome Oscillator crosses above its 5-period SMA and the close is above the jaw, teeth and lips
/// of the daily Alligator, only between TradeStart and TradeStop. The position is closed by a stop at the higher of the
/// percent stop below entry and the daily jaw. After a profitable exit the next two entry signals are skipped.
/// </summary>
public class MomentumAlligator4hBitcoinStrategy : Strategy
{
	private const int _signalsToSkip = 2;

	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _tradeStart;
	private readonly StrategyParam<DateTimeOffset> _tradeStop;

	private SimpleMovingAverage _aoSma;
	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private decimal? _prevAo;
	private decimal? _prevAoSma;
	private decimal? _dailyJaw;
	private decimal? _dailyTeeth;
	private decimal? _dailyLips;
	private decimal _entryPrice;
	private int _skipCount;

	/// <summary>
	/// Percent stop below the entry price, as a fraction (0.02 = 2%).
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type of the trading timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Entries are allowed from this time.
	/// </summary>
	public DateTimeOffset TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}

	/// <summary>
	/// Entries are allowed until this time.
	/// </summary>
	public DateTimeOffset TradeStop
	{
		get => _tradeStop.Value;
		set => _tradeStop.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MomentumAlligator4hBitcoinStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 0.02m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Percent stop below entry as a fraction", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_tradeStart = Param(nameof(TradeStart), new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Trade Start", "Entries are allowed from this time", "General");

		_tradeStop = Param(nameof(TradeStop), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Trade Stop", "Entries are allowed until this time", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_prevAo = null;
		_prevAoSma = null;
		_dailyJaw = null;
		_dailyTeeth = null;
		_dailyLips = null;
		_entryPrice = 0m;
		_skipCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ao = new AwesomeOscillator();
		_aoSma = new SimpleMovingAverage { Length = 5 };
		_jaw = new SmoothedMovingAverage { Length = 13 };
		_teeth = new SmoothedMovingAverage { Length = 8 };
		_lips = new SmoothedMovingAverage { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ao, ProcessCandle)
			.Start();

		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
			.Bind(ProcessDailyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, ao);
				DrawIndicator(oscillators, _aoSma);
			}
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The Alligator lines are smoothed averages of the median price.
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jaw = _jaw.Process(new DecimalIndicatorValue(_jaw, median, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var teeth = _teeth.Process(new DecimalIndicatorValue(_teeth, median, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var lips = _lips.Process(new DecimalIndicatorValue(_lips, median, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (_jaw.IsFormed)
			_dailyJaw = jaw;
		if (_teeth.IsFormed)
			_dailyTeeth = teeth;
		if (_lips.IsFormed)
			_dailyLips = lips;
	}

	private void ProcessCandle(ICandleMessage candle, decimal ao)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var aoSma = _aoSma.Process(new DecimalIndicatorValue(_aoSma, ao, candle.OpenTime) { IsFinal = true }).ToDecimal();
		if (!_aoSma.IsFormed)
		{
			_prevAo = ao;
			return;
		}

		var prevAo = _prevAo;
		var prevAoSma = _prevAoSma;
		_prevAo = ao;
		_prevAoSma = aoSma;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			var stop = _entryPrice * (1m - StopLossPercent);
			if (_dailyJaw is decimal jawStop && jawStop > stop)
				stop = jawStop;

			if (candle.LowPrice <= stop)
			{
				var exitPrice = Math.Min(stop, candle.OpenPrice);
				if (exitPrice > _entryPrice)
					_skipCount = _signalsToSkip;

				SellMarket(Position);
			}

			return;
		}

		if (prevAo is not decimal pa || prevAoSma is not decimal ps)
			return;

		if (_dailyJaw is not decimal jaw || _dailyTeeth is not decimal teeth || _dailyLips is not decimal lips)
			return;

		var crossUp = pa <= ps && ao > aoSma;
		if (!crossUp)
			return;

		var close = candle.ClosePrice;
		if (close <= jaw || close <= teeth || close <= lips)
			return;

		var openTime = candle.OpenTime;
		if (openTime < TradeStart.UtcDateTime || openTime >= TradeStop.UtcDateTime)
			return;

		if (_skipCount > 0)
		{
			_skipCount--;
			return;
		}

		_entryPrice = close;
		BuyMarket(Volume);
	}
}
