namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Flawless Victory Strategy.
/// Uses Bollinger Bands and RSI for mean reversion trading.
/// Buys when price below lower BB with RSI oversold.
/// Sells when price above upper BB with RSI overbought.
/// </summary>
public class FlawlessVictoryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;
	private int _cooldownRemaining;

	public FlawlessVictoryStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbWidth = Param(nameof(BBWidth), 1.5m)
			.SetDisplay("BB Width", "Bollinger Bands standard deviation", "Bollinger Bands");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiOversold = Param(nameof(RSIOversold), 42m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_rsiOverbought = Param(nameof(RSIOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal BBWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public decimal RSIOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public decimal RSIOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
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

		_bollinger = null;
		_rsi = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBWidth
		};

		_rsi = new RelativeStrengthIndex { Length = RSILength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, _rsi, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_rsi.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		if (rsiValue.IsEmpty)
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

		// Buy: price below lower BB with RSI oversold
		if (close < lower && rsi < RSIOversold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: price above upper BB with RSI overbought
		else if (close > upper && rsi > RSIOverbought && Position >= 0)
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
