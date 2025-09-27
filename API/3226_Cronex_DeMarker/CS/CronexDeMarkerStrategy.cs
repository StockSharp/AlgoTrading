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
/// Strategy that reproduces the Cronex DeMarker crossover expert advisor.
/// </summary>
public class CronexDeMarkerStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Period for the DeMarker oscillator.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Fast smoothing period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothing period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable opening of long trades.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable opening of short trades.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Enable closing of long trades.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Enable closing of short trades.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CronexDeMarkerStrategy"/>.
	/// </summary>
	public CronexDeMarkerStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 25)
			.SetRange(5, 100)
			.SetDisplay("DeMarker Period", "Length for the DeMarker oscillator", "Indicators")
			.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetRange(2, 50)
			.SetDisplay("Fast Period", "Length for the fast smoothing average", "Indicators")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetRange(2, 100)
			.SetDisplay("Slow Period", "Length for the slow smoothing average", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening of long trades", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening of short trades", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing of long trades", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing of short trades", "Trading");
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

		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Build indicator chain: raw DeMarker value -> fast SMA -> slow SMA.
		var deMarker = new DeMarker { Length = DeMarkerPeriod };
		var fastAverage = new SimpleMovingAverage { Length = FastPeriod };
		var slowAverage = new SimpleMovingAverage { Length = SlowPeriod };

		// Subscribe to candles and bind indicators sequentially.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(deMarker, fastAverage, slowAverage, ProcessCandle)
			.Start();

		// Plot the indicator lines together with trade markers for easier analysis.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastAverage);
			DrawIndicator(area, slowAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue, decimal fastValue, decimal slowValue)
	{
		// Process only finished candles to avoid using incomplete data.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure that the strategy is ready to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;

		if (prevFast.HasValue && prevSlow.HasValue)
		{
			if (prevFast > prevSlow)
			{
				if (EnableLongEntry && fastValue <= slowValue && Position <= 0)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}

				if (EnableShortExit && Position < 0)
				{
					BuyMarket(Math.Abs(Position));
				}
			}
			else if (prevFast < prevSlow)
			{
				if (EnableShortEntry && fastValue >= slowValue && Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}

				if (EnableLongExit && Position > 0)
				{
					SellMarket(Math.Abs(Position));
				}
			}
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}

