namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Advanced EMA Cross Strategy - uses EMA crossover with ADX filter.
/// </summary>
public class AdvancedEmaCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxHighLevel;

	private decimal _prevEmaShort;
	private decimal _prevEmaLong;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaShortLength { get => _emaShortLength.Value; set => _emaShortLength.Value = value; }
	public int EmaLongLength { get => _emaLongLength.Value; set => _emaLongLength.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxHighLevel { get => _adxHighLevel.Value; set => _adxHighLevel.Value = value; }

	public AdvancedEmaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaShortLength = Param(nameof(EmaShortLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short Length", "Short EMA period", "EMA");

		_emaLongLength = Param(nameof(EmaLongLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long Length", "Long EMA period", "EMA");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX calculation period", "ADX");

		_adxHighLevel = Param(nameof(AdxHighLevel), 25m)
			.SetDisplay("ADX Level", "ADX threshold for trending market", "ADX");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevEmaShort = 0;
		_prevEmaLong = 0;
		_entryPrice = 0;

		var emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		var emaLong = new ExponentialMovingAverage { Length = EmaLongLength };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(emaShort, emaLong, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaShortValue, IIndicatorValue emaLongValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var emaShort = emaShortValue.ToDecimal();
		var emaLong = emaLongValue.ToDecimal();

		var adxTyped = (IAverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var crossover = _prevEmaShort > 0 && _prevEmaShort <= _prevEmaLong && emaShort > emaLong;
		var crossunder = _prevEmaShort > 0 && _prevEmaShort >= _prevEmaLong && emaShort < emaLong;
		var trending = adx > AdxHighLevel;

		if (crossover && trending && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (crossunder && Position > 0)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (crossunder && trending && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (crossover && Position < 0)
		{
			BuyMarket();
			_entryPrice = 0;
		}

		_prevEmaShort = emaShort;
		_prevEmaLong = emaLong;
	}
}
