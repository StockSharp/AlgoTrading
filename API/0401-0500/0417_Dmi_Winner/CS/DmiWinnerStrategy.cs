using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Directional Movement Index Winner Strategy.
/// Uses DMI crossover with ADX confirmation and EMA trend filter.
/// </summary>
public class DmiWinnerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _diLength;
	private readonly StrategyParam<int> _adxSmoothing;
	private readonly StrategyParam<decimal> _keyLevel;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _cooldownBars;

	private DirectionalIndex _dmi;
	private AverageDirectionalIndex _adx;
	private ExponentialMovingAverage _ma;

	private decimal _prevDiPlus;
	private decimal _prevDiMinus;
	private int _cooldownRemaining;

	public DmiWinnerStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_diLength = Param(nameof(DILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DI Length", "Directional Indicator period", "DMI");

		_adxSmoothing = Param(nameof(ADXSmoothing), 13)
			.SetGreaterThanZero()
			.SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI");

		_keyLevel = Param(nameof(KeyLevel), 20m)
			.SetDisplay("Key Level", "ADX key level threshold", "DMI");

		_maLength = Param(nameof(MALength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Moving Average");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int DILength
	{
		get => _diLength.Value;
		set => _diLength.Value = value;
	}

	public int ADXSmoothing
	{
		get => _adxSmoothing.Value;
		set => _adxSmoothing.Value = value;
	}

	public decimal KeyLevel
	{
		get => _keyLevel.Value;
		set => _keyLevel.Value = value;
	}

	public int MALength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_dmi = null;
		_adx = null;
		_ma = null;
		_prevDiPlus = 0;
		_prevDiMinus = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_dmi = new DirectionalIndex { Length = DILength };
		_adx = new AverageDirectionalIndex { Length = ADXSmoothing };
		_ma = new ExponentialMovingAverage { Length = MALength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_dmi, _adx, _ma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue dmiValue, IIndicatorValue adxValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_dmi.IsFormed || !_adx.IsFormed || !_ma.IsFormed)
			return;

		var dmiTyped = (DirectionalIndexValue)dmiValue;
		if (dmiTyped.Plus is not decimal diPlus || dmiTyped.Minus is not decimal diMinus)
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adxVal)
			return;

		if (maValue.IsEmpty)
			return;

		var maVal = maValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevDiPlus = diPlus;
			_prevDiMinus = diMinus;
			return;
		}

		var close = candle.ClosePrice;

		// DI crossover detection
		var diPlusCrossUp = diPlus > diMinus && _prevDiPlus <= _prevDiMinus && _prevDiPlus > 0;
		var diPlusCrossDown = diPlus < diMinus && _prevDiPlus >= _prevDiMinus && _prevDiPlus > 0;

		// Buy: DI+ crosses above DI-, ADX above key level, price above MA
		if (diPlusCrossUp && adxVal > KeyLevel && close > maVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: DI- crosses above DI+, ADX above key level, price below MA
		else if (diPlusCrossDown && adxVal > KeyLevel && close < maVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long if DI+ crosses below DI-
		else if (Position > 0 && diPlusCrossDown)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short if DI+ crosses above DI-
		else if (Position < 0 && diPlusCrossUp)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevDiPlus = diPlus;
		_prevDiMinus = diMinus;
	}
}
