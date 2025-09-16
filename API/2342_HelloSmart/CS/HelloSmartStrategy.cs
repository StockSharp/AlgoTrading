using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that opens sequential orders in a single direction.
/// Increases order volume when total position exceeds threshold and
/// closes all positions on reaching profit or loss limits.
/// </summary>
public class HelloSmartStrategy : Strategy
{
	private readonly StrategyParam<TradeMode> _tradeMode;
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _lot;
	private readonly StrategyParam<decimal> _bigLot;
	private readonly StrategyParam<decimal> _maxLots;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _lossLimit;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _currentLot;
	private decimal _lastPrice;

	/// <summary>Trade direction. 1 - Buy only, 2 - Sell only.</summary>
	public TradeMode Mode { get => _tradeMode.Value; set => _tradeMode.Value = value; }
	/// <summary>Price movement in ticks to add a new position.</summary>
	public decimal Step { get => _step.Value; set => _step.Value = value; }
	/// <summary>Base volume for the first order.</summary>
	public decimal Lot { get => _lot.Value; set => _lot.Value = value; }
	/// <summary>Volume threshold that triggers lot multiplication.</summary>
	public decimal BigLot { get => _bigLot.Value; set => _bigLot.Value = value; }
	/// <summary>Maximum allowed volume for a single order.</summary>
	public decimal MaxLots { get => _maxLots.Value; set => _maxLots.Value = value; }
	/// <summary>Profit target to close all positions.</summary>
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }
	/// <summary>Loss limit to close all positions.</summary>
	public decimal LossLimit { get => _lossLimit.Value; set => _lossLimit.Value = value; }
	/// <summary>Multiplier applied when total volume reaches threshold.</summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	/// <summary>Candle type used for step calculation.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Constructor.</summary>
	public HelloSmartStrategy()
	{
		_tradeMode = Param(nameof(Mode), TradeMode.Sell)
			.SetDisplay("Trade Direction", "1 - Buy only, 2 - Sell only", "General");
		_step = Param(nameof(Step), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Step", "Price movement in ticks to add position", "Risk");
		_lot = Param(nameof(Lot), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Lot", "Base volume for first order", "Volume");
		_bigLot = Param(nameof(BigLot), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold Volume", "Volume level that increases next lot", "Volume");
		_maxLots = Param(nameof(MaxLots), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Lot", "Maximum allowed volume per order", "Volume");
		_profitTarget = Param(nameof(ProfitTarget), 60m)
			.SetDisplay("Profit Target", "Close all positions on this profit", "Risk")
			.SetCanOptimize(true);
		_lossLimit = Param(nameof(LossLimit), 5100m)
			.SetDisplay("Loss Limit", "Close all positions on this loss", "Risk")
			.SetCanOptimize(true);
		_multiplier = Param(nameof(Multiplier), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Factor applied when total volume reaches threshold", "Volume");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for step calculation", "General");
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

		_currentLot = Lot;
		_lastPrice = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var stepPrice = Step * (Security.PriceStep ?? 1m);

		if (Mode == TradeMode.Buy)
		{
			var needOpen = Position <= 0 || (Position > 0 && (_lastPrice - price) >= stepPrice);
			if (needOpen)
			{
				BuyMarket(_currentLot + Math.Max(0m, -Position));
				_lastPrice = price;
			}
		}
		else
		{
			var needOpen = Position >= 0 || (Position < 0 && (price - _lastPrice) >= stepPrice);
			if (needOpen)
			{
				SellMarket(_currentLot + Math.Max(0m, Position));
				_lastPrice = price;
			}
		}

		var totalVolume = Math.Abs(Position);
		if (totalVolume >= BigLot)
		{
			_currentLot *= Multiplier;
			if (_currentLot > MaxLots)
				_currentLot = Lot;
		}

		if (PnL > ProfitTarget || PnL < -LossLimit)
		{
			CloseAllPositions();
			_currentLot = Lot;
			_lastPrice = price;
		}
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}

public enum TradeMode
{
	Buy = 1,
	Sell = 2
}
