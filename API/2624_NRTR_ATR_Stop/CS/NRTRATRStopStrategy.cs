using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the NRTR ATR Stop indicator behavior to generate trading signals.
/// The logic follows the original MetaTrader implementation: ATR-based trailing levels switch direction
/// when price crosses the opposite stop, producing long or short entries.
/// </summary>
public class NRTRATRStopStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _coefficient;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private AverageTrueRange _atr = null!;
	private readonly Queue<SignalInfo> _signals = new();

	private decimal _upperStop;
	private decimal _lowerStop;
	private int _trend;
	private bool _hasStops;
	private bool _hasPrevious;
	private decimal _prevHigh;
	private decimal _prevLow;

	/// <summary>
	/// ATR period used by the trailing stop calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set
		{
			var normalized = Math.Max(1, value);
			_atrPeriod.Value = normalized;

			if (_atr != null)
				_atr.Length = normalized;
		}
	}

	/// <summary>
	/// Multiplier applied to ATR to build stop levels.
	/// </summary>
	public decimal Coefficient
	{
		get => _coefficient.Value;
		set
		{
			var normalized = Math.Max(0.1m, value);
			_coefficient.Value = normalized;
		}
	}

	/// <summary>
	/// Number of closed candles to delay signal execution.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set
		{
			var normalized = Math.Max(0, value);
			_signalBar.Value = normalized;
		}
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing existing long positions on sell signals.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing existing short positions on buy signals.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Initializes parameters for the NRTR ATR Stop strategy.
	/// </summary>
	public NRTRATRStopStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 20)
			.SetDisplay("ATR Period", "Number of candles used for ATR calculation", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_coefficient = Param(nameof(Coefficient), 2m)
			.SetDisplay("Coefficient", "Multiplier applied to ATR when building the stop levels", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "How many closed candles to wait before acting on a signal", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame of the candles used for calculations", "General");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow the strategy to open long positions", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow the strategy to open short positions", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing long positions when a sell signal appears", "Risk");

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing short positions when a buy signal appears", "Risk");
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

		_signals.Clear();
		_upperStop = 0m;
		_lowerStop = 0m;
		_trend = 0;
		_hasStops = false;
		_hasPrevious = false;
		_prevHigh = 0m;
		_prevLow = 0m;
		_atr = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed)
		{
			UpdatePreviousValues(candle);
			return;
		}

		if (!_hasPrevious)
		{
			UpdatePreviousValues(candle);
			return;
		}

		var previousTrend = _trend;
		var trend = previousTrend;
		var upperStop = _upperStop;
		var lowerStop = _lowerStop;
		var rez = Coefficient * atrValue;

		if (!_hasStops)
		{
			upperStop = _prevLow - rez;
			lowerStop = _prevHigh + rez;
			_hasStops = true;
		}

		if (trend <= 0 && _prevLow > lowerStop)
		{
			upperStop = _prevLow - rez;
			trend = 1;
		}

		if (trend >= 0 && _prevHigh < upperStop)
		{
			lowerStop = _prevHigh + rez;
			trend = -1;
		}

		if (trend >= 0)
		{
			if (_prevLow > upperStop + rez)
				upperStop = _prevLow - rez;
		}

		if (trend <= 0)
		{
			if (_prevHigh < lowerStop - rez)
				lowerStop = _prevHigh + rez;
		}

		var buySignal = trend > 0 && previousTrend <= 0;
		var sellSignal = trend < 0 && previousTrend >= 0;

		_trend = trend;
		_upperStop = upperStop;
		_lowerStop = lowerStop;

		_signals.Enqueue(new SignalInfo(buySignal, sellSignal, upperStop, lowerStop, candle.CloseTime, candle.ClosePrice));

		if (_signals.Count <= SignalBar)
		{
			UpdatePreviousValues(candle);
			return;
		}

		var signal = _signals.Dequeue();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousValues(candle);
			return;
		}

		if (signal.Buy)
			HandleBuy(signal);

		if (signal.Sell)
			HandleSell(signal);

		UpdatePreviousValues(candle);
	}

	private void HandleBuy(SignalInfo signal)
	{
		var volume = CalculateBuyVolume();
		if (volume <= 0)
			return;

		BuyMarket(volume);
		LogInfo($"Buy signal at {signal.Time:u}. Close={signal.ClosePrice:0.#####}, upper stop={signal.UpperStop:0.#####}, lower stop={signal.LowerStop:0.#####}, volume={volume:0.#####}.");
	}

	private void HandleSell(SignalInfo signal)
	{
		var volume = CalculateSellVolume();
		if (volume <= 0)
			return;

		SellMarket(volume);
		LogInfo($"Sell signal at {signal.Time:u}. Close={signal.ClosePrice:0.#####}, upper stop={signal.UpperStop:0.#####}, lower stop={signal.LowerStop:0.#####}, volume={volume:0.#####}.");
	}

	private decimal CalculateBuyVolume()
	{
		var volume = 0m;

		if (EnableShortExit && Position < 0)
			volume += Math.Abs(Position);

		if (EnableLongEntry && Position <= 0 && Volume > 0)
			volume += Volume;

		return volume;
	}

	private decimal CalculateSellVolume()
	{
		var volume = 0m;

		if (EnableLongExit && Position > 0)
			volume += Position;

		if (EnableShortEntry && Position >= 0 && Volume > 0)
			volume += Volume;

		return volume;
	}

	private void UpdatePreviousValues(ICandleMessage candle)
	{
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_hasPrevious = true;
	}

	private sealed record SignalInfo(bool Buy, bool Sell, decimal UpperStop, decimal LowerStop, DateTimeOffset Time, decimal ClosePrice);
}
