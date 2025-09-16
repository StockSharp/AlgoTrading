using System;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple price action strategy that alternates between long and short trades.
/// Uses fixed stop loss and take profit distances with optional trailing stop.
/// </summary>
public class PriceActionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _tp;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<TradeDirection> _initialDirection;
	private readonly StrategyParam<DataType> _candleType;

	private TradeDirection _nextDirection;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal TP { get => _tp.Value; set => _tp.Value = value; }

	/// <summary>
	/// Take profit multiplier relative to stop distance.
	/// </summary>
	public decimal Leverage { get => _leverage.Value; set => _leverage.Value = value; }

	/// <summary>
	/// Trailing stop distance.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Minimal move required to update trailing stop.
	/// </summary>
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }

	/// <summary>
	/// Direction of the first trade.
	/// </summary>
	public TradeDirection InitialDirection { get => _initialDirection.Value; set => _initialDirection.Value = value; }

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PriceActionStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Trade volume", "General");

		_tp = Param(nameof(TP), 100m)
			.SetDisplay("Stop Distance", "Initial stop distance", "Risk");

		_leverage = Param(nameof(Leverage), 5m)
			.SetDisplay("Leverage", "Take profit multiplier", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 0m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 0m)
			.SetDisplay("Trailing Step", "Minimal move to trail", "Risk");

		_initialDirection = Param(nameof(InitialDirection), TradeDirection.Buy)
			.SetDisplay("Initial Direction", "First trade side", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for logic", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_nextDirection = InitialDirection;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_nextDirection = InitialDirection;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			if (_nextDirection == TradeDirection.Buy)
			{
				BuyMarket(Volume);
				_stopPrice = candle.ClosePrice - TP;
				_takeProfitPrice = candle.ClosePrice + Leverage * TP;
				_nextDirection = TradeDirection.Sell;
			}
			else
			{
				SellMarket(Volume);
				_stopPrice = candle.ClosePrice + TP;
				_takeProfitPrice = candle.ClosePrice - Leverage * TP;
				_nextDirection = TradeDirection.Buy;
			}
		}
		else if (Position > 0)
		{
			if (TrailingStop > 0)
			{
				var newStop = candle.ClosePrice - TrailingStop;
				if (_stopPrice == 0m || newStop - _stopPrice >= TrailingStep)
					_stopPrice = newStop;
			}

			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (TrailingStop > 0)
			{
				var newStop = candle.ClosePrice + TrailingStop;
				if (_stopPrice == 0m || _stopPrice - newStop >= TrailingStep)
					_stopPrice = newStop;
			}

			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
				BuyMarket(-Position);
		}
	}
}

public enum TradeDirection
{
	Buy = 1,
	Sell = 2
}
