namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Grid Template strategy: places trades at regular grid intervals.
/// Buys when price drops by grid step, sells when price rises by grid step.
/// </summary>
public class GridTemplateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _gridStepPercent;

	private decimal _lastTradePrice;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal GridStepPercent
	{
		get => _gridStepPercent.Value;
		set => _gridStepPercent.Value = value;
	}

	public GridTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_gridStepPercent = Param(nameof(GridStepPercent), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step %", "Price change percentage for grid level", "Grid");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_initialized = false;

		var sma = new SimpleMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_initialized)
		{
			_lastTradePrice = close;
			_initialized = true;
			return;
		}

		var step = _lastTradePrice * GridStepPercent / 100m;

		// Buy when price drops by grid step
		if (close <= _lastTradePrice - step)
		{
			BuyMarket();
			_lastTradePrice = close;
		}
		// Sell when price rises by grid step
		else if (close >= _lastTradePrice + step)
		{
			SellMarket();
			_lastTradePrice = close;
		}
	}
}
