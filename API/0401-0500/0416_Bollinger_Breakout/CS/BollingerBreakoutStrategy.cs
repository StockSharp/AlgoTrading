using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Breakout strategy.
/// Goes long when a candle closes above the upper band and short when it closes below the lower band, provided every
/// enabled filter agrees: RSI beyond its long/short momentum level, Aroon up/down dominance in the trade direction and
/// price on the trade side of the moving average. A long closes when price touches the lower band and a short when it
/// touches the upper band; an optional percent stop-loss caps the risk.
/// </summary>
public class BollingerBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiShortLevel;
	private readonly StrategyParam<decimal> _rsiLongLevel;
	private readonly StrategyParam<bool> _useAroon;
	private readonly StrategyParam<int> _aroonLength;
	private readonly StrategyParam<bool> _useMa;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _useSl;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Enable the RSI filter.
	/// </summary>
	public bool UseRSI
	{
		get => _useRsi.Value;
		set => _useRsi.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level below which shorts are allowed.
	/// </summary>
	public decimal RSIShortLevel
	{
		get => _rsiShortLevel.Value;
		set => _rsiShortLevel.Value = value;
	}

	/// <summary>
	/// RSI level above which longs are allowed.
	/// </summary>
	public decimal RSILongLevel
	{
		get => _rsiLongLevel.Value;
		set => _rsiLongLevel.Value = value;
	}

	/// <summary>
	/// Enable the Aroon filter.
	/// </summary>
	public bool UseAroon
	{
		get => _useAroon.Value;
		set => _useAroon.Value = value;
	}

	/// <summary>
	/// Aroon period.
	/// </summary>
	public int AroonLength
	{
		get => _aroonLength.Value;
		set => _aroonLength.Value = value;
	}

	/// <summary>
	/// Enable the moving average filter.
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
	public BollingerBreakoutStrategy()
	{
		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

		_useRsi = Param(nameof(UseRSI), true)
			.SetDisplay("Use RSI", "Require RSI confirmation", "RSI Filter");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI Filter");

		_rsiShortLevel = Param(nameof(RSIShortLevel), 45m)
			.SetDisplay("RSI Short Level", "RSI level below which shorts are allowed", "RSI Filter");

		_rsiLongLevel = Param(nameof(RSILongLevel), 55m)
			.SetDisplay("RSI Long Level", "RSI level above which longs are allowed", "RSI Filter");

		_useAroon = Param(nameof(UseAroon), false)
			.SetDisplay("Use Aroon", "Require Aroon confirmation", "Aroon Filter");

		_aroonLength = Param(nameof(AroonLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Aroon Length", "Aroon period", "Aroon Filter");

		_useMa = Param(nameof(UseMA), true)
			.SetDisplay("Use MA", "Require price on the trade side of the moving average", "Moving Average");

		_maLength = Param(nameof(MALength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Moving Average");

		_useSl = Param(nameof(UseSL), true)
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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		var rsi = new RelativeStrengthIndex { Length = RSILength };
		var aroon = new Aroon { Length = AroonLength };
		var ma = new ExponentialMovingAverage { Length = MALength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, rsi, aroon, ma, ProcessCandle)
			.Start();

		if (UseSL)
			StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue aroonValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !rsiValue.IsFormed || !aroonValue.IsFormed || !maValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var aroon = (AroonValue)aroonValue;
		if (aroon.Up is not decimal aroonUp || aroon.Down is not decimal aroonDown)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.ToDecimal();
		var ma = maValue.ToDecimal();
		var close = candle.ClosePrice;

		var longSignal = close > upper
			&& (!UseRSI || rsi > RSILongLevel)
			&& (!UseAroon || aroonUp > aroonDown)
			&& (!UseMA || close > ma);

		var shortSignal = close < lower
			&& (!UseRSI || rsi < RSIShortLevel)
			&& (!UseAroon || aroonDown > aroonUp)
			&& (!UseMA || close < ma);

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && candle.LowPrice <= lower)
			SellMarket(Position);
		else if (Position < 0 && candle.HighPrice >= upper)
			BuyMarket(-Position);
	}
}
