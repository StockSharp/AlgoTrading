using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grr-Al Breakout strategy: captures breakout from candle open price.
/// When price moves a configurable ATR-based distance from the open, enters in that direction.
/// Uses ATR for dynamic breakout distance measurement.
/// </summary>
public class GrrAlBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private DateTimeOffset? _currentCandleTime;
	private decimal _anchorPrice;
	private bool _hasTriggered;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public GrrAlBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length for breakout distance", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for breakout distance", "Signals");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentCandleTime = null;
		_hasTriggered = false;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.ToDecimal();
		if (atr <= 0)
			return;

		// Reset trigger on new candle
		if (_currentCandleTime != candle.OpenTime)
		{
			_currentCandleTime = candle.OpenTime;
			_anchorPrice = candle.OpenPrice;
			_hasTriggered = false;
		}

		if (_hasTriggered)
			return;

		var deltaPrice = atr * AtrMultiplier;
		if (deltaPrice <= 0)
			return;

		var close = candle.ClosePrice;

		// Breakout up - buy
		if (close - _anchorPrice >= deltaPrice && Position <= 0)
		{
			BuyMarket();
			_hasTriggered = true;
		}
		// Breakout down - sell
		else if (_anchorPrice - close >= deltaPrice && Position >= 0)
		{
			SellMarket();
			_hasTriggered = true;
		}
	}
}
