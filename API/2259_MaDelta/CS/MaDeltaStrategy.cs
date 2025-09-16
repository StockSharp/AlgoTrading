using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average Delta strategy. It trades when the cubed amplified difference
/// between fast and slow moving averages crosses dynamic thresholds.
/// </summary>
public class MaDeltaStrategy : Strategy
{
	private readonly StrategyParam<int> _delta;
	private readonly StrategyParam<int> _multiplier;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _hi;
	private decimal _lo;
	private bool _isInit;
	private int _trade;
	private decimal _deltaStep;
	private decimal _multiplierFactor;

	/// <summary>
	/// Distance between high and low thresholds in pips.
	/// </summary>
	public int Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	/// <summary>
	/// Multiplier for the moving average difference.
	/// </summary>
	public int Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Period for fast moving average.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for slow moving average.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Initializes <see cref="MaDeltaStrategy"/>.
	/// </summary>
	public MaDeltaStrategy()
	{
		_delta = Param(nameof(Delta), 195)
			.SetDisplay("Delta (pips)", "Hi-Lo threshold in pips", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 5);

		_multiplier = Param(nameof(Multiplier), 392)
			.SetDisplay("Multiplier", "Amplifier for MA difference", "General")
			.SetCanOptimize(true)
			.SetOptimize(100, 500, 10);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 26)
			.SetDisplay("Fast MA Period", "Period for fast moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 51)
			.SetDisplay("Slow MA Period", "Period for slow moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 1);

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "General");

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
		_hi = 0m;
		_lo = 0m;
		_isInit = false;
		_trade = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Precalculate scaled parameters
		_deltaStep = Delta * 0.00001m;
		_multiplierFactor = Multiplier * 0.1m;

		// Create indicators
		var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		// Optional charting
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Cubic amplifier transfer of the difference
		var diff = _multiplierFactor * (fastMaValue - slowMaValue);
		var px = (decimal)Math.Pow((double)diff, 3);

		if (!_isInit)
		{
			_hi = 0m;
			_lo = 0m;
			_trade = 0;
			_isInit = true;
		}

		if (px > _hi)
		{
			_hi = px;
			_lo = _hi - _deltaStep;
			_trade = 1;
		}
		else if (px < _lo)
		{
			_lo = px;
			_hi = _lo + _deltaStep;
			_trade = -1;
		}

		// Reverse existing position if direction changes
		if (_trade == 1 && Position < 0)
			BuyMarket(Math.Abs(Position));
		else if (_trade == -1 && Position > 0)
			SellMarket(Position);

		// Enter new position if flat
		if (Position == 0)
		{
			if (_trade == 1)
				BuyMarket(Volume);
			else if (_trade == -1)
				SellMarket(Volume);
		}
	}
}
