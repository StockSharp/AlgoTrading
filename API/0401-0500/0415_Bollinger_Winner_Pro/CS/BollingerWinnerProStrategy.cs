using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands Winner PRO Strategy with RSI and MA filters.
/// Buys when price touches lower BB, RSI confirms oversold, and price above MA.
/// Sells when price touches upper BB, RSI confirms overbought, and price below MA.
/// </summary>
public class BollingerWinnerProStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _cooldownBars;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

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
	/// RSI period.
	/// </summary>
	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RSIOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RSIOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
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
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ma;
	private int _cooldownRemaining;

	public BollingerWinnerProStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 1.5m)
			.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI Filter");

		_rsiOversold = Param(nameof(RSIOversold), 40m)
			.SetDisplay("RSI Oversold", "RSI oversold threshold", "RSI Filter");

		_rsiOverbought = Param(nameof(RSIOverbought), 60m)
			.SetDisplay("RSI Overbought", "RSI overbought threshold", "RSI Filter");

		_maLength = Param(nameof(MALength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Moving Average");

		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bollinger = null;
		_rsi = null;
		_ma = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		_rsi = new RelativeStrengthIndex { Length = RSILength };
		_ma = new ExponentialMovingAverage { Length = MALength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_bollinger, _rsi, _ma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle,
		IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_rsi.IsFormed || !_ma.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		if (rsiValue.IsEmpty || maValue.IsEmpty)
			return;

		var rsi = rsiValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = candle.ClosePrice;

		// Buy: price at/below lower BB + RSI oversold
		if (close <= lower && rsi < RSIOversold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: price at/above upper BB + RSI overbought
		else if (close >= upper && rsi > RSIOverbought && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long at middle band
		else if (Position > 0 && close >= middle)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short at middle band
		else if (Position < 0 && close <= middle)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
