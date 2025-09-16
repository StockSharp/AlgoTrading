using System;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// True Strength Index calculated on DeMarker indicator.
/// Trades on crossover between TSI and its signal line.
/// </summary>
public class TSIDeMarkerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _signalLength;
	
	private DeMarker _demarker;
	private TrueStrengthIndex _tsi;
	private SimpleMovingAverage _signal;
	
	private decimal _prevTsi;
	private decimal _prevSignal;
	private bool _isFirst = true;
	
	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Period for DeMarker indicator.
	/// </summary>
	public int DemarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}
	
	/// <summary>
	/// Short smoothing length for TSI.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}
	
	/// <summary>
	/// Long smoothing length for TSI.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}
	
	/// <summary>
	/// Moving average period for signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TSIDeMarkerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");
		_demarkerPeriod = Param(nameof(DemarkerPeriod), 25)
		.SetDisplay("DeMarker Period", "Period for DeMarker", "Indicators");
		_shortLength = Param(nameof(ShortLength), 5)
		.SetDisplay("Short Length", "Short EMA for TSI", "Indicators");
		_longLength = Param(nameof(LongLength), 8)
		.SetDisplay("Long Length", "Long EMA for TSI", "Indicators");
		_signalLength = Param(nameof(SignalLength), 20)
		.SetDisplay("Signal Length", "Smoothing for TSI", "Indicators");
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		_demarker = new DeMarker { Length = DemarkerPeriod };
		_tsi = new TrueStrengthIndex { ShortLength = ShortLength, LongLength = LongLength };
		_signal = new SimpleMovingAverage { Length = SignalLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tsi);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var demVal = _demarker.Process(candle).ToDecimal();
		var tsiVal = _tsi.Process(demVal, candle.ServerTime).ToDecimal();
		var signalVal = _signal.Process(tsiVal, candle.ServerTime).ToDecimal();
		
		if (_isFirst)
		{
			_prevTsi = tsiVal;
			_prevSignal = signalVal;
			_isFirst = false;
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevTsi = tsiVal;
			_prevSignal = signalVal;
			return;
		}
		
		var crossUp = _prevTsi <= _prevSignal && tsiVal > signalVal;
		var crossDown = _prevTsi >= _prevSignal && tsiVal < signalVal;
		
		if (crossUp && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));
		
		_prevTsi = tsiVal;
		_prevSignal = signalVal;
	}
}
