using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified trading panel ported from the MetaTrader 4 expert advisor "EA_TradingPanel".
/// Executes a batch of market orders in the selected direction and attaches optional protective levels.
/// Distances for stop-loss and take-profit are configured in pips and converted to absolute price offsets.
/// </summary>
public class TradingPanelBatchStrategy : Strategy
{
	/// <summary>
	/// Direction requested for the next batch of orders.
	/// </summary>
	public enum TradeDirections
	{
		/// <summary>
		/// Do not execute any trades.
		/// </summary>
		None,

		/// <summary>
		/// Send buy market orders.
		/// </summary>
		Buy,

		/// <summary>
		/// Send sell market orders.
		/// </summary>
		Sell
	}

	private readonly StrategyParam<int> _numberOfOrders;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<TradeDirections> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;

	/// <summary>
	/// Number of market orders to submit in one batch.
	/// </summary>
	public int NumberOfOrders
	{
		get => _numberOfOrders.Value;
		set => _numberOfOrders.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Volume used for each market order in the batch.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Trade direction to be executed on the next completed candle.
	/// Automatically returns to <see cref="TradeDirections.None"/> after execution.
	/// </summary>
	public TradeDirections Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Candle type that triggers trade execution when a candle is finished.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TradingPanelBatchStrategy"/>.
	/// </summary>
	public TradingPanelBatchStrategy()
	{
		_numberOfOrders = Param(nameof(NumberOfOrders), 1)
		.SetGreaterThanZero()
		.SetDisplay("Trades Count", "Number of market orders per batch", "Execution")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_stopLossPips = Param(nameof(StopLossPips), 2m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 10m, 1m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 20m, 2m);

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume of a single market order", "Execution");

		_direction = Param(nameof(Direction), TradeDirections.None)
		.SetDisplay("Direction", "Trade direction for the next execution", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used to trigger execution", "Market Data");
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
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		var takeDistance = ConvertPipsToPrice(TakeProfitPips);

		if (stopDistance > 0m || takeDistance > 0m)
		{
			StartProtection(
			takeProfit: takeDistance > 0m ? new Unit(takeDistance, UnitTypes.Absolute) : null,
			stopLoss: stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : null,
			useMarketOrders: true);
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		ExecuteBatch(candle.ClosePrice);
	}

	private void ExecuteBatch(decimal referencePrice)
	{
		var direction = Direction;
		if (direction == TradeDirections.None)
		return;

		var volume = OrderVolume;
		if (volume <= 0m)
		{
			LogWarning("Order volume must be positive to send trades.");
			Direction = TradeDirections.None;
			return;
		}

		var count = NumberOfOrders;
		if (count <= 0)
		{
			LogWarning("Number of orders must be positive to send trades.");
			Direction = TradeDirections.None;
			return;
		}

		for (var i = 0; i < count; i++)
		{
			switch (direction)
			{
			case TradeDirections.Buy:
				BuyMarket(volume);
				break;
			case TradeDirections.Sell:
				SellMarket(volume);
				break;
			}
		}

		LogInfo($"Executed {count} {direction} market order(s) at reference price {referencePrice:F5} with volume {volume}.");

		Direction = TradeDirections.None;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m || _pipSize <= 0m)
		return 0m;

		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		if (Security == null)
		throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		var decimals = Security.Decimals ?? GetDecimalsFromStep(step);
		if (decimals is 1 or 3 or 5)
		step *= 10m;

		return step;
	}

	private static int GetDecimalsFromStep(decimal step)
	{
		if (step <= 0m)
		return 0;

		var value = step;
		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}

