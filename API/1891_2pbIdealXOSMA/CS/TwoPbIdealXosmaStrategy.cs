using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy translated from the MQL5 Exp_2pbIdealXOSMA expert adviser.
/// Uses MACD histogram slope to generate entry and exit signals.
/// </summary>
public class TwoPbIdealXosmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<bool> _closeLong;
	private readonly StrategyParam<bool> _closeShort;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast MA period for MACD calculation.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow MA period for MACD calculation.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Index of the bar used for signal calculations.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	/// <summary>
	/// Allow closing existing long positions.
	/// </summary>
	public bool CloseLong
	{
		get => _closeLong.Value;
		set => _closeLong.Value = value;
	}

	/// <summary>
	/// Allow closing existing short positions.
	/// </summary>
	public bool CloseShort
	{
		get => _closeShort.Value;
		set => _closeShort.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TwoPbIdealXosmaStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal", "Signal line period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Bar index for signal", "General");

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Enable long entries", "Trading");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Enable short entries", "Trading");

		_closeLong = Param(nameof(CloseLong), true)
			.SetDisplay("Close Long", "Allow closing long positions", "Trading");

		_closeShort = Param(nameof(CloseShort), true)
			.SetDisplay("Close Short", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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

		StartProtection();

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			FastPeriod = FastPeriod,
			SlowPeriod = SlowPeriod,
			SignalPeriod = SignalPeriod
		};

		var buffer = new decimal?[SignalBar + 3];
		var index = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, (candle, value) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var macdValue = (MovingAverageConvergenceDivergenceSignalValue)value;
				if (macdValue.Histogram is not decimal current)
					return;

				buffer[index % buffer.Length] = current;
				index++;

				if (index < buffer.Length)
					return;

				var currentIdx = (index - SignalBar - 1) % buffer.Length;
				var prev1Idx = (index - SignalBar - 2) % buffer.Length;
				var prev2Idx = (index - SignalBar - 3) % buffer.Length;

				var currentVal = buffer[currentIdx];
				var prev1 = buffer[prev1Idx];
				var prev2 = buffer[prev2Idx];

				if (currentVal is null || prev1 is null || prev2 is null)
					return;

				var buySignal = prev1 < prev2 && currentVal > prev1;
				var sellSignal = prev1 > prev2 && currentVal < prev1;

				if (buySignal)
				{
					if (CloseShort && Position < 0)
						BuyMarket(Math.Abs(Position));

					if (AllowBuy && Position <= 0)
						BuyMarket(Volume + Math.Abs(Position));
				}

				if (sellSignal)
				{
					if (CloseLong && Position > 0)
						SellMarket(Position);

					if (AllowSell && Position >= 0)
						SellMarket(Volume + Math.Abs(Position));
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}
}
