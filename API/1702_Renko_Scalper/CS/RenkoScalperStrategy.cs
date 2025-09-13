using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko scalper strategy.
/// Opens long when close price is higher than previous close.
/// Opens short when close price is lower than previous close.
/// </summary>
public class RenkoScalperStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _isTrailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousClose;

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool IsTrailingStop
	{
		get => _isTrailingStop.Value;
		set => _isTrailingStop.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RenkoScalperStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Risk Management")
			.SetRange(0.1m, 10m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk Management")
			.SetRange(0.1m, 10m);

		_isTrailingStop = Param(nameof(IsTrailingStop), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			isStopTrailing: IsTrailingStop
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Initialize previous close on first candle
		if (_previousClose is null)
		{
			_previousClose = close;
			return;
		}

		// Open long when price rises above previous close
		if (close > _previousClose && Position <= 0)
			BuyMarket();
		// Open short when price falls below previous close
		else if (close < _previousClose && Position >= 0)
			SellMarket();

		// Store current close for next comparison
		_previousClose = close;
	}
}

