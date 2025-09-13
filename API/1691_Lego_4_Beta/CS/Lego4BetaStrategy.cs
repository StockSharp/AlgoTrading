using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Modular strategy combining moving averages, Stochastic oscillator and RSI filters.
/// Translated from the MQL script "exp_Lego_4_Beta".
/// </summary>
public class Lego4BetaStrategy : Strategy
{
	private readonly StrategyParam<bool> _useMaOpen;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<bool> _useStochasticOpen;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _stochBuyLevel;
	private readonly StrategyParam<decimal> _stochSellLevel;
	private readonly StrategyParam<bool> _useRsiClose;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiHigh;
	private readonly StrategyParam<decimal> _rsiLow;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _fastMa;
	private MovingAverage _slowMa;
	private StochasticOscillator _stochastic;
	private RelativeStrengthIndex _rsi;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Use moving average cross for entry signals.
	/// </summary>
	public bool UseMaOpen
	{
		get => _useMaOpen.Value;
		set => _useMaOpen.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaType MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Enable Stochastic oscillator filter for entries.
	/// </summary>
	public bool UseStochasticOpen
	{
		get => _useStochasticOpen.Value;
		set => _useStochasticOpen.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator main period.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Smoothing period for %K line.
	/// </summary>
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for %D line.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold for Stochastic.
	/// </summary>
	public decimal StochSellLevel
	{
		get => _stochSellLevel.Value;
		set => _stochSellLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold for Stochastic.
	/// </summary>
	public decimal StochBuyLevel
	{
		get => _stochBuyLevel.Value;
		set => _stochBuyLevel.Value = value;
	}

	/// <summary>
	/// Use RSI for exit signals.
	/// </summary>
	public bool UseRsiClose
	{
		get => _useRsiClose.Value;
		set => _useRsiClose.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiHigh
	{
		get => _rsiHigh.Value;
		set => _rsiHigh.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiLow
	{
		get => _rsiLow.Value;
		set => _rsiLow.Value = value;
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
	/// Initializes a new instance of <see cref="Lego4BetaStrategy"/>.
	/// </summary>
	public Lego4BetaStrategy()
	{
		_useMaOpen = Param(nameof(UseMaOpen), true)
		.SetDisplay("Use MA", "Enable MA cross entry", "Entry");

		_fastMaLength = Param(nameof(FastMaLength), 4)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Length", "Length of fast MA", "Moving Average")
		.SetCanOptimize(true);

		_slowMaLength = Param(nameof(SlowMaLength), 67)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Length", "Length of slow MA", "Moving Average")
		.SetCanOptimize(true);

		_maType = Param(nameof(MaType), MaType.EMA)
		.SetDisplay("MA Type", "Moving average type", "Moving Average");

		_useStochasticOpen = Param(nameof(UseStochasticOpen), false)
		.SetDisplay("Use Stochastic", "Enable Stochastic filter", "Entry");

		_stochLength = Param(nameof(StochLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stoch Length", "Main period for Stochastic", "Stochastic")
		.SetCanOptimize(true);

		_stochKPeriod = Param(nameof(StochKPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Smoothing for %K line", "Stochastic")
		.SetCanOptimize(true);

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Smoothing for %D line", "Stochastic")
		.SetCanOptimize(true);

		_stochBuyLevel = Param(nameof(StochBuyLevel), 20m)
		.SetDisplay("Stoch Buy", "Oversold level", "Stochastic")
		.SetCanOptimize(true);

		_stochSellLevel = Param(nameof(StochSellLevel), 80m)
		.SetDisplay("Stoch Sell", "Overbought level", "Stochastic")
		.SetCanOptimize(true);

		_useRsiClose = Param(nameof(UseRsiClose), false)
		.SetDisplay("Use RSI", "Enable RSI exit", "Exit");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of RSI", "Exit")
		.SetCanOptimize(true);

		_rsiHigh = Param(nameof(RsiHigh), 70m)
		.SetDisplay("RSI High", "Overbought level", "Exit")
		.SetCanOptimize(true);

		_rsiLow = Param(nameof(RsiLow), 30m)
		.SetDisplay("RSI Low", "Oversold level", "Exit")
		.SetCanOptimize(true);

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

		_prevFast = default;
		_prevSlow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_fastMa = CreateMa(MaType, FastMaLength);
		_slowMa = CreateMa(MaType, SlowMaLength);
		_stochastic = new StochasticOscillator
		{
			Length = StochLength,
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod },
		};
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_fastMa, _slowMa, _stochastic, _rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			var oscArea = CreateChartArea();
			DrawIndicator(oscArea, _stochastic);
			DrawIndicator(oscArea, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastVal, IIndicatorValue slowVal, IIndicatorValue stochVal, IIndicatorValue rsiVal)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var fast = fastVal.ToDecimal();
		var slow = slowVal.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochVal;
		var k = stoch.K;
		var d = stoch.D;
		var rsi = rsiVal.ToDecimal();

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (UseMaOpen && crossUp)
		{
			var allow = !UseStochasticOpen || k < StochBuyLevel;
			if (allow && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (UseMaOpen && crossDown)
		{
			var allow = !UseStochasticOpen || k > StochSellLevel;
			if (allow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}

		if (UseRsiClose)
		{
			if (Position > 0 && rsi > RsiHigh)
			SellMarket(Math.Abs(Position));
			else if (Position < 0 && rsi < RsiLow)
			BuyMarket(Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private MovingAverage CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.SMA => new SimpleMovingAverage { Length = length },
			MaType.WMA => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}
}

/// <summary>
/// Moving average type.
/// </summary>
public enum MaType
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	SMA,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	EMA,

	/// <summary>
	/// Weighted moving average.
	/// </summary>
	WMA,
}
