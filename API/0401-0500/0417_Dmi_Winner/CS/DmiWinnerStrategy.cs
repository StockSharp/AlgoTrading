using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DMI Winner strategy.
/// Goes long when +DI crosses above -DI and short when -DI crosses above +DI, both only while ADX is above KeyLevel
/// and, when UseMA is set, price is on the trade side of the moving average. The opposite DI cross closes the position
/// (and reverses it when it is a valid entry), and an optional percent stop-loss caps the risk.
/// </summary>
public class DmiWinnerStrategy : Strategy
{
	private readonly StrategyParam<int> _diLength;
	private readonly StrategyParam<int> _adxSmoothing;
	private readonly StrategyParam<decimal> _keyLevel;
	private readonly StrategyParam<bool> _useMa;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _useSl;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _adx;
	private decimal? _prevDiPlus;
	private decimal? _prevDiMinus;

	/// <summary>
	/// Directional indicator period.
	/// </summary>
	public int DILength
	{
		get => _diLength.Value;
		set => _diLength.Value = value;
	}

	/// <summary>
	/// ADX smoothing period.
	/// </summary>
	public int ADXSmoothing
	{
		get => _adxSmoothing.Value;
		set => _adxSmoothing.Value = value;
	}

	/// <summary>
	/// ADX level a crossover needs to be traded.
	/// </summary>
	public decimal KeyLevel
	{
		get => _keyLevel.Value;
		set => _keyLevel.Value = value;
	}

	/// <summary>
	/// Enable the moving average trend filter.
	/// </summary>
	public bool UseMA
	{
		get => _useMa.Value;
		set => _useMa.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MALength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Enable the stop-loss.
	/// </summary>
	public bool UseSL
	{
		get => _useSl.Value;
		set => _useSl.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from the entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DmiWinnerStrategy()
	{
		_diLength = Param(nameof(DILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DI Length", "Directional indicator period", "DMI");

		_adxSmoothing = Param(nameof(ADXSmoothing), 13)
			.SetGreaterThanZero()
			.SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI");

		_keyLevel = Param(nameof(KeyLevel), 23m)
			.SetDisplay("Key Level", "ADX level a crossover needs to be traded", "DMI");

		_useMa = Param(nameof(UseMA), true)
			.SetDisplay("Use MA", "Trade only on the trend side of the moving average", "Moving Average");

		_maLength = Param(nameof(MALength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Moving Average");

		_useSl = Param(nameof(UseSL), false)
			.SetDisplay("Use Stop Loss", "Enable the percent stop-loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss percentage from the entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_adx = null;
		_prevDiPlus = null;
		_prevDiMinus = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDiPlus = null;
		_prevDiMinus = null;

		var dmi = new DirectionalIndex { Length = DILength };
		// ADX is the DX of the DILength directional lines smoothed over ADXSmoothing bars.
		_adx = new SmoothedMovingAverage { Length = ADXSmoothing };
		var ma = new ExponentialMovingAverage { Length = MALength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(dmi, ma, ProcessCandle)
			.Start();

		if (UseSL)
			StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);

			var dmiArea = CreateChartArea();
			if (dmiArea != null)
				DrawIndicator(dmiArea, dmi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!dmiValue.IsFormed)
			return;

		var dmi = (DirectionalIndexValue)dmiValue;
		if (dmi.Plus is not decimal diPlus || dmi.Minus is not decimal diMinus)
			return;

		var diSum = diPlus + diMinus;
		var dx = diSum == 0 ? 0m : 100m * Math.Abs(diPlus - diMinus) / diSum;
		var adx = _adx.Process(dx, candle.ServerTime, true).ToDecimal();

		var prevPlus = _prevDiPlus;
		var prevMinus = _prevDiMinus;
		_prevDiPlus = diPlus;
		_prevDiMinus = diMinus;

		if (!_adx.IsFormed || !maValue.IsFormed || prevPlus is not decimal lastPlus || prevMinus is not decimal lastMinus)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ma = maValue.ToDecimal();
		var close = candle.ClosePrice;

		var crossUp = lastPlus <= lastMinus && diPlus > diMinus;
		var crossDown = lastPlus >= lastMinus && diPlus < diMinus;

		var longSignal = crossUp && adx > KeyLevel && (!UseMA || close > ma);
		var shortSignal = crossDown && adx > KeyLevel && (!UseMA || close < ma);

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && crossDown)
			SellMarket(Position);
		else if (Position < 0 && crossUp)
			BuyMarket(-Position);
	}
}
