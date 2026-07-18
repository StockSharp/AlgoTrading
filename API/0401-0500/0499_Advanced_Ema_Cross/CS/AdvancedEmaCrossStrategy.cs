namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

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
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevEmaShort;
	private decimal _prevEmaLong;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaShortLength { get => _emaShortLength.Value; set => _emaShortLength.Value = value; }
	public int EmaLongLength { get => _emaLongLength.Value; set => _emaLongLength.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxHighLevel { get => _adxHighLevel.Value; set => _adxHighLevel.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdvancedEmaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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

		_adxHighLevel = Param(nameof(AdxHighLevel), 20m)
			.SetDisplay("ADX Level", "ADX threshold for trending market", "ADX");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEmaShort = 0;
		_prevEmaLong = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var emaShort = emaShortValue.ToDecimal();
		var emaLong = emaLongValue.ToDecimal();

		var adxTyped = (IAverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
		{
			_prevEmaShort = emaShort;
			_prevEmaLong = emaLong;
			return;
		}

		if (_prevEmaShort == 0)
		{
			_prevEmaShort = emaShort;
			_prevEmaLong = emaLong;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevEmaShort = emaShort;
			_prevEmaLong = emaLong;
			return;
		}

		var crossover = _prevEmaShort <= _prevEmaLong && emaShort > emaLong;
		var crossunder = _prevEmaShort >= _prevEmaLong && emaShort < emaLong;
		var trending = adx > AdxHighLevel;

		if (crossover && trending && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (crossunder && trending && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevEmaShort = emaShort;
		_prevEmaLong = emaLong;
	}
}
