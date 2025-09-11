using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining a GM-8 SMA, a secondary EMA filter, and ADX.
/// </summary>
public class Gm8AdxSecondEmaStrategy : Strategy
{
	private readonly StrategyParam<int> _gmPeriod;
	private readonly StrategyParam<int> _secondEmaPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _gmSma;
	private ExponentialMovingAverage _secondEma;
	private AverageDirectionalIndex _adx;

	private decimal _prevClose;
	private decimal _prevSma;
	private bool _isFirst = true;

	/// <summary>
	/// GM-8 SMA period.
	/// </summary>
	public int GmPeriod
	{
		get => _gmPeriod.Value;
		set => _gmPeriod.Value = value;
	}

	/// <summary>
	/// Second EMA period.
	/// </summary>
	public int SecondEmaPeriod
	{
		get => _secondEmaPeriod.Value;
		set => _secondEmaPeriod.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public Gm8AdxSecondEmaStrategy()
	{
		_gmPeriod = Param(nameof(GmPeriod), 15)
			.SetDisplay("GM Period", "SMA period for GM-8", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_secondEmaPeriod = Param(nameof(SecondEmaPeriod), 59)
			.SetDisplay("Second EMA Period", "Period for secondary EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_adxPeriod = Param(nameof(AdxPeriod), 8)
			.SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_adxThreshold = Param(nameof(AdxThreshold), 34m)
			.SetDisplay("ADX Threshold", "Minimum ADX level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 50m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_gmSma = default;
		_secondEma = default;
		_adx = default;
		_prevClose = default;
		_prevSma = default;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_gmSma = new SimpleMovingAverage { Length = GmPeriod };
		_secondEma = new ExponentialMovingAverage { Length = SecondEmaPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_gmSma, _secondEma, _adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _gmSma);
			DrawIndicator(area, _secondEma);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue emaValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFinal || !emaValue.IsFinal || !adxValue.IsFinal)
			return;

		var sma = smaValue.ToDecimal();
		var ema = emaValue.ToDecimal();
		var adxTyped = (AverageDirectionalIndexValue)adxValue;

		if (adxTyped.MovingAverage is not decimal adx)
			return;

		if (_isFirst)
		{
			_prevClose = candle.ClosePrice;
			_prevSma = sma;
			_isFirst = false;
			return;
		}

		var crossAbove = _prevClose <= _prevSma && candle.ClosePrice > sma;
		var crossBelow = _prevClose >= _prevSma && candle.ClosePrice < sma;

		if (crossAbove && candle.ClosePrice > ema && adx > AdxThreshold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (crossBelow && candle.ClosePrice < ema && adx > AdxThreshold && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (crossBelow && Position > 0)
		{
			SellMarket(Position);
		}
		else if (crossAbove && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevClose = candle.ClosePrice;
		_prevSma = sma;
	}
}

