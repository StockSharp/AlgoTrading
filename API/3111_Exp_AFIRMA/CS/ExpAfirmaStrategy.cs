namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive FIR/ARMA crossover strategy converted from MetaTrader.
/// Buys when the ARMA component turns upward and sells when it turns downward.
/// </summary>
public class ExpAfirmaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _periods;
	private readonly StrategyParam<int> _taps;
	private readonly StrategyParam<AfirmaIndicator.WindowType> _window;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyEntries;
	private readonly StrategyParam<bool> _sellEntries;
	private readonly StrategyParam<bool> _buyExits;
	private readonly StrategyParam<bool> _sellExits;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private readonly List<decimal> _armaHistory = new();
	private AfirmaIndicator? _indicator;

	/// <summary>
	/// Initializes a new instance of <see cref="ExpAfirmaStrategy"/>.
	/// </summary>
	public ExpAfirmaStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order volume used for entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for AFIRMA calculations", "General");

		_periods = Param(nameof(Periods), 4)
			.SetGreaterThanZero()
			.SetDisplay("Bandwidth", "Reciprocal bandwidth parameter of the FIR stage", "Indicator");

		_taps = Param(nameof(Taps), 21)
			.SetGreaterThanZero()
			.SetDisplay("Taps", "Number of FIR coefficients (odd value)", "Indicator");

		_window = Param(nameof(Window), AfirmaIndicator.WindowType.Blackman)
			.SetDisplay("Window", "Window function applied to FIR coefficients", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Number of closed bars to look back for confirmation", "Signals");

		_buyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Buy Entries", "Allow opening long positions", "Trading");

		_sellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Sell Entries", "Allow opening short positions", "Trading");

		_buyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Enable Buy Exits", "Allow closing existing longs", "Trading");

		_sellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Enable Sell Exits", "Allow closing existing shorts", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss (price)", "Protective stop distance expressed in price units", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetDisplay("Take Profit (price)", "Protective target distance expressed in price units", "Risk");
	}

	/// <summary>
	/// Base order volume used for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Reciprocal bandwidth parameter of the FIR stage.
	/// </summary>
	public int Periods
	{
		get => _periods.Value;
		set => _periods.Value = value;
	}

	/// <summary>
	/// Number of FIR coefficients used by AFIRMA (odd value).
	/// </summary>
	public int Taps
	{
		get => _taps.Value;
		set => _taps.Value = value;
	}

	/// <summary>
	/// Window function applied to FIR coefficients.
	/// </summary>
	public AfirmaIndicator.WindowType Window
	{
		get => _window.Value;
		set => _window.Value = value;
	}

	/// <summary>
	/// Number of closed bars to look back for confirmation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _buyEntries.Value;
		set => _buyEntries.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _sellEntries.Value;
		set => _sellEntries.Value = value;
	}

	/// <summary>
	/// Enable closing existing long positions on bearish signals.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _buyExits.Value;
		set => _buyExits.Value = value;
	}

	/// <summary>
	/// Enable closing existing short positions on bullish signals.
	/// </summary>
	public bool EnableSellExits
	{
		get => _sellExits.Value;
		set => _sellExits.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in price units.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Protective target distance expressed in price units.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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
		_armaHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_armaHistory.Clear();

		_indicator = new AfirmaIndicator
		{
			Periods = Math.Max(1, Periods),
			Taps = Math.Max(3, Taps),
			Window = Window
		};

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_indicator, ProcessCandle)
			.Start();

		ConfigureProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}
	}

	private void ConfigureProtection()
	{
		Unit? stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Price) : null;
		Unit? takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Price) : null;

		if (stopLoss != null || takeProfit != null)
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_indicator == null || !_indicator.IsFormed)
			return;

		if (value is not AfirmaValue afirma)
			return;

		var arma = afirma.Arma;

		_armaHistory.Add(arma);

		var maxHistory = Math.Max(8, SignalBar + 6);
		if (_armaHistory.Count > maxHistory)
			_armaHistory.RemoveRange(0, _armaHistory.Count - maxHistory);

		var signalOffset = Math.Max(0, SignalBar - 1);
		var required = signalOffset + 3;
		if (_armaHistory.Count < required)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var current = GetArmaValue(signalOffset);
		var previous = GetArmaValue(signalOffset + 1);
		var prior = GetArmaValue(signalOffset + 2);

		var closeLong = EnableBuyExits && previous > prior && Position > 0m;
		var closeShort = EnableSellExits && previous < prior && Position < 0m;
		var openLong = EnableBuyEntries && current > previous && previous < prior && Position <= 0m && TradeVolume > 0m;
		var openShort = EnableSellEntries && current < previous && previous > prior && Position >= 0m && TradeVolume > 0m;

		if (closeLong || closeShort || openLong || openShort)
			CancelActiveOrders();

		if (closeLong)
			SellMarket(Position);

		if (closeShort)
			BuyMarket(-Position);

		var targetVolume = TradeVolume;
		if (openLong)
		{
			var desired = targetVolume;
			var delta = desired - Position;
			if (delta > 0m)
				BuyMarket(delta);
		}
		else if (openShort)
		{
			var desired = -targetVolume;
			var delta = desired - Position;
			if (delta < 0m)
				SellMarket(-delta);
		}
	}

	private decimal GetArmaValue(int index)
	{
		var idx = _armaHistory.Count - 1 - index;
		return idx >= 0 ? _armaHistory[idx] : _armaHistory[0];
	}
}

/// <summary>
/// AFIRMA indicator implementation producing FIR and ARMA components.
/// </summary>
public sealed class AfirmaIndicator : BaseIndicator<decimal>
{
	private readonly List<decimal> _prices = new();
	private decimal[] _weights = Array.Empty<decimal>();
	private decimal _weightSum;
	private int _effectiveTaps;
	private int _halfWindow;
	private decimal _sx2;
	private decimal _sx3;
	private decimal _sx4;
	private decimal _sx5;
	private decimal _sx6;
	private decimal _denominator;
	private decimal? _previousFir;

	/// <summary>
	/// Available window functions for the FIR design.
	/// </summary>
	public enum WindowType
	{
		Rectangular = 1,
		Hanning1,
		Hanning2,
		Blackman,
		BlackmanHarris
	}

	/// <summary>
	/// Reciprocal bandwidth parameter used in the FIR stage.
	/// </summary>
	public int Periods { get; set; } = 4;

	/// <summary>
	/// Number of FIR coefficients (odd value).
	/// </summary>
	public int Taps { get; set; } = 21;

	/// <summary>
	/// Window function applied to the FIR coefficients.
	/// </summary>
	public WindowType Window { get; set; } = WindowType.Blackman;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		IsFormed = false;

		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new AfirmaValue(this, input, 0m, 0m);

		if (_weights.Length == 0 || _effectiveTaps != GetEffectiveTaps() || _halfWindow <= 0)
			RebuildCoefficients();

		_prices.Add(candle.ClosePrice);
		while (_prices.Count > _effectiveTaps)
			_prices.RemoveAt(0);

		if (_prices.Count < _effectiveTaps)
			return new AfirmaValue(this, input, 0m, 0m);

		var fir = ComputeFir();

		if (_previousFir is null)
		{
			_previousFir = fir;
			return new AfirmaValue(this, input, fir, fir);
		}

		var arma = ComputeArma(fir, _previousFir.Value);
		_previousFir = fir;

		IsFormed = true;
		return new AfirmaValue(this, input, fir, arma);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_prices.Clear();
		_weights = Array.Empty<decimal>();
		_weightSum = 0m;
		_effectiveTaps = 0;
		_halfWindow = 0;
		_previousFir = null;
	}

	private int GetEffectiveTaps()
	{
		var taps = Math.Max(3, Taps);
		if (taps % 2 == 0)
			taps += 1;
		return taps;
	}

	private void RebuildCoefficients()
	{
		_effectiveTaps = GetEffectiveTaps();
		_halfWindow = (_effectiveTaps - 1) / 2;

		_weights = new decimal[_effectiveTaps];
		_weightSum = 0m;

		var middle = _effectiveTaps / 2.0;
		var periods = Math.Max(1, Periods);

		for (var k = 0; k < _effectiveTaps; k++)
		{
			double weight = Window switch
			{
				WindowType.Rectangular => 1.0,
				WindowType.Hanning1 => 0.50 - 0.50 * Math.Cos(2.0 * Math.PI * k / _effectiveTaps),
				WindowType.Hanning2 => 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * k / _effectiveTaps),
				WindowType.Blackman => 0.42 - 0.50 * Math.Cos(2.0 * Math.PI * k / _effectiveTaps) + 0.08 * Math.Cos(4.0 * Math.PI * k / _effectiveTaps),
				WindowType.BlackmanHarris => 0.35875 - 0.48829 * Math.Cos(2.0 * Math.PI * k / _effectiveTaps) + 0.14128 * Math.Cos(4.0 * Math.PI * k / _effectiveTaps) - 0.01168 * Math.Cos(6.0 * Math.PI * k / _effectiveTaps),
				_ => 1.0
			};

			if (Math.Abs(k - middle) > double.Epsilon)
			{
				var numerator = Math.Sin(Math.PI * (k - middle) / periods);
				var denominator = Math.PI * (k - middle) / periods;
				if (Math.Abs(denominator) > double.Epsilon)
					weight *= numerator / denominator;
			}

			var decWeight = (decimal)weight;
			_weights[k] = decWeight;
			_weightSum += decWeight;
		}

		if (_weightSum == 0m)
			_weightSum = 1m;

		var n = (decimal)_halfWindow;
		_sx2 = (2m * n + 1m) / 3m;
		_sx3 = n * (n + 1m) / 2m;
		_sx4 = _sx2 * (3m * n * n + 3m * n - 1m) / 5m;
		_sx5 = _sx3 * (2m * n * n + 2m * n - 1m) / 3m;
		_sx6 = _sx2 * (3m * n * n * n * (n + 2m) - 3m * n + 1m) / 7m;
		_denominator = _sx6 * _sx4 / (_sx5 == 0m ? 1m : _sx5) - _sx5;
	}

	private decimal ComputeFir()
	{
		var sum = 0m;
		var count = _prices.Count;
		for (var i = 0; i < _weights.Length; i++)
		{
			var price = _prices[count - 1 - i];
			sum += price * _weights[i];
		}
		return sum / _weightSum;
	}

	private decimal ComputeArma(decimal fir, decimal previousFir)
	{
		var n = _halfWindow;
		if (n <= 0)
			return fir;

		var nDec = (decimal)n;
		var sx2y = 0m;
		var sx3y = 0m;

		for (var i = 0; i <= n; i++)
		{
			var lag = n - i;
			var price = _prices[_prices.Count - 1 - lag];
			var iDec = (decimal)i;
			sx2y += iDec * iDec * price;
			sx3y += iDec * iDec * iDec * price;
		}

		sx2y = 2m * sx2y / nDec / (nDec + 1m);
		sx3y = 2m * sx3y / nDec / (nDec + 1m);

		var a0 = fir;
		var a1 = fir - previousFir;
		var p = sx2y - a0 * _sx2 - a1 * _sx3;
		var q = sx3y - a0 * _sx3 - a1 * _sx4;

		if (_sx5 == 0m || Math.Abs(_denominator) < 1e-12m)
			return fir;

		var a2 = (p * _sx6 / _sx5 - q) / _denominator;
		var a3 = (q * _sx4 / _sx5 - p) / _denominator;
		var k = nDec;

		return a0 + k * a1 + k * k * a2 + k * k * k * a3;
	}
}

/// <summary>
/// Indicator value carrying FIR and ARMA components of <see cref="AfirmaIndicator"/>.
/// </summary>
public sealed class AfirmaValue : ComplexIndicatorValue
{
	public AfirmaValue(IIndicator indicator, IIndicatorValue input, decimal fir, decimal arma)
		: base(indicator, input, (nameof(Fir), fir), (nameof(Arma), arma))
	{
	}

	/// <summary>
	/// FIR output of the AFIRMA filter.
	/// </summary>
	public decimal Fir => (decimal)GetValue(nameof(Fir));

	/// <summary>
	/// ARMA forecast output of the AFIRMA filter.
	/// </summary>
	public decimal Arma => (decimal)GetValue(nameof(Arma));
}
