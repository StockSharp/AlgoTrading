using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that calculates potential profit or loss at a specified price level.
/// The strategy does not place orders and only reports the hypothetical result
/// if the current position were closed at the configured line price.
/// </summary>
public class CalcProfitLossOnLinePriceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _linePrice;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Price level used to evaluate potential profit or loss.
	/// </summary>
	public decimal LinePrice
	{
		get => _linePrice.Value;
		set => _linePrice.Value = value;
	}

	/// <summary>
	/// Candle type for updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CalcProfitLossOnLinePriceStrategy()
	{
		_linePrice = Param(nameof(LinePrice), 0m)
			.SetDisplay("Line Price", "Price level to evaluate PnL", "General")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (LinePrice <= 0m || Position == 0)
			return;

		var priceStep = Security.PriceStep ?? 1m;
		var stepPrice = Security.StepPrice ?? 1m;
		if (priceStep == 0m || stepPrice == 0m)
			return;

		var diff = LinePrice - PositionPrice;
		var pnl = diff / priceStep * stepPrice * Position;

		AddInfoLog($"Profit/Loss at {LinePrice} = {pnl:0.##} {Portfolio?.Currency}");
	}
}
