using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that manages trailing stop levels for existing positions.
/// The strategy does not generate entry signals and only adjusts stop levels.
/// </summary>
public class TrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal _step;

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Candle type used for price updates.
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
		_trailingStop = Param(nameof(TrailingStop), 500m)
			.SetGreaterThanZero()
			.SetCanOptimize()
			.SetDisplay("Trailing Stop", "Distance from price to stop in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for trailing calculations", "General");
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
		_longStop = null;
		_shortStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_step = Security?.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			// Calculate trailing stop for long positions.
			var newStop = candle.ClosePrice - TrailingStop * _step;
			_longStop = _longStop is null ? newStop : Math.Max(_longStop.Value, newStop);

			// Close the position if price crosses the stop level.
			if (candle.LowPrice <= _longStop)
			{
				SellMarket(Position);
				_longStop = null;
			}
		}
		else if (Position < 0)
		{
			// Calculate trailing stop for short positions.
			var newStop = candle.ClosePrice + TrailingStop * _step;
			_shortStop = _shortStop is null ? newStop : Math.Min(_shortStop.Value, newStop);

			// Close the position if price crosses the stop level.
			if (candle.HighPrice >= _shortStop)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = null;
			}
		}
		else
		{
			// Reset stops when no position exists.
			_longStop = null;
			_shortStop = null;
		}
	}
}
