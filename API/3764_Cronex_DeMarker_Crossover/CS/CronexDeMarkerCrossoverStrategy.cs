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
/// Strategy that replicates the Cronex DeMarker indicator setup and trades crossovers of its smoothed values.
/// </summary>
public class CronexDeMarkerCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private DeMarker _deMarker = null!;
	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;

	private decimal? _previousFast;
	private decimal? _previousSlow;

	/// <summary>
	/// DeMarker indicator period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CronexDeMarkerCrossoverStrategy"/>.
	/// </summary>
	public CronexDeMarkerCrossoverStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 25)
			.SetRange(2, 150)
			.SetDisplay("DeMarker Period", "Length of the DeMarker oscillator", "Indicators")
			.SetCanOptimize(true);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("Fast LWMA Period", "Length of the fast linear weighted moving average", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 25)
			.SetRange(2, 150)
			.SetDisplay("Slow LWMA Period", "Length of the slow linear weighted moving average", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame of processed candles", "General");
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

		_previousFast = null;
		_previousSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Instantiate indicators matching the original MetaTrader logic.
		_deMarker = new DeMarker
		{
			Length = DeMarkerPeriod
		};

		_fastMa = new WeightedMovingAverage
		{
			Length = FastMaPeriod
		};

		_slowMa = new WeightedMovingAverage
		{
			Length = SlowMaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _deMarker);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only act on completed candles to avoid repainting effects.
		if (candle.State != CandleStates.Finished)
			return;

		// Update the DeMarker oscillator with the full candle data.
		var deMarkerValue = _deMarker.Process(candle).ToDecimal();

		// Smooth the oscillator with linear weighted moving averages.
		var fastValue = _fastMa.Process(deMarkerValue, candle.OpenTime, true).ToDecimal();
		var slowValue = _slowMa.Process(deMarkerValue, candle.OpenTime, true).ToDecimal();

		// Ensure all indicators accumulated enough samples.
		if (!_deMarker.IsFormed || !_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			_previousFast = fastValue;
			_previousSlow = slowValue;
			return;
		}

		var previousFast = _previousFast;
		var previousSlow = _previousSlow;

		_previousFast = fastValue;
		_previousSlow = slowValue;

		if (!previousFast.HasValue || !previousSlow.HasValue)
			return;

		// Check readiness and trading permissions before sending orders.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = previousFast.Value <= previousSlow.Value && fastValue > slowValue;
		var crossDown = previousFast.Value >= previousSlow.Value && fastValue < slowValue;

		if (crossUp)
		{
			// Close short exposure and establish a long position.
			if (Position < 0)
			{
				BuyMarket(OrderVolume + Math.Abs(Position));
			}
			else if (Position == 0)
			{
				BuyMarket(OrderVolume);
			}
		}
		else if (crossDown)
		{
			// Close long exposure and establish a short position.
			if (Position > 0)
			{
				SellMarket(OrderVolume + Math.Abs(Position));
			}
			else if (Position == 0)
			{
				SellMarket(OrderVolume);
			}
		}
	}
}

