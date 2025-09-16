
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining RSI and Stochastic oscillator signals.
/// </summary>
public class JBrainUltraRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<AlgorithmMode> _mode;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<DataType> _candleType;
	
	private RelativeStrengthIndex _rsi;
	private StochasticOscillator _stochastic;
	private decimal? _currentRsi;
	private decimal? _prevRsi;
	private decimal? _prevK;
	private decimal? _prevD;
	
	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}
	
	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}
	
	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}
	
	/// <summary>
	/// Mode combining indicators.
	/// </summary>
	public AlgorithmMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}
	
	/// <summary>
	/// Permission to open long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}
	
	/// <summary>
	/// Permission to open short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}
	
	/// <summary>
	/// Permission to close long positions.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}
	
	/// <summary>
	/// Permission to close short positions.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}
	
	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="JBrainUltraRsiStrategy"/>.
	/// </summary>
	public JBrainUltraRsiStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
		.SetCanOptimize(true);
		
		_stochLength = Param(nameof(StochLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "Period for %K line", "Indicators")
		.SetCanOptimize(true);
		
		_signalLength = Param(nameof(SignalLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "Period for %D line", "Indicators")
		.SetCanOptimize(true);
		
		_mode = Param(nameof(Mode), AlgorithmMode.Composition)
		.SetDisplay("Mode", "Algorithm to enter the market", "General");
		
		_allowLongEntry = Param(nameof(AllowLongEntry), true)
		.SetDisplay("Allow Long Entry", "Permission to open long positions", "Trading");
		
		_allowShortEntry = Param(nameof(AllowShortEntry), true)
		.SetDisplay("Allow Short Entry", "Permission to open short positions", "Trading");
		
		_allowLongExit = Param(nameof(AllowLongExit), true)
		.SetDisplay("Allow Long Exit", "Permission to close long positions", "Trading");
		
		_allowShortExit = Param(nameof(AllowShortExit), true)
		.SetDisplay("Allow Short Exit", "Permission to close short positions", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		
		_rsi = default;
		_stochastic = default;
		_currentRsi = default;
		_prevRsi = default;
		_prevK = default;
		_prevD = default;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stochastic = new StochasticOscillator
		{
			K = { Length = StochLength },
			D = { Length = SignalLength },
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, OnRsi)
		.BindEx(_stochastic, OnStochastic)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}
	
	private void OnRsi(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_currentRsi = rsiValue;
	}
	
	private void OnStochastic(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var d = stoch.D;
		
		if (_currentRsi is not decimal rsi)
		{
			_prevK = k;
			_prevD = d;
			return;
		}
		
		ProcessSignals(rsi, k, d);
		
		_currentRsi = null;
	}
	
	private void ProcessSignals(decimal rsi, decimal k, decimal d)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsi;
			_prevK = k;
			_prevD = d;
			return;
		}
		
		var rsiUp = _prevRsi is decimal pr && pr <= 50m && rsi > 50m;
		var rsiDown = _prevRsi is decimal pr2 && pr2 >= 50m && rsi < 50m;
		var stochUp = _prevK is decimal pk && _prevD is decimal pd && pk <= pd && k > d;
		var stochDown = _prevK is decimal pk2 && _prevD is decimal pd2 && pk2 >= pd2 && k < d;
		
		var buySignal = false;
		var sellSignal = false;
		
		switch (Mode)
		{
			case AlgorithmMode.JBrainSig1Filter:
			buySignal = rsiUp && k > d;
			sellSignal = rsiDown && k < d;
			break;
			case AlgorithmMode.UltraRsiFilter:
			buySignal = stochUp && rsi > 50m;
			sellSignal = stochDown && rsi < 50m;
			break;
			case AlgorithmMode.Composition:
			buySignal = rsiUp && stochUp;
			sellSignal = rsiDown && stochDown;
			break;
		}
		
		if (buySignal)
		{
			if (Position < 0 && AllowShortExit)
			BuyMarket(Math.Abs(Position));
			if (AllowLongEntry && Position <= 0)
			BuyMarket(Volume);
		}
		else if (sellSignal)
		{
			if (Position > 0 && AllowLongExit)
			SellMarket(Position);
			if (AllowShortEntry && Position >= 0)
			SellMarket(Volume);
		}
		
		_prevRsi = rsi;
		_prevK = k;
		_prevD = d;
	}
}

/// <summary>
/// Combination modes for indicators.
/// </summary>
public enum AlgorithmMode
{
	JBrainSig1Filter,
	UltraRsiFilter,
	Composition
}
