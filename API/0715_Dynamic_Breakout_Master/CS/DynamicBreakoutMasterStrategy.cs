
namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Dynamic Breakout Master strategy with multiple filters.
/// </summary>
public class DynamicBreakoutMasterStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<bool> _ma1IsEma;
	private readonly StrategyParam<bool> _ma2IsEma;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _rewardRatio;
	private readonly StrategyParam<decimal> _accountSize;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<DataType> _candleType;
	
	private IIndicator _ma1;
	private IIndicator _ma2;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private DonchianChannels _donchian;
	private SimpleMovingAverage _volumeSma;
	
	private decimal _prevClose;
	private decimal _prevMa1;
	private decimal _prevMa2;
	private decimal? _lastBreakoutLevel;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _trailStop;
	
	/// <summary>
	/// Donchian Channel period.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}
	
	/// <summary>
	/// First MA length.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}
	
	/// <summary>
	/// Second MA length.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}
	
	/// <summary>
	/// Use EMA for first MA.
	/// </summary>
	public bool Ma1IsEma
	{
		get => _ma1IsEma.Value;
		set => _ma1IsEma.Value = value;
	}
	
	/// <summary>
	/// Use EMA for second MA.
	/// </summary>
	public bool Ma2IsEma
	{
		get => _ma2IsEma.Value;
		set => _ma2IsEma.Value = value;
	}
	
	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}
	
	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}
	
	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}
	
	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier threshold.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Risk per trade in percent.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}
	
	/// <summary>
	/// Reward ratio.
	/// </summary>
	public decimal RewardRatio
	{
		get => _rewardRatio.Value;
		set => _rewardRatio.Value = value;
	}
	
	/// <summary>
	/// Account size for position sizing.
	/// </summary>
	public decimal AccountSize
	{
		get => _accountSize.Value;
		set => _accountSize.Value = value;
	}
	
	/// <summary>
	/// Start hour for trading.
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}
	
	/// <summary>
	/// End hour for trading.
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
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
	/// Initializes a new instance of the <see cref="DynamicBreakoutMasterStrategy"/> class.
	/// </summary>
	public DynamicBreakoutMasterStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 50)
		.SetDisplay("Donchian Period", "Channel lookback period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 200, 10);
		
		_ma1Length = Param(nameof(Ma1Length), 50)
		.SetDisplay("MA1 Length", "Length for first moving average", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(10, 100, 10);
		
		_ma2Length = Param(nameof(Ma2Length), 200)
		.SetDisplay("MA2 Length", "Length for second moving average", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(50, 300, 10);
		
		_ma1IsEma = Param(nameof(Ma1IsEma), true)
		.SetDisplay("MA1 EMA", "Use EMA for MA1", "Trend Filter");
		
		_ma2IsEma = Param(nameof(Ma2IsEma), true)
		.SetDisplay("MA2 EMA", "Use EMA for MA2", "Trend Filter");
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "Relative Strength Index period", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(7, 30, 1);
		
		_rsiOverbought = Param(nameof(RsiOverbought), 70)
		.SetDisplay("RSI Overbought", "RSI overbought level", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(60, 90, 5);
		
		_rsiOversold = Param(nameof(RsiOversold), 30)
		.SetDisplay("RSI Oversold", "RSI oversold level", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetDisplay("ATR Length", "ATR period", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(7, 30, 1);
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.2m)
		.SetDisplay("ATR Multiplier", "ATR threshold multiplier", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.1m);
		
		_riskPerTrade = Param(nameof(RiskPerTrade), 1m)
		.SetDisplay("Risk %", "Risk per trade percentage", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);
		
		_rewardRatio = Param(nameof(RewardRatio), 2m)
		.SetDisplay("Reward Ratio", "Take profit multiplier", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_accountSize = Param(nameof(AccountSize), 10000m)
		.SetDisplay("Account Size", "Account size in dollars", "Risk Management");
		
		_tradingStartHour = Param(nameof(TradingStartHour), 9)
		.SetDisplay("Start Hour", "Trading session start hour", "Time Filter")
		.SetCanOptimize(true)
		.SetOptimize(0, 12, 1);
		
		_tradingEndHour = Param(nameof(TradingEndHour), 17)
		.SetDisplay("End Hour", "Trading session end hour", "Time Filter")
		.SetCanOptimize(true)
		.SetOptimize(12, 23, 1);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		
		_ma1 = null;
		_ma2 = null;
		_rsi = null;
		_atr = null;
		_donchian = null;
		_volumeSma = null;
		
		_prevClose = default;
		_prevMa1 = default;
		_prevMa2 = default;
		_lastBreakoutLevel = null;
		_stopPrice = default;
		_takePrice = default;
		_trailStop = default;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_ma1 = Ma1IsEma ? new ExponentialMovingAverage { Length = Ma1Length } : new SimpleMovingAverage { Length = Ma1Length };
		_ma2 = Ma2IsEma ? new ExponentialMovingAverage { Length = Ma2Length } : new SimpleMovingAverage { Length = Ma2Length };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_donchian = new DonchianChannels { Length = DonchianPeriod };
		_volumeSma = new SimpleMovingAverage { Length = 20 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_donchian, _ma1, _ma2, _rsi, _atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			
			var rsiArea = CreateChartArea();
			if (rsiArea != null)
			{
				DrawIndicator(rsiArea, _rsi);
			}
			
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue donchianValue,
	IIndicatorValue ma1Value,
	IIndicatorValue ma2Value,
	IIndicatorValue rsiValue,
	IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var volAvg = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		
		if (!donchianValue.IsFinal || !ma1Value.IsFinal || !ma2Value.IsFinal || !rsiValue.IsFinal || !atrValue.IsFinal || !_volumeSma.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevMa1 = ma1Value.ToDecimal();
			_prevMa2 = ma2Value.ToDecimal();
			return;
		}
		
		var dc = (DonchianChannelsValue)donchianValue;
		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
		{
			_prevClose = candle.ClosePrice;
			_prevMa1 = ma1Value.ToDecimal();
			_prevMa2 = ma2Value.ToDecimal();
			return;
		}
		
		var ma1 = ma1Value.GetValue<decimal>();
		var ma2 = ma2Value.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		
		var isBullishTrend = ma1 > ma2;
		var isBearishTrend = ma1 < ma2;
		
		var rsiBullish = rsi > RsiOversold && rsi < RsiOverbought;
		var rsiBearish = rsi < RsiOverbought && rsi > RsiOversold;
		
		var inTradingHours = candle.OpenTime.Hour >= TradingStartHour && candle.OpenTime.Hour <= TradingEndHour;
		var volCondition = candle.TotalVolume > volAvg * 1.5m;
		var volatilityOk = atr > AtrMultiplier;
		
		var resistanceBroken = _prevClose <= upper && candle.ClosePrice > upper;
		var supportBroken = _prevClose >= lower && candle.ClosePrice < lower;
		
		if (resistanceBroken)
		_lastBreakoutLevel = upper;
		else if (supportBroken)
		_lastBreakoutLevel = lower;
		
		var pullbackLong = _lastBreakoutLevel is decimal bl && candle.ClosePrice < bl && _prevClose >= bl && isBullishTrend;
		var pullbackShort = _lastBreakoutLevel is decimal sl && candle.ClosePrice > sl && _prevClose <= sl && isBearishTrend;
		
		var longCondition = (resistanceBroken || pullbackLong) && isBullishTrend && rsiBullish && volatilityOk && inTradingHours && volCondition;
		var shortCondition = (supportBroken || pullbackShort) && isBearishTrend && rsiBearish && volatilityOk && inTradingHours && volCondition;
		
		var stopLossDistance = atr * 1.5m;
		var takeProfitDistance = stopLossDistance * RewardRatio;
		var riskAmount = AccountSize * (RiskPerTrade / 100m);
		var positionSize = riskAmount / stopLossDistance;
		
		var maExitLong = _prevMa2 <= _prevMa1 && ma2 > ma1;
		var maExitShort = _prevMa1 <= _prevMa2 && ma1 > ma2;
		var rsiExitLong = rsi > 80;
		var rsiExitShort = rsi < 20;
		
		if (longCondition && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - stopLossDistance;
			_takePrice = _entryPrice + takeProfitDistance;
			_trailStop = _entryPrice - stopLossDistance * 0.5m;
			BuyMarket(positionSize);
		}
		else if (shortCondition && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + stopLossDistance;
			_takePrice = _entryPrice - takeProfitDistance;
			_trailStop = _entryPrice + stopLossDistance * 0.5m;
			SellMarket(positionSize);
		}
		
		if (Position > 0)
		{
			_trailStop = Math.Max(_trailStop, candle.ClosePrice - stopLossDistance * 0.5m);
			var currentStop = Math.Max(_stopPrice, _trailStop);
			if (candle.LowPrice <= currentStop || candle.HighPrice >= _takePrice || rsiExitLong || maExitLong)
			ClosePosition();
		}
		else if (Position < 0)
		{
			_trailStop = Math.Min(_trailStop, candle.ClosePrice + stopLossDistance * 0.5m);
			var currentStop = Math.Min(_stopPrice, _trailStop);
			if (candle.HighPrice >= currentStop || candle.LowPrice <= _takePrice || rsiExitShort || maExitShort)
			ClosePosition();
		}
		
		_prevClose = candle.ClosePrice;
		_prevMa1 = ma1;
		_prevMa2 = ma2;
	}
	
	private decimal _entryPrice;
}
