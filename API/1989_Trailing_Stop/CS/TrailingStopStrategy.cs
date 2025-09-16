using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that manages trailing stop and optional stop-loss for existing positions.
/// </summary>
public class TrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _stopPrice;
	private decimal _entryPrice;
	private decimal _lastPosition;

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle data type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStopStrategy"/> class.
	/// </summary>
	public TrailingStopStrategy()
	{
		_trailingStop = Param(nameof(TrailingStop), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Initial stop-loss distance", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 20m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_stopPrice = null;
		_entryPrice = 0m;
		_lastPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (_lastPosition == 0 && Position != 0)
		{
			// Position was opened; store entry price and optional stop-loss.
			_entryPrice = trade.Trade.Price;
			_stopPrice = StopLoss > 0m
				? (Position > 0m ? _entryPrice - StopLoss : _entryPrice + StopLoss)
				: null;
		}
		else if (Position == 0)
		{
			// Position was closed; reset state.
			_stopPrice = null;
			_entryPrice = 0m;
		}

		_lastPosition = Position;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			// Long position.
			var profit = candle.ClosePrice - _entryPrice;

			if (profit >= TrailingStop)
			{
				var newStop = candle.ClosePrice - TrailingStop;
				if (_stopPrice is null || newStop > _stopPrice.Value)
					_stopPrice = newStop;
			}

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_stopPrice = null;
			}
		}
		else if (Position < 0)
		{
			// Short position.
			var profit = _entryPrice - candle.ClosePrice;

			if (profit >= TrailingStop)
			{
				var newStop = candle.ClosePrice + TrailingStop;
				if (_stopPrice is null || newStop < _stopPrice.Value)
					_stopPrice = newStop;
			}

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = null;
			}
		}
	}
}
