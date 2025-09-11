using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades when two MACD indicators agree on direction.
/// </summary>
public class DoubleMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength1;
	private readonly StrategyParam<int> _slowLength1;
	private readonly StrategyParam<int> _signalLength1;
	private readonly StrategyParam<MaType> _maType1;
	
	private readonly StrategyParam<int> _fastLength2;
	private readonly StrategyParam<int> _slowLength2;
	private readonly StrategyParam<int> _signalLength2;
	private readonly StrategyParam<MaType> _maType2;
	
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private MovingAverageConvergenceDivergenceSignal _macd1;
	private MovingAverageConvergenceDivergenceSignal _macd2;
	
	/// <summary>
	/// Fast length for first MACD.
	/// </summary>
	public int FastLength1
	{
		get => _fastLength1.Value;
		set => _fastLength1.Value = value;
	}
	
	/// <summary>
	/// Slow length for first MACD.
	/// </summary>
	public int SlowLength1
	{
		get => _slowLength1.Value;
		set => _slowLength1.Value = value;
	}
	
	/// <summary>
	/// Signal length for first MACD.
	/// </summary>
	public int SignalLength1
	{
		get => _signalLength1.Value;
		set => _signalLength1.Value = value;
	}
	
	/// <summary>
	/// Moving average type for first MACD.
	/// </summary>
	public MaType MaType1
	{
		get => _maType1.Value;
		set => _maType1.Value = value;
	}
	
	/// <summary>
	/// Fast length for second MACD.
	/// </summary>
	public int FastLength2
	{
		get => _fastLength2.Value;
		set => _fastLength2.Value = value;
	}
	
	/// <summary>
	/// Slow length for second MACD.
	/// </summary>
	public int SlowLength2
	{
		get => _slowLength2.Value;
		set => _slowLength2.Value = value;
	}
	
	/// <summary>
	/// Signal length for second MACD.
	/// </summary>
	public int SignalLength2
	{
		get => _signalLength2.Value;
		set => _signalLength2.Value = value;
	}
	
	/// <summary>
	/// Moving average type for second MACD.
	/// </summary>
	public MaType MaType2
	{
		get => _maType2.Value;
		set => _maType2.Value = value;
	}
	
	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initialize <see cref="DoubleMacdStrategy"/>.
	/// </summary>
	public DoubleMacdStrategy()
	{
		_fastLength1 = Param(nameof(FastLength1), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast Length 1", "Fast length for first MACD", "MACD 1")
		.SetCanOptimize(true);
		
		_slowLength1 = Param(nameof(SlowLength1), 26)
		.SetGreaterThanZero()
		.SetDisplay("Slow Length 1", "Slow length for first MACD", "MACD 1")
		.SetCanOptimize(true);
		
		_signalLength1 = Param(nameof(SignalLength1), 9)
		.SetGreaterThanZero()
		.SetDisplay("Signal Length 1", "Signal length for first MACD", "MACD 1")
		.SetCanOptimize(true);
		
		_maType1 = Param(nameof(MaType1), MaType.Ema)
		.SetDisplay("MA Type 1", "MA type for first MACD", "MACD 1");
		
		_fastLength2 = Param(nameof(FastLength2), 24)
		.SetGreaterThanZero()
		.SetDisplay("Fast Length 2", "Fast length for second MACD", "MACD 2")
		.SetCanOptimize(true);
		
		_slowLength2 = Param(nameof(SlowLength2), 52)
		.SetGreaterThanZero()
		.SetDisplay("Slow Length 2", "Slow length for second MACD", "MACD 2")
		.SetCanOptimize(true);
		
		_signalLength2 = Param(nameof(SignalLength2), 9)
		.SetGreaterThanZero()
		.SetDisplay("Signal Length 2", "Signal length for second MACD", "MACD 2")
		.SetCanOptimize(true);
		
		_maType2 = Param(nameof(MaType2), MaType.Ema)
		.SetDisplay("MA Type 2", "MA type for second MACD", "MACD 2");
		
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_macd1 = null;
		_macd2 = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_macd1 = CreateMacd(FastLength1, SlowLength1, SignalLength1, MaType1);
		_macd2 = CreateMacd(FastLength2, SlowLength2, SignalLength2, MaType2);
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd1, _macd2, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd1);
			DrawIndicator(area, _macd2);
			DrawOwnTrades(area);
		}
		
		StartProtection(
		takeProfit: null,
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macd1Value, IIndicatorValue macd2Value)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var m1 = (MovingAverageConvergenceDivergenceSignalValue)macd1Value;
		var m2 = (MovingAverageConvergenceDivergenceSignalValue)macd2Value;
		
		var longSignal = m1.Macd > m1.Signal && m2.Macd > m2.Signal;
		var shortSignal = m1.Macd < m1.Signal && m2.Macd < m2.Signal;
		
		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Long: MACD1 {m1.Macd:F5} > {m1.Signal:F5} & MACD2 {m2.Macd:F5} > {m2.Signal:F5}");
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Short: MACD1 {m1.Macd:F5} < {m1.Signal:F5} & MACD2 {m2.Macd:F5} < {m2.Signal:F5}");
		}
	}
	
	private static MovingAverageConvergenceDivergenceSignal CreateMacd(int fast, int slow, int signal, MaType type)
	{
		return new()
		{
			Macd =
			{
				ShortMa = CreateMa(type, fast),
				LongMa = CreateMa(type, slow),
			},
			SignalMa = CreateMa(type, signal)
		};
	}
	
	private static LengthIndicator<decimal> CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.Sma => new SimpleMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}
	
	/// <summary>
	/// Supported moving average types.
	/// </summary>
	public enum MaType
	{
		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,
		
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma
	}
}
