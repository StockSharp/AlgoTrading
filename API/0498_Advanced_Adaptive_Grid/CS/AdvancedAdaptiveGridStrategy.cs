namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Advanced Adaptive Grid Trading Strategy.
/// Uses RSI and MA trend to determine grid direction, enters on grid levels.
/// </summary>
public class AdvancedAdaptiveGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseGridSize;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal _lastEntryPrice;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal BaseGridSize { get => _baseGridSize.Value; set => _baseGridSize.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int ShortMaLength { get => _shortMaLength.Value; set => _shortMaLength.Value = value; }
	public int LongMaLength { get => _longMaLength.Value; set => _longMaLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	public AdvancedAdaptiveGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_baseGridSize = Param(nameof(BaseGridSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Grid Size %", "Base grid step as percentage", "Grid")
			.SetOptimize(0.5m, 5m, 0.5m);

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
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management");
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

		_lastEntryPrice = 0;
		_entryPrice = 0;

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

		var currentPrice = candle.ClosePrice;
		var bullish = shortMaValue > longMaValue;
		var bearish = shortMaValue < longMaValue;

		// Check stop loss / take profit
		if (Position > 0 && _entryPrice > 0)
		{
			var stopPrice = _entryPrice * (1 - StopLossPercent / 100m);
			var tpPrice = _entryPrice * (1 + TakeProfitPercent / 100m);
			if (currentPrice <= stopPrice || currentPrice >= tpPrice)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var stopPrice = _entryPrice * (1 + StopLossPercent / 100m);
			var tpPrice = _entryPrice * (1 - TakeProfitPercent / 100m);
			if (currentPrice >= stopPrice || currentPrice <= tpPrice)
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		// Grid entry logic
		var gridStep = currentPrice * BaseGridSize / 100m;
		var priceMovedDown = _lastEntryPrice > 0 && currentPrice <= _lastEntryPrice - gridStep;
		var priceMovedUp = _lastEntryPrice > 0 && currentPrice >= _lastEntryPrice + gridStep;

		if (Position == 0)
		{
			// Initial entry
			if (bullish && rsiValue < RsiOversold)
			{
				BuyMarket();
				_entryPrice = currentPrice;
				_lastEntryPrice = currentPrice;
			}
			else if (bearish && rsiValue > RsiOverbought)
			{
				SellMarket();
				_entryPrice = currentPrice;
				_lastEntryPrice = currentPrice;
			}
		}
		else if (Position > 0 && priceMovedDown && bullish)
		{
			// Grid: add to long on dip
			BuyMarket();
			_entryPrice = currentPrice;
			_lastEntryPrice = currentPrice;
		}
		else if (Position < 0 && priceMovedUp && bearish)
		{
			// Grid: add to short on rally
			SellMarket();
			_entryPrice = currentPrice;
			_lastEntryPrice = currentPrice;
		}
		// Trend reversal exit
		else if (Position > 0 && bearish && rsiValue > RsiOverbought)
		{
			SellMarket();
			_entryPrice = 0;
			_lastEntryPrice = 0;
		}
		else if (Position < 0 && bullish && rsiValue < RsiOversold)
		{
			BuyMarket();
			_entryPrice = 0;
			_lastEntryPrice = 0;
		}
	}
}
