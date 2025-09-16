using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy based on two sequential moving averages.
/// The original MQL expert uses a colored double smoothed moving average.
/// Here the indicator is approximated by two simple moving averages and
/// trades on their crossovers.
/// </summary>
public class ColorX2MaDigitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Length of the fast moving average.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Length of the slow moving average.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ColorX2MaDigitStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Length of the first smoothing", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowLength = Param(nameof(SlowLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Length of the second smoothing", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

		// Create moving average indicators.
		var fastMa = new SimpleMovingAverage { Length = FastLength };
		var slowMa = new SimpleMovingAverage { Length = SlowLength };

		// Subscribe to candle data and bind indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		// Optional chart drawing.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa, "Fast MA");
			DrawIndicator(area, slowMa, "Slow MA");
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		// Process only finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Check strategy readiness and trading permissions.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Store first values to compare later.
		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fastMa;
			_prevSlow = slowMa;
			return;
		}

		var wasAbove = _prevFast > _prevSlow;
		var isAbove = fastMa > slowMa;

		// Generate trading signals on crossover.
		if (!wasAbove && isAbove && Position <= 0)
		{
			// Fast MA crossed above slow MA -> open long position.
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (wasAbove && !isAbove && Position >= 0)
		{
			// Fast MA crossed below slow MA -> open short position.
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevFast = fastMa;
		_prevSlow = slowMa;
	}
}

