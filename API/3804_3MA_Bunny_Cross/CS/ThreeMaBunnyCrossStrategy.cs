// ThreeMaBunnyCrossStrategy.cs
// -----------------------------------------------------------------------------
// Port of the "3MA Bunny Cross" MQL4 expert advisor to StockSharp high-level API.
// -----------------------------------------------------------------------------
// Date: 14 Apr 2024
// -----------------------------------------------------------------------------

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implements the 3MA Bunny Cross logic using linear weighted moving averages.
/// </summary>
public class ThreeMaBunnyCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;

	private bool _hasPrevious;
	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>Candle type used for calculations.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Length of the fast linear weighted moving average.</summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>Length of the slow linear weighted moving average.</summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public ThreeMaBunnyCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
		_fastPeriod = Param(nameof(FastPeriod), 5).SetDisplay("Fast LWMA").SetCanOptimize(true);
		_slowPeriod = Param(nameof(SlowPeriod), 20).SetDisplay("Slow LWMA").SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_hasPrevious = false;
		_prevFast = 0m;
		_prevSlow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new LinearWeightedMovingAverage
		{
			Length = FastPeriod,
			CandlePrice = CandlePrice.Close,
		};

		_slowMa = new LinearWeightedMovingAverage
		{
			Length = SlowPeriod,
			CandlePrice = CandlePrice.Close,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevious)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_hasPrevious = true;
			return;
		}

		var crossedUp = _prevFast <= _prevSlow && fastValue >= slowValue;
		var crossedDown = _prevFast >= _prevSlow && fastValue <= slowValue;

		if (crossedUp && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
				BuyMarket(volume);
		}
		else if (crossedDown && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
				SellMarket(volume);
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
