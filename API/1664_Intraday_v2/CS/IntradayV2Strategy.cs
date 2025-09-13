using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intraday mean reversion strategy based on double Bollinger Bands.
/// </summary>
public class IntradayV2Strategy : Strategy
{
	private readonly StrategyParam<int> _bandLength;
	private readonly StrategyParam<decimal> _entryWidth;
	private readonly StrategyParam<decimal> _exitWidth;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int BandLength { get => _bandLength.Value; set => _bandLength.Value = value; }
	public decimal EntryWidth { get => _entryWidth.Value; set => _entryWidth.Value = value; }
	public decimal ExitWidth { get => _exitWidth.Value; set => _exitWidth.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IntradayV2Strategy()
	{
		_bandLength = Param(nameof(BandLength), 20)
			.SetDisplay("Band Length", "Bollinger band length", "General")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_entryWidth = Param(nameof(EntryWidth), 2.4m)
			.SetDisplay("Entry Width", "Standard deviation for entries", "General")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_exitWidth = Param(nameof(ExitWidth), 1m)
			.SetDisplay("Exit Width", "Standard deviation for exits", "General")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_stopLoss = Param(nameof(StopLoss), 30m)
			.SetDisplay("Stop Loss", "Price offset for stop loss", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_takeProfit = Param(nameof(TakeProfit), 60m)
			.SetDisplay("Take Profit", "Price offset for take profit", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General")
			.SetCanOptimize(false);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var entryBands = new BollingerBands
		{
			Length = BandLength,
			Width = EntryWidth
		};

		var exitBands = new BollingerBands
		{
			Length = BandLength,
			Width = ExitWidth
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(entryBands, exitBands, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal entryMiddle, decimal entryUpper, decimal entryLower, decimal exitMiddle, decimal exitUpper, decimal exitLower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Entry rules based on outer bands
		if (candle.ClosePrice < entryLower && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (candle.ClosePrice > entryUpper && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}

		// Exit rules for long positions
		if (Position > 0)
		{
			if (candle.ClosePrice > exitLower ||
				(StopLoss > 0m && candle.ClosePrice <= _entryPrice - StopLoss) ||
				(TakeProfit > 0m && candle.ClosePrice >= _entryPrice + TakeProfit))
			{
				SellMarket();
			}
		}
		// Exit rules for short positions
		else if (Position < 0)
		{
			if (candle.ClosePrice < exitUpper ||
				(StopLoss > 0m && candle.ClosePrice >= _entryPrice + StopLoss) ||
				(TakeProfit > 0m && candle.ClosePrice <= _entryPrice - TakeProfit))
			{
				BuyMarket();
			}
		}
	}
}
