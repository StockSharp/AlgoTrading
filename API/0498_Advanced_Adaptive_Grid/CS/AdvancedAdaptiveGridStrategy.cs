namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Advanced Adaptive Grid Trading Strategy.
/// Uses RSI extremes and MA trend to enter, with percentage-based stop/TP.
/// </summary>
public class AdvancedAdaptiveGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _entryPrice;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int ShortMaLength { get => _shortMaLength.Value; set => _shortMaLength.Value = value; }
	public int LongMaLength { get => _longMaLength.Value; set => _longMaLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdvancedAdaptiveGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators");

		_shortMaLength = Param(nameof(ShortMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Short MA", "Short moving average length", "Trend");

		_longMaLength = Param(nameof(LongMaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Long MA", "Long moving average length", "Trend");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

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
		_entryPrice = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var shortMa = new SimpleMovingAverage { Length = ShortMaLength };
		var longMa = new SimpleMovingAverage { Length = LongMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, shortMa, longMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal shortMaValue, decimal longMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentPrice = candle.ClosePrice;
		var bullish = shortMaValue > longMaValue;
		var bearish = shortMaValue < longMaValue;

		// Check stop/TP for existing positions
		if (Position > 0 && _entryPrice > 0)
		{
			var stopPrice = _entryPrice * (1 - StopLossPercent / 100m);
			var tpPrice = _entryPrice * (1 + TakeProfitPercent / 100m);
			if (currentPrice <= stopPrice || currentPrice >= tpPrice)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var stopPrice = _entryPrice * (1 + StopLossPercent / 100m);
			var tpPrice = _entryPrice * (1 - TakeProfitPercent / 100m);
			if (currentPrice >= stopPrice || currentPrice <= tpPrice)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
				return;
			}
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Entry logic
		if (bullish && rsiValue < RsiOversold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = currentPrice;
			_cooldownRemaining = CooldownBars;
		}
		else if (bearish && rsiValue > RsiOverbought && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = currentPrice;
			_cooldownRemaining = CooldownBars;
		}
		// Trend reversal exit
		else if (Position > 0 && bearish && rsiValue > RsiOverbought)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && bullish && rsiValue < RsiOversold)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
	}
}
