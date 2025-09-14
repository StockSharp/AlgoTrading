using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD based strategy that interprets histogram or signal line slope.
/// </summary>
public enum MacdSignalMode
{
	Histogram,
	SignalLine
}

/// <summary>
/// Strategy that reacts to color changes of the MACD histogram or signal line.
/// </summary>
public class ColorXMacdCandleStrategy : Strategy
{
	private readonly StrategyParam<MacdSignalMode> _mode;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<bool> _enableBuyOpen;
	private readonly StrategyParam<bool> _enableSellOpen;
	private readonly StrategyParam<bool> _enableBuyClose;
	private readonly StrategyParam<bool> _enableSellClose;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd;

	private decimal _prevHist;
	private decimal _prevSignal;
	private bool _prevUp;
	private bool _isFirst = true;

	/// <summary>
	/// Source of signals.
	/// </summary>
	public MacdSignalMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line smoothing length.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableBuyOpen
	{
		get => _enableBuyOpen.Value;
		set => _enableBuyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableSellOpen
	{
		get => _enableSellOpen.Value;
		set => _enableSellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool EnableBuyClose
	{
		get => _enableBuyClose.Value;
		set => _enableBuyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool EnableSellClose
	{
		get => _enableSellClose.Value;
		set => _enableSellClose.Value = value;
	}

	/// <summary>
	/// Candle type for subscriptions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public ColorXMacdCandleStrategy()
	{
		_mode = Param(nameof(Mode), MacdSignalMode.Histogram)
				.SetDisplay("Mode", "Source of signals", "Parameters");

		_fastPeriod = Param(nameof(FastPeriod), 12)
				.SetGreaterThanZero()
				.SetDisplay("Fast Period", "Fast EMA period", "MACD")
				.SetCanOptimize(true)
				.SetOptimize(6, 18, 2);

		_slowPeriod = Param(nameof(SlowPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Slow Period", "Slow EMA period", "MACD")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Signal Period", "Signal line period", "MACD")
				.SetCanOptimize(true)
				.SetOptimize(6, 18, 1);

		_enableBuyOpen = Param(nameof(EnableBuyOpen), true)
				.SetDisplay("Enable Buy Open", "Allow opening longs", "Behaviour");

		_enableSellOpen = Param(nameof(EnableSellOpen), true)
				.SetDisplay("Enable Sell Open", "Allow opening shorts", "Behaviour");

		_enableBuyClose = Param(nameof(EnableBuyClose), true)
				.SetDisplay("Enable Buy Close", "Allow closing longs", "Behaviour");

		_enableSellClose = Param(nameof(EnableSellClose), true)
				.SetDisplay("Enable Sell Close", "Allow closing shorts", "Behaviour");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for calculations", "Common");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastPeriod,
			LongPeriod = SlowPeriod,
			SignalPeriod = SignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
				.Bind(_macd, ProcessCandle)
				.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal histValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevHist = histValue;
			_prevSignal = signalValue;
			_prevUp = false;
			_isFirst = false;
			return;
		}

		var isUp = Mode == MacdSignalMode.Histogram ? histValue > _prevHist : signalValue > _prevSignal;

		if (Mode == MacdSignalMode.Histogram)
			_prevHist = histValue;
		else
			_prevSignal = signalValue;

		if (isUp && !_prevUp)
		{
			if (EnableSellClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (EnableBuyOpen && Position <= 0)
				BuyMarket(Volume);
		}
		else if (!isUp && _prevUp)
		{
			if (EnableBuyClose && Position > 0)
				SellMarket(Position);

			if (EnableSellOpen && Position >= 0)
				SellMarket(Volume);
		}

		_prevUp = isUp;
	}
}
