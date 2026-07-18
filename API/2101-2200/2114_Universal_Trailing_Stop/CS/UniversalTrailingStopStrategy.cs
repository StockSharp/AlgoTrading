using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters on EMA crossover and manages position with a trailing stop.
/// Uses two EMAs for entries and a price-based trailing stop for exits.
/// </summary>
public class UniversalTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _trailPercent;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;
	private decimal _entryPrice;
	private decimal _bestPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public decimal TrailPercent
	{
		get => _trailPercent.Value;
		set => _trailPercent.Value = value;
	}

	public UniversalTrailingStopStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetDisplay("Fast Period", "Fast EMA period", "Entry");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetDisplay("Slow Period", "Slow EMA period", "Entry");

		_trailPercent = Param(nameof(TrailPercent), 1.5m)
			.SetDisplay("Trail %", "Trailing stop distance in percent", "Trailing");
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
		_prevFast = 0;
		_prevSlow = 0;
		_isInitialized = false;
		_entryPrice = 0;
		_bestPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_isInitialized = false;
		_entryPrice = 0;
		_bestPrice = 0;

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_isInitialized = true;
			return;
		}

		var price = candle.ClosePrice;

		// Trailing stop check
		if (Position > 0)
		{
			if (price > _bestPrice)
				_bestPrice = price;

			var stopLevel = _bestPrice * (1 - TrailPercent / 100m);
			if (price <= stopLevel)
			{
				SellMarket();
				_prevFast = fastValue;
				_prevSlow = slowValue;
				return;
			}
		}
		else if (Position < 0)
		{
			if (price < _bestPrice)
				_bestPrice = price;

			var stopLevel = _bestPrice * (1 + TrailPercent / 100m);
			if (price >= stopLevel)
			{
				BuyMarket();
				_prevFast = fastValue;
				_prevSlow = slowValue;
				return;
			}
		}

		// Entry signals: EMA crossover
		var prevCrossUp = _prevFast <= _prevSlow;
		var currCrossUp = fastValue > slowValue;
		var prevCrossDown = _prevFast >= _prevSlow;
		var currCrossDown = fastValue < slowValue;

		if (prevCrossUp && currCrossUp && !(_prevFast > _prevSlow))
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
			{
				BuyMarket();
				_entryPrice = price;
				_bestPrice = price;
			}
		}
		else if (prevCrossDown && currCrossDown && !(_prevFast < _prevSlow))
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
			{
				SellMarket();
				_entryPrice = price;
				_bestPrice = price;
			}
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
