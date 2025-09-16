// MaOscillatorHistogramStrategy.cs
// -----------------------------------------------------------------------------
// Strategy based on a moving average oscillator with histogram-like turning points.
// Converted from MQL5 Exp_MAOscillatorHist.mq5 (ID 14965).
// -----------------------------------------------------------------------------

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average Oscillator Histogram strategy.
/// Generates signals when the oscillator forms local minima or maxima.
/// </summary>
public class MaOscillatorHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<bool> _enableBuyOpen;
	private readonly StrategyParam<bool> _enableSellOpen;
	private readonly StrategyParam<bool> _enableBuyClose;
	private readonly StrategyParam<bool> _enableSellClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOsc1;
	private decimal _prevOsc2;
	private bool _isWarmup;

	/// <summary>
	/// Fast MA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow MA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableBuyOpen
	{
		get => _enableBuyOpen.Value;
		set => _enableBuyOpen.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableSellOpen
	{
		get => _enableSellOpen.Value;
		set => _enableSellOpen.Value = value;
	}

	/// <summary>
	/// Enable closing long positions.
	/// </summary>
	public bool EnableBuyClose
	{
		get => _enableBuyClose.Value;
		set => _enableBuyClose.Value = value;
	}

	/// <summary>
	/// Enable closing short positions.
	/// </summary>
	public bool EnableSellClose
	{
		get => _enableSellClose.Value;
		set => _enableSellClose.Value = value;
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
	/// Initializes a new instance of the <see cref="MaOscillatorHistogramStrategy"/>.
	/// </summary>
	public MaOscillatorHistogramStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 13)
			.SetDisplay("Fast Period", "Period of fast moving average", "Indicators")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 24)
			.SetDisplay("Slow Period", "Period of slow moving average", "Indicators")
			.SetCanOptimize(true);

		_enableBuyOpen = Param(nameof(EnableBuyOpen), true)
			.SetDisplay("Enable Buy Open", "Allow opening long positions", "Signals");

		_enableSellOpen = Param(nameof(EnableSellOpen), true)
			.SetDisplay("Enable Sell Open", "Allow opening short positions", "Signals");

		_enableBuyClose = Param(nameof(EnableBuyClose), true)
			.SetDisplay("Enable Buy Close", "Allow closing long positions", "Signals");

		_enableSellClose = Param(nameof(EnableSellClose), true)
			.SetDisplay("Enable Sell Close", "Allow closing short positions", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOsc1 = default;
		_prevOsc2 = default;
		_isWarmup = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create moving averages
		var fastMa = new SimpleMovingAverage { Length = FastPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowPeriod };

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		// Enable position protection using market orders
		StartProtection(useMarketOrders: true);

		// Chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure indicators are formed and trading is allowed
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var osc = fastValue - slowValue;

		if (_isWarmup)
		{
			_prevOsc1 = osc;
			_prevOsc2 = osc;
			_isWarmup = false;
			return;
		}

		var buySignal = _prevOsc2 > _prevOsc1 && _prevOsc1 < osc;
		var sellSignal = _prevOsc2 < _prevOsc1 && _prevOsc1 > osc;

		if (buySignal)
		{
			if (EnableSellClose && Position < 0)
				BuyMarket(-Position);

			if (EnableBuyOpen && Position <= 0)
				BuyMarket();
		}
		else if (sellSignal)
		{
			if (EnableBuyClose && Position > 0)
				SellMarket(Position);

			if (EnableSellOpen && Position >= 0)
				SellMarket();
		}

		_prevOsc2 = _prevOsc1;
		_prevOsc1 = osc;
	}
}