using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with optional ADX filter.
/// </summary>
public class Ema5813AdxFilterStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<int> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDiff;

	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }
	public bool UseAdxFilter { get => _useAdxFilter.Value; set => _useAdxFilter.Value = value; }
	public int AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Ema5813AdxFilterStrategy()
	{
		_enableLong = Param(nameof(EnableLong), true).SetDisplay("Enable Long");
		_enableShort = Param(nameof(EnableShort), true).SetDisplay("Enable Short");
		_useAdxFilter = Param(nameof(UseAdxFilter), false).SetDisplay("Use ADX Filter");
		_adxThreshold = Param(nameof(AdxThreshold), 20).SetDisplay("ADX Threshold").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema5 = new EMA { Length = 5 };
		var ema8 = new EMA { Length = 8 };
		var ema13 = new EMA { Length = 13 };
		var adx = new AverageDirectionalIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(ema5, ema8, ema13, adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema5);
			DrawIndicator(area, ema8);
			DrawIndicator(area, ema13);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ema5Value, IIndicatorValue ema8Value, IIndicatorValue ema13Value, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ema5 = ema5Value.ToDecimal();
		var ema8 = ema8Value.ToDecimal();
		var ema13 = ema13Value.ToDecimal();
		var adx = ((AverageDirectionalIndexValue)adxValue).MovingAverage;
		if (adx is not decimal adxVal)
			return;

		var diff = ema5 - ema8;
		var crossUp = _prevDiff <= 0m && diff > 0m;
		var crossDown = _prevDiff >= 0m && diff < 0m;
		_prevDiff = diff;

		var adxOk = !UseAdxFilter || adxVal >= AdxThreshold;

		if (EnableLong && crossUp && adxOk && Position <= 0)
			BuyMarket();
		else if (EnableShort && crossDown && adxOk && Position >= 0)
			SellMarket();

		if (Position > 0 && candle.ClosePrice < ema13)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice > ema13)
			BuyMarket();
	}
}

