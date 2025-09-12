using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Displays common indicator values for the current security.
/// </summary>
public class IndicatorPanelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _diLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<int> _mfiLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<bool> _ma1IsEma;
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<bool> _ma2IsEma;
	private readonly StrategyParam<int> _ma2Length;
	
	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private DirectionalIndex _dmi;
	private AverageDirectionalIndex _adx;
	private CommodityChannelIndex _cci;
	private MoneyFlowIndex _mfi;
	private Momentum _momentum;
	private IIndicator _ma1;
	private IIndicator _ma2;
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public IndicatorPanelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "Period for RSI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 1);
		
		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);
		
		_diLength = Param(nameof(DiLength), 14)
		.SetDisplay("DI Length", "Directional Index period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_adxLength = Param(nameof(AdxLength), 14)
		.SetDisplay("ADX Length", "ADX smoothing period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_cciLength = Param(nameof(CciLength), 20)
		.SetDisplay("CCI Length", "Period for Commodity Channel Index", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);
		
		_mfiLength = Param(nameof(MfiLength), 20)
		.SetDisplay("MFI Length", "Period for Money Flow Index", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);
		
		_momentumLength = Param(nameof(MomentumLength), 10)
		.SetDisplay("Momentum Length", "Period for Momentum", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_ma1IsEma = Param(nameof(Ma1IsEma), false)
		.SetDisplay("MA1 Is EMA", "Use EMA for MA1", "Indicators");
		
		_ma1Length = Param(nameof(Ma1Length), 50)
		.SetDisplay("MA1 Length", "Period for first moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 100, 10);
		
		_ma2IsEma = Param(nameof(Ma2IsEma), false)
		.SetDisplay("MA2 Is EMA", "Use EMA for MA2", "Indicators");
		
		_ma2Length = Param(nameof(Ma2Length), 200)
		.SetDisplay("MA2 Length", "Period for second moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(50, 300, 10);
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
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}
	
	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}
	
	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}
	
	/// <summary>
	/// MACD signal EMA period.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}
	
	/// <summary>
	/// Directional Index period.
	/// </summary>
	public int DiLength
	{
		get => _diLength.Value;
		set => _diLength.Value = value;
	}
	
	/// <summary>
	/// ADX smoothing period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}
	
	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}
	
	/// <summary>
	/// MFI period.
	/// </summary>
	public int MfiLength
	{
		get => _mfiLength.Value;
		set => _mfiLength.Value = value;
	}
	
	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}
	
	/// <summary>
	/// Use EMA for first moving average.
	/// </summary>
	public bool Ma1IsEma
	{
		get => _ma1IsEma.Value;
		set => _ma1IsEma.Value = value;
	}
	
	/// <summary>
	/// First moving average period.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}
	
	/// <summary>
	/// Use EMA for second moving average.
	/// </summary>
	public bool Ma2IsEma
	{
		get => _ma2IsEma.Value;
		set => _ma2IsEma.Value = value;
	}
	
	/// <summary>
	/// Second moving average period.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
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
		
		_rsi?.Reset();
		_macd?.Reset();
		_dmi?.Reset();
		_adx?.Reset();
		_cci?.Reset();
		_mfi?.Reset();
		_momentum?.Reset();
		_ma1?.Reset();
		_ma2?.Reset();
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength }
		};
		_dmi = new DirectionalIndex { Length = DiLength };
		_adx = new AverageDirectionalIndex { Length = AdxLength };
		_cci = new CommodityChannelIndex { Length = CciLength };
		_mfi = new MoneyFlowIndex { Length = MfiLength };
		_momentum = new Momentum { Length = MomentumLength };
		_ma1 = Ma1IsEma ? new ExponentialMovingAverage { Length = Ma1Length } : new SimpleMovingAverage { Length = Ma1Length };
		_ma2 = Ma2IsEma ? new ExponentialMovingAverage { Length = Ma2Length } : new SimpleMovingAverage { Length = Ma2Length };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _dmi, _adx, _rsi, _cci, _mfi, _momentum, _ma1, _ma2, OnProcess)
		.Start();
	}
	
	private void OnProcess(
	ICandleMessage candle,
	IIndicatorValue macdValue,
	IIndicatorValue dmiValue,
	IIndicatorValue adxValue,
	IIndicatorValue rsiValue,
	IIndicatorValue cciValue,
	IIndicatorValue mfiValue,
	IIndicatorValue momentumValue,
	IIndicatorValue ma1Value,
	IIndicatorValue ma2Value)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var dmiTyped = (DirectionalIndexValue)dmiValue;
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		
		var rsi = rsiValue.ToDecimal();
		var cci = cciValue.ToDecimal();
		var mfi = mfiValue.ToDecimal();
		var momentum = momentumValue.ToDecimal();
		var ma1 = ma1Value.ToDecimal();
		var ma2 = ma2Value.ToDecimal();
		
		AddInfoLog(
		"RSI={0:F2}, MACD={1:F4}, Signal={2:F4}, Hist={3:F4}, +DI={4:F2}, -DI={5:F2}, ADX={6:F2}, CCI={7:F2}, MFI={8:F2}, Momentum={9:F2}, MA1={10:F2}, MA2={11:F2}",
		rsi,
		macdTyped.Macd,
		macdTyped.Signal,
		macdTyped.Histogram,
		dmiTyped.Plus,
		dmiTyped.Minus,
		adxTyped.MovingAverage,
		cci,
		mfi,
		momentum,
		ma1,
		ma2);
	}
}