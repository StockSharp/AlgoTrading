using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big Candle RSI Divergence strategy.
/// When flat, a candle whose body is bigger than each of the previous five bodies opens a trade in its direction. An initial stop
/// InitialStopLossTicks price steps away protects the trade; once price has moved TrailStartTicks steps in profit a trailing stop
/// follows the best price at TrailDistanceTicks steps. Fast RSI(5) and slow RSI(14) are plotted for comparison.
/// </summary>
public class BigCandleRsiDivergenceStrategy : Strategy
{
	private const int _bodyLookback = 5;

	private readonly StrategyParam<int> _trailStartTicks;
	private readonly StrategyParam<int> _trailDistanceTicks;
	private readonly StrategyParam<int> _initialStopLossTicks;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _bodies = new();
	private decimal _entryPrice;
	private decimal _bestPrice;
	private bool _trailingActive;

	/// <summary>
	/// Profit in price steps that activates the trailing stop.
	/// </summary>
	public int TrailStartTicks
	{
		get => _trailStartTicks.Value;
		set => _trailStartTicks.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public int TrailDistanceTicks
	{
		get => _trailDistanceTicks.Value;
		set => _trailDistanceTicks.Value = value;
	}

	/// <summary>
	/// Initial stop loss in price steps.
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
	/// Constructor.
	/// </summary>
	public BigCandleRsiDivergenceStrategy()
	{
		_trailStartTicks = Param(nameof(TrailStartTicks), 200)
			.SetNotNegative()
			.SetDisplay("Trail Start Ticks", "Profit in price steps that activates the trailing stop", "Risk");

		_trailDistanceTicks = Param(nameof(TrailDistanceTicks), 150)
			.SetNotNegative()
			.SetDisplay("Trail Distance Ticks", "Trailing stop distance in price steps", "Risk");

		_initialStopLossTicks = Param(nameof(InitialStopLossTicks), 200)
			.SetNotNegative()
			.SetDisplay("Initial Stop Loss Ticks", "Initial stop loss in price steps", "Risk");

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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsiFast = new RelativeStrengthIndex { Length = 5 };
		var rsiSlow = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsiFast, rsiSlow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsiFast);
				DrawIndicator(oscillators, rsiSlow);
			}
		}
	}

	private void ResetState()
	{
		_bodies.Clear();
		_entryPrice = 0;
		_bestPrice = 0;
		_trailingActive = false;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiFastValue, IIndicatorValue rsiSlowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var isBig = _bodies.Count == _bodyLookback && _bodies.All(b => body > b);

		_bodies.Enqueue(body);
		while (_bodies.Count > _bodyLookback)
			_bodies.Dequeue();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			ManageStops(candle);
			return;
		}

		if (!isBig)
			return;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			BuyMarket(Volume);
			StartTrade(candle.ClosePrice);
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			SellMarket(Volume);
			StartTrade(candle.ClosePrice);
		}
	}

	private void StartTrade(decimal price)
	{
		_entryPrice = price;
		_bestPrice = price;
		_trailingActive = false;
	}

	private void ManageStops(ICandleMessage candle)
	{
		if (_entryPrice <= 0)
			return;

		var step = Security?.PriceStep ?? 1m;
		var initialStop = InitialStopLossTicks * step;
		var trailStart = TrailStartTicks * step;
		var trailDistance = TrailDistanceTicks * step;

		if (Position > 0)
		{
			decimal? stop = InitialStopLossTicks > 0 ? _entryPrice - initialStop : null;
			if (_trailingActive)
				stop = Math.Max(stop ?? decimal.MinValue, _bestPrice - trailDistance);

			if (stop is decimal s && candle.LowPrice <= s)
			{
				SellMarket(Position);
				_entryPrice = 0;
				return;
			}

			_bestPrice = Math.Max(_bestPrice, candle.HighPrice);
			if (TrailDistanceTicks > 0 && _bestPrice - _entryPrice >= trailStart)
				_trailingActive = true;
		}
		else if (Position < 0)
		{
			decimal? stop = InitialStopLossTicks > 0 ? _entryPrice + initialStop : null;
			if (_trailingActive)
				stop = Math.Min(stop ?? decimal.MaxValue, _bestPrice + trailDistance);

			if (stop is decimal s && candle.HighPrice >= s)
			{
				BuyMarket(-Position);
				_entryPrice = 0;
				return;
			}

			_bestPrice = Math.Min(_bestPrice, candle.LowPrice);
			if (TrailDistanceTicks > 0 && _entryPrice - _bestPrice >= trailStart)
				_trailingActive = true;
		}
	}
}
