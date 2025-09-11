using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enhanced Market Structure strategy with ATR, RSI, EMA, volume and MACD filters.
/// Enters on breakouts or sweep reversals of recent swings.
/// </summary>
public class EnhancedMarketStructureStrategy : Strategy
{
	private readonly StrategyParam<int> _structurePeriod;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<decimal> _takeProfitAtr;
	private readonly StrategyParam<bool> _enableLongs;
	private readonly StrategyParam<bool> _enableShorts;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<bool> _useVolumeFilter;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;
	
	private Highest _highest;
	private Lowest _lowest;
	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private SimpleMovingAverage _volumeSma;
	
	private decimal _prevEma;
	private decimal _prevMacd;
	
	/// <summary>
	/// Period for swing high/low detection.
	/// </summary>
	public int StructurePeriod { get => _structurePeriod.Value; set => _structurePeriod.Value = value; }
	
	/// <summary>
	/// ATR calculation length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	
	/// <summary>
	/// Stop loss in ATR multiples.
	/// </summary>
	public decimal StopLossAtr { get => _stopLossAtr.Value; set => _stopLossAtr.Value = value; }
	
	/// <summary>
	/// Take profit in ATR multiples.
	/// </summary>
	public decimal TakeProfitAtr { get => _takeProfitAtr.Value; set => _takeProfitAtr.Value = value; }
	
	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLongs { get => _enableLongs.Value; set => _enableLongs.Value = value; }
	
	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShorts { get => _enableShorts.Value; set => _enableShorts.Value = value; }
	
	/// <summary>
	/// Use RSI filter.
	/// </summary>
	public bool UseRsiFilter { get => _useRsiFilter.Value; set => _useRsiFilter.Value = value; }
	
	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	
	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	
	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	
	/// <summary>
	/// Use volume filter.
	/// </summary>
	public bool UseVolumeFilter { get => _useVolumeFilter.Value; set => _useVolumeFilter.Value = value; }
	
	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolumeLength { get => _volumeLength.Value; set => _volumeLength.Value = value; }
	
	/// <summary>
	/// Volume threshold multiplier.
	/// </summary>
	public decimal VolumeThreshold { get => _volumeThreshold.Value; set => _volumeThreshold.Value = value; }
	
	/// <summary>
	/// Use MACD filter.
	/// </summary>
	public bool UseMacdFilter { get => _useMacdFilter.Value; set => _useMacdFilter.Value = value; }
	
	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	
	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	
	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	
	/// <summary>
	/// Use EMA filter.
	/// </summary>
	public bool UseEmaFilter { get => _useEmaFilter.Value; set => _useEmaFilter.Value = value; }
	
	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	
	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize <see cref="EnhancedMarketStructureStrategy"/>.
	/// </summary>
	public EnhancedMarketStructureStrategy()
	{
		_structurePeriod = Param(nameof(StructurePeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("Structure Period", "Bars to detect swings", "Market Structure");
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR calculation length", "Risk Management");
		
		_stopLossAtr = Param(nameof(StopLossAtr), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss ATR", "Stop loss ATR multiplier", "Risk Management");
		
		_takeProfitAtr = Param(nameof(TakeProfitAtr), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit ATR", "Take profit ATR multiplier", "Risk Management");
		
		_enableLongs = Param(nameof(EnableLongs), true)
		.SetDisplay("Enable Longs", "Allow long positions", "General");
		
		_enableShorts = Param(nameof(EnableShorts), true)
		.SetDisplay("Enable Shorts", "Allow short positions", "General");
		
		_useRsiFilter = Param(nameof(UseRsiFilter), true)
		.SetDisplay("Use RSI Filter", "Enable RSI filter", "Filters");
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI calculation length", "Filters");
		
		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
		.SetRange(60m, 90m)
		.SetDisplay("RSI Overbought", "Overbought level", "Filters");
		
		_rsiOversold = Param(nameof(RsiOversold), 30m)
		.SetRange(10m, 40m)
		.SetDisplay("RSI Oversold", "Oversold level", "Filters");
		
		_useVolumeFilter = Param(nameof(UseVolumeFilter), true)
		.SetDisplay("Use Volume Filter", "Enable volume filter", "Filters");
		
		_volumeLength = Param(nameof(VolumeLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Volume Length", "Volume SMA length", "Filters");
		
		_volumeThreshold = Param(nameof(VolumeThreshold), 1.2m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Threshold", "Volume multiplier threshold", "Filters");
		
		_useMacdFilter = Param(nameof(UseMacdFilter), false)
		.SetDisplay("Use MACD Filter", "Enable MACD filter", "Filters");
		
		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "MACD fast length", "Filters");
		
		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "MACD slow length", "Filters");
		
		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "MACD signal length", "Filters");
		
		_useEmaFilter = Param(nameof(UseEmaFilter), true)
		.SetDisplay("Use EMA Filter", "Enable EMA trend filter", "Filters");
		
		_emaLength = Param(nameof(EmaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "EMA calculation length", "Filters");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		
		_prevEma = 0m;
		_prevMacd = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_highest = new Highest { Length = StructurePeriod };
		_lowest = new Lowest { Length = StructurePeriod };
		_atr = new AverageTrueRange { Length = AtrLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Fast = MacdFast,
			Slow = MacdSlow,
			Signal = MacdSignal
		};
		_volumeSma = new SimpleMovingAverage { Length = VolumeLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(new IIndicator[] { _highest, _lowest, _atr, _rsi, _ema, _macd }, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var volumeAvg = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		
		if (!IsFormedAndOnlineAndAllowTrading() || !_highest.IsFormed || !_lowest.IsFormed || !_atr.IsFormed || !_rsi.IsFormed || !_ema.IsFormed || !_macd.IsFormed || (UseVolumeFilter && !_volumeSma.IsFormed))
		return;
		
		var swingHigh = values[0].ToDecimal();
		var swingLow = values[1].ToDecimal();
		var atrValue = values[2].ToDecimal();
		var rsiValue = values[3].ToDecimal();
		var emaValue = values[4].ToDecimal();
		
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)values[5];
		var macdLine = macdTyped.Macd;
		var macdSignal = macdTyped.Signal;
		
		var rsiLong = !UseRsiFilter || (rsiValue < RsiOverbought && rsiValue > 40m);
		var rsiShort = !UseRsiFilter || (rsiValue > RsiOversold && rsiValue < 60m);
		var highVolume = !UseVolumeFilter || candle.TotalVolume > volumeAvg * VolumeThreshold;
		var macdBullish = !UseMacdFilter || (macdLine > macdSignal && macdLine > _prevMacd);
		var macdBearish = !UseMacdFilter || (macdLine < macdSignal && macdLine < _prevMacd);
		var emaBullish = !UseEmaFilter || (candle.ClosePrice > emaValue && emaValue > _prevEma);
		var emaBearish = !UseEmaFilter || (candle.ClosePrice < emaValue && emaValue < _prevEma);
		
		var longFilters = rsiLong && highVolume && macdBullish && emaBullish;
		var shortFilters = rsiShort && highVolume && macdBearish && emaBearish;
		
		var longBreakout = candle.ClosePrice > swingHigh;
		var shortBreakout = candle.ClosePrice < swingLow;
		var longSweep = candle.HighPrice > swingHigh && candle.ClosePrice < swingHigh;
		var shortSweep = candle.LowPrice < swingLow && candle.ClosePrice > swingLow;
		
		var volume = Volume + Math.Abs(Position);
		
		if (EnableLongs && Position <= 0 && longBreakout && longFilters)
		{
			BuyMarket(volume);
		}
		else if (EnableShorts && Position >= 0 && shortBreakout && shortFilters)
		{
			SellMarket(volume);
		}
		else if (EnableLongs && Position <= 0 && longSweep && longFilters)
		{
			BuyMarket(volume);
		}
		else if (EnableShorts && Position >= 0 && shortSweep && shortFilters)
		{
			SellMarket(volume);
		}
		
		_prevEma = emaValue;
		_prevMacd = macdLine;
	}
}
