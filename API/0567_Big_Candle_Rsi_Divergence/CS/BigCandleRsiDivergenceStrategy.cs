using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big Candle Identifier with RSI divergence and delayed trailing stops.
/// </summary>
public class BigCandleRsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _trailStartTicks;
	private readonly StrategyParam<int> _trailDistanceTicks;
	private readonly StrategyParam<int> _initialStopLossTicks;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsiFast;
	private RelativeStrengthIndex _rsiSlow;

	private decimal _body1;
	private decimal _body2;
	private decimal _body3;
	private decimal _body4;
	private decimal _body5;

	private decimal _entryPrice;
	private decimal? _trailStop;

	private decimal _trailStartPrice;
	private decimal _trailDistancePrice;
	private decimal _initialStopLossPrice;

	/// <summary>
	/// Ticks before trailing stop activation.
	/// </summary>
	public int TrailStartTicks
	{
		get => _trailStartTicks.Value;
		set => _trailStartTicks.Value = value;
	}

	/// <summary>
	/// Distance in ticks between price and trailing stop.
	/// </summary>
	public int TrailDistanceTicks
	{
		get => _trailDistanceTicks.Value;
		set => _trailDistanceTicks.Value = value;
	}

	/// <summary>
	/// Initial stop loss in ticks.
	/// </summary>
	public int InitialStopLossTicks
	{
		get => _initialStopLossTicks.Value;
		set => _initialStopLossTicks.Value = value;
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
	/// Initialize the strategy.
	/// </summary>
	public BigCandleRsiDivergenceStrategy()
	{
		_trailStartTicks = Param(nameof(TrailStartTicks), 200)
			.SetDisplay("Trailing Start Ticks", "Ticks before trailing stop activates", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_trailDistanceTicks = Param(nameof(TrailDistanceTicks), 150)
			.SetDisplay("Trailing Distance Ticks", "Trailing stop distance in ticks", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50, 250, 50);

		_initialStopLossTicks = Param(nameof(InitialStopLossTicks), 200)
			.SetDisplay("Initial Stop Loss Ticks", "Initial stop loss distance in ticks", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_body1 = _body2 = _body3 = _body4 = _body5 = 0;
		_entryPrice = 0;
		_trailStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tick = Security?.PriceStep ?? 1m;
		_trailStartPrice = TrailStartTicks * tick;
		_trailDistancePrice = TrailDistanceTicks * tick;
		_initialStopLossPrice = InitialStopLossTicks * tick;

		_rsiFast = new RelativeStrengthIndex { Length = 5 };
		_rsiSlow = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsiFast, _rsiSlow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsiFast);
			DrawIndicator(area, _rsiSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiFast, decimal rsiSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var body0 = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		var bullishBigCandle = body0 > _body1 && body0 > _body2 && body0 > _body3 && body0 > _body4 && body0 > _body5 && candle.OpenPrice < candle.ClosePrice;
		var bearishBigCandle = body0 > _body1 && body0 > _body2 && body0 > _body3 && body0 > _body4 && body0 > _body5 && candle.OpenPrice > candle.ClosePrice;

		var _ = rsiFast - rsiSlow; // Divergence for visualization

		if (Position == 0)
		{
			_trailStop = null;

			if (bullishBigCandle)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_trailStop = _entryPrice - _initialStopLossPrice;
			}
			else if (bearishBigCandle)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_trailStop = _entryPrice + _initialStopLossPrice;
			}
		}
		else if (Position > 0)
		{
			var profit = candle.ClosePrice - _entryPrice;
			if (profit >= _trailStartPrice)
			{
				var newStop = candle.ClosePrice - _trailDistancePrice;
				if (_trailStop == null || newStop > _trailStop)
					_trailStop = newStop;
			}

			var stop = _trailStop ?? (_entryPrice - _initialStopLossPrice);
			if (candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_trailStop = null;
			}
		}
		else
		{
			var profit = _entryPrice - candle.ClosePrice;
			if (profit >= _trailStartPrice)
			{
				var newStop = candle.ClosePrice + _trailDistancePrice;
				if (_trailStop == null || newStop < _trailStop)
					_trailStop = newStop;
			}

			var stop = _trailStop ?? (_entryPrice + _initialStopLossPrice);
			if (candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_trailStop = null;
			}
		}

		body5 = _body4;
		body4 = _body3;
		body3 = _body2;
		body2 = _body1;
		body1 = body0;
	}
}
