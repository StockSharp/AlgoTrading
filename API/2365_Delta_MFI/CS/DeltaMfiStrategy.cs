using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the difference between fast and slow Money Flow Index (MFI).
/// Buys when the fast MFI is above the slow MFI and the slow MFI is above the signal level.
/// Sells when the fast MFI is below the slow MFI and the slow MFI is below 100 minus the signal level.
/// </summary>
public class DeltaMfiStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _level;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast MFI period length.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow MFI period length.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// MFI level used to confirm signals.
	/// </summary>
	public int Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// The type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DeltaMfiStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MFI Period", "Period for fast Money Flow Index", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow MFI Period", "Period for slow Money Flow Index", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_level = Param(nameof(Level), 50)
			.SetGreaterThanZero()
			.SetDisplay("Signal Level", "MFI level to confirm signals", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(30, 70, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Enable position protection once
		StartProtection();

		var fastMfi = new MoneyFlowIndex { Length = FastPeriod };
		var slowMfi = new MoneyFlowIndex { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMfi, slowMfi, ProcessCandle).Start();

		// Draw indicators if a chart is available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMfi);
			DrawIndicator(area, slowMfi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check strategy readiness and connection state
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Long signal: slow MFI above level and fast MFI above slow MFI
		if (slowValue > Level && fastValue > slowValue && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			return;
		}

		// Short signal: slow MFI below (100 - level) and fast MFI below slow MFI
		if (slowValue < (100 - Level) && fastValue < slowValue && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
