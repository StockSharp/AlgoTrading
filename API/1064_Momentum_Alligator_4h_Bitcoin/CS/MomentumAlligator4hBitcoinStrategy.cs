using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum Alligator strategy for Bitcoin on 4h candles.
/// Buys when Awesome Oscillator crosses above its 5-period SMA and
/// price is above the daily Alligator lines. Uses dynamic stop-loss
/// based on the greater of percent drop and Alligator jaw. After a
/// profitable trade skips the next two signals.
/// </summary>
public class MomentumAlligator4hBitcoinStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _tradeStart;
	private readonly StrategyParam<DateTimeOffset> _tradeStop;

	private SmoothedMovingAverage _jawSmma;
	private readonly Queue<decimal> _jawQueue = new();
	private decimal? _jaw;

	private SmoothedMovingAverage _jaw1DSmma;
	private SmoothedMovingAverage _teeth1DSmma;
	private SmoothedMovingAverage _lips1DSmma;
	private readonly Queue<decimal> _jaw1DQueue = new();
	private readonly Queue<decimal> _teeth1DQueue = new();
	private readonly Queue<decimal> _lips1DQueue = new();
	private decimal? _jaw1D;
	private decimal? _teeth1D;
	private decimal? _lips1D;

	private AwesomeOscillator _ao;
	private SimpleMovingAverage _aoSma;
	private decimal _prevAo;
	private decimal _prevAoSma;
	private bool _isFirstAo = true;

	private int _skipCount;
	private decimal _entryPrice;

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trading start time.
	/// </summary>
	public DateTimeOffset TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}

	/// <summary>
	/// Trading stop time.
	/// </summary>
	public DateTimeOffset TradeStop
	{
		get => _tradeStop.Value;
		set => _tradeStop.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MomentumAlligator4hBitcoinStrategy"/>.
	/// </summary>
	public MomentumAlligator4hBitcoinStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (%)", "Percent stop-loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_tradeStart = Param(nameof(TradeStart), new DateTimeOffset(new DateTime(2023, 1, 1)))
			.SetDisplay("Trade Start", "Start of trading period", "General");

		_tradeStop = Param(nameof(TradeStop), new DateTimeOffset(new DateTime(2025, 1, 1)))
			.SetDisplay("Trade Stop", "End of trading period", "General");
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

		_jawQueue.Clear();
		_jaw1DQueue.Clear();
		_teeth1DQueue.Clear();
		_lips1DQueue.Clear();
		_jaw = null;
		_jaw1D = null;
		_teeth1D = null;
		_lips1D = null;
		_prevAo = 0m;
		_prevAoSma = 0m;
		_isFirstAo = true;
		_skipCount = 0;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jawSmma = new SmoothedMovingAverage { Length = 13 };

		_jaw1DSmma = new SmoothedMovingAverage { Length = 13 };
		_teeth1DSmma = new SmoothedMovingAverage { Length = 8 };
		_lips1DSmma = new SmoothedMovingAverage { Length = 5 };

		_ao = new AwesomeOscillator { ShortPeriod = 5, LongPeriod = 34 };
		_aoSma = new SimpleMovingAverage { Length = 5 };

		SubscribeCandles(CandleType)
			.BindEx(_ao, ProcessCandle)
			.Start();

		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
			.Bind(ProcessDaily)
			.Start();
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawVal = _jaw1DSmma.Process(new DecimalIndicatorValue(_jaw1DSmma, median, candle.ServerTime));
		var teethVal = _teeth1DSmma.Process(new DecimalIndicatorValue(_teeth1DSmma, median, candle.ServerTime));
		var lipsVal = _lips1DSmma.Process(new DecimalIndicatorValue(_lips1DSmma, median, candle.ServerTime));
		if (!jawVal.IsFormed || !teethVal.IsFormed || !lipsVal.IsFormed)
		return;

		_jaw1DQueue.Enqueue(jawVal.ToDecimal());
		_teeth1DQueue.Enqueue(teethVal.ToDecimal());
		_lips1DQueue.Enqueue(lipsVal.ToDecimal());

		if (_jaw1DQueue.Count > 8)
		_jaw1D = _jaw1DQueue.Dequeue();
		if (_teeth1DQueue.Count > 5)
		_teeth1D = _teeth1DQueue.Dequeue();
		if (_lips1DQueue.Count > 3)
		_lips1D = _lips1DQueue.Dequeue();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aoValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_skipCount > 0)
		_skipCount--;

		var time = candle.OpenTime;
		if (time < TradeStart || time > TradeStop)
		return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawVal = _jawSmma.Process(new DecimalIndicatorValue(_jawSmma, median, candle.ServerTime));
		if (jawVal.IsFormed)
		{
		_jawQueue.Enqueue(jawVal.ToDecimal());
		if (_jawQueue.Count > 8)
		_jaw = _jawQueue.Dequeue();
		}

		if (!aoValue.IsFinal)
		return;

		var ao = aoValue.GetValue<decimal>();
		var aoSmaVal = _aoSma.Process(new DecimalIndicatorValue(_aoSma, ao, candle.ServerTime));
		if (!_aoSma.IsFormed || _jaw1D is not decimal jaw1D || _teeth1D is not decimal teeth1D || _lips1D is not decimal lips1D)
		return;

		var aoSma = aoSmaVal.ToDecimal();
		var aoCross = !_isFirstAo && _prevAo <= _prevAoSma && ao > aoSma;
		_prevAo = ao;
		_prevAoSma = aoSma;
		_isFirstAo = false;

		if (aoCross && candle.ClosePrice > jaw1D && candle.ClosePrice > teeth1D && candle.ClosePrice > lips1D && _skipCount == 0 && Position <= 0)
		{
		BuyMarket();
		_entryPrice = candle.ClosePrice;
		}

		if (Position > 0)
		{
		var percentStop = _entryPrice * (1m - StopLossPercent);
		var stop = _jaw is decimal j && j > percentStop ? j : percentStop;
		if (candle.LowPrice <= stop)
		{
		SellMarket();
		if (stop > _entryPrice && _skipCount == 0)
		_skipCount = 2;
		}
		}
	}
}
