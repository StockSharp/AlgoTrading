using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Translation of the MetaTrader "PROphet" expert advisor.
/// Uses a range-weighted signal from previous candles to enter trades.
/// </summary>
public class ProphetStrategy : Strategy
{
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<int> _x4;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _candle1;
	private ICandleMessage _candle2;
	private ICandleMessage _candle3;
	private decimal _entryPrice;

	public ProphetStrategy()
	{
		_x1 = Param(nameof(X1), 9)
			.SetDisplay("X1", "Weight applied to |High[1] - Low[2]|.", "Signal");

		_x2 = Param(nameof(X2), 29)
			.SetDisplay("X2", "Weight applied to |High[3] - Low[2]|.", "Signal");

		_x3 = Param(nameof(X3), 94)
			.SetDisplay("X3", "Weight applied to |High[2] - Low[1]|.", "Signal");

		_x4 = Param(nameof(X4), 125)
			.SetDisplay("X4", "Weight applied to |High[2] - Low[3]|.", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations.", "General");
	}

	public int X1 { get => _x1.Value; set => _x1.Value = value; }
	public int X2 { get => _x2.Value; set => _x2.Value = value; }
	public int X3 { get => _x3.Value; set => _x3.Value = value; }
	public int X4 { get => _x4.Value; set => _x4.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candle1 = null;
		_candle2 = null;
		_candle3 = null;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Shift history
		_candle3 = _candle2;
		_candle2 = _candle1;
		_candle1 = candle;

		if (_candle1 == null || _candle2 == null || _candle3 == null)
			return;

		// Manage position - simple exit on reversal signal
		if (Position > 0)
		{
			var sellSignal = CalculateSignal(true);
			if (sellSignal > 0)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var buySignal = CalculateSignal(false);
			if (buySignal > 0)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry
		if (Position == 0)
		{
			var buySignal = CalculateSignal(false);
			var sellSignal = CalculateSignal(true);

			if (buySignal > 0 && buySignal > sellSignal)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (sellSignal > 0 && sellSignal > buySignal)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private decimal CalculateSignal(bool isSell)
	{
		var term1 = Math.Abs(_candle1.HighPrice - _candle2.LowPrice);
		var term2 = Math.Abs(_candle3.HighPrice - _candle2.LowPrice);
		var term3 = Math.Abs(_candle2.HighPrice - _candle1.LowPrice);
		var term4 = Math.Abs(_candle2.HighPrice - _candle3.LowPrice);

		if (isSell)
		{
			// For sell, use inverted weights
			return (100 - X1) * term1 + (100 - X2) * term2 + (X3 - 100) * term3 + (X4 - 100) * term4;
		}

		// For buy, use standard weights
		return (X1 - 100) * term1 + (X2 - 100) * term2 + (100 - X3) * term3 + (100 - X4) * term4;
	}
}
