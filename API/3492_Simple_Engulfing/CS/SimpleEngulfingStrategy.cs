using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple engulfing strategy converted from MetaTrader expert advisors.
/// </summary>
public class SimpleEngulfingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _minBodyPips;
	private readonly StrategyParam<decimal> _maxBodyPips;
	private readonly StrategyParam<EngulfingTradeDirection> _direction;

	private decimal _pipSize;
	private CandleSnapshot? _previousCandle;

	/// <summary>
	/// Initializes a new instance of <see cref="SimpleEngulfingStrategy"/>.
	/// </summary>
	public SimpleEngulfingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for engulfing detection.", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume replicated from the MetaTrader expert advisor.", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance between entry price and stop loss in pips.", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance between entry price and take profit in pips.", "Risk Management");

		_minBodyPips = Param(nameof(MinBodyPips), 0m)
			.SetNotNegative()
			.SetDisplay("Min Body (pips)", "Minimum candle body size required by the pattern.", "Pattern");

		_maxBodyPips = Param(nameof(MaxBodyPips), 50m)
			.SetNotNegative()
			.SetDisplay("Max Body (pips)", "Maximum candle body size accepted by the pattern. Set to zero to disable the filter.", "Pattern");

		_direction = Param(nameof(Direction), EngulfingTradeDirection.BuyOnly)
			.SetDisplay("Direction", "Defines which side of the original MetaTrader robots should be executed.", "Trading");
	}

	/// <summary>
	/// Time frame used to build candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Net volume for each entry order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Minimum candle body required to validate an engulfing pattern.
	/// </summary>
	public decimal MinBodyPips
	{
		get => _minBodyPips.Value;
		set => _minBodyPips.Value = value;
	}

	/// <summary>
	/// Maximum candle body accepted by the pattern.
	/// </summary>
	public decimal MaxBodyPips
	{
		get => _maxBodyPips.Value;
		set => _maxBodyPips.Value = value;
	}

	/// <summary>
	/// Defines whether the strategy trades buy setups, sell setups, or both.
	/// </summary>
	public EngulfingTradeDirection Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
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
		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		Volume = TradeVolume;
		_previousCandle = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var takeUnit = TakeProfitPips > 0m && _pipSize > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Price) : null;
		var stopUnit = StopLossPips > 0m && _pipSize > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Price) : null;

		if (takeUnit != null || stopUnit != null)
		{
			// Use StockSharp protective orders to emulate MetaTrader stop-loss and take-profit placement.
			StartProtection(takeProfit: takeUnit, stopLoss: stopUnit, useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only react to completed candles to match MetaTrader behaviour.
		if (candle.State != CandleStates.Finished)
			return;

		var current = new CandleSnapshot
		{
			Open = candle.OpenPrice,
			High = candle.HighPrice,
			Low = candle.LowPrice,
			Close = candle.ClosePrice
		};

		var previous = _previousCandle;
		_previousCandle = current;

		if (previous == null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		var bodySize = Math.Abs(current.Close - current.Open);
		var bodyInPips = _pipSize > 0m ? bodySize / _pipSize : bodySize;

		if (MinBodyPips > 0m && bodyInPips < MinBodyPips)
			return;

		if (MaxBodyPips > 0m && bodyInPips > MaxBodyPips)
			return;

		var bullish = current.Close > current.Open && previous.Value.Close < previous.Value.Open &&
			current.Open <= previous.Value.Close && current.Close >= previous.Value.Open;

		if (bullish && AllowsSide(Sides.Buy) && Position <= 0)
		{
			EnterPosition(Sides.Buy);
			return;
		}

		var bearish = current.Close < current.Open && previous.Value.Close > previous.Value.Open &&
			current.Open >= previous.Value.Close && current.Close <= previous.Value.Open;

		if (bearish && AllowsSide(Sides.Sell) && Position >= 0)
			EnterPosition(Sides.Sell);
	}

	private void EnterPosition(Sides side)
	{
		var volume = Volume;

		if (side == Sides.Buy && Position < 0)
			volume += Math.Abs(Position);
		else if (side == Sides.Sell && Position > 0)
			volume += Math.Abs(Position);

		if (volume <= 0m)
			return;

		switch (side)
		{
			case Sides.Buy:
				// Enter long when a bullish engulfing pattern appears.
				BuyMarket(volume);
				break;
			case Sides.Sell:
				// Enter short when a bearish engulfing pattern appears.
				SellMarket(volume);
				break;
		}
	}

	private bool AllowsSide(Sides side)
	{
		return Direction switch
		{
			EngulfingTradeDirection.BuyOnly => side == Sides.Buy,
			EngulfingTradeDirection.SellOnly => side == Sides.Sell,
			EngulfingTradeDirection.Both => true,
			_ => false
		};
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;
		return step * multiplier;
	}

	private struct CandleSnapshot
	{
		public decimal Open;
		public decimal High;
		public decimal Low;
		public decimal Close;
	}

	/// <summary>
	/// Specifies which signals of the MetaTrader robots should be executed.
	/// </summary>
	public enum EngulfingTradeDirection
	{
		BuyOnly,
		SellOnly,
		Both,
	}
}
