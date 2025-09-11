using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smart scalping strategy using EMA9, VWAP and RSI with ATR based risk management.
/// </summary>
public class Nas100AndGoldSmartScalpingStrategyProEnhancedV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _accountCapital;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<decimal> _volumeSpikeMultiplier;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _cooldownMins;
	private readonly StrategyParam<bool> _exitOnOpposite;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly SimpleMovingAverage _avgVol = new() { Length = 20 };
	private decimal? _ema200Value;
	private DateTimeOffset? _lastTradeTime;
	private decimal? _longStop, _longTake, _shortStop, _shortTake;
	private decimal _slPoints, _tpPoints;
	
	public decimal AccountCapital { get => _accountCapital.Value; set => _accountCapital.Value = value; }
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }
	public decimal AtrMultiplierSl { get => _atrMultiplierSl.Value; set => _atrMultiplierSl.Value = value; }
	public decimal AtrMultiplierTp { get => _atrMultiplierTp.Value; set => _atrMultiplierTp.Value = value; }
	public decimal VolumeSpikeMultiplier { get => _volumeSpikeMultiplier.Value; set => _volumeSpikeMultiplier.Value = value; }
	public bool UseTrendFilter { get => _useTrendFilter.Value; set => _useTrendFilter.Value = value; }
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }
	public int CooldownMins { get => _cooldownMins.Value; set => _cooldownMins.Value = value; }
	public bool ExitOnOpposite { get => _exitOnOpposite.Value; set => _exitOnOpposite.Value = value; }
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public Nas100AndGoldSmartScalpingStrategyProEnhancedV2Strategy()
	{
		_accountCapital = Param(nameof(AccountCapital), 5000m)
		.SetDisplay("Account Capital", "Trading capital in USD", "Money");
		
		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetDisplay("Risk %", "Risk per trade percent", "Money");
		
		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1m)
		.SetDisplay("ATR SL Mult", "ATR multiplier for stop-loss", "Risk");
		
		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 2m)
		.SetDisplay("ATR TP Mult", "ATR multiplier for take-profit", "Risk");
		
		_volumeSpikeMultiplier = Param(nameof(VolumeSpikeMultiplier), 1.5m)
		.SetDisplay("Volume Spike x", "Multiplier for average volume", "Trading");
		
		_useTrendFilter = Param(nameof(UseTrendFilter), true)
		.SetDisplay("Use Trend Filter", "Filter by 15m EMA200", "Trading");
		
		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk");
		
		_cooldownMins = Param(nameof(CooldownMins), 30)
		.SetDisplay("Cooldown (min)", "Delay between trades", "Trading");
		
		_exitOnOpposite = Param(nameof(ExitOnOpposite), true)
		.SetDisplay("Exit On Opposite", "Close position on opposite signal", "Trading");
		
		_startHour = Param(nameof(StartHour), 13)
		.SetDisplay("Start Hour", "UTC session start hour", "Session");
		
		_endHour = Param(nameof(EndHour), 20)
		.SetDisplay("End Hour", "UTC session end hour", "Session");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromMinutes(15).TimeFrame())];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var ema9 = new ExponentialMovingAverage { Length = 9 };
		var vwap = new VolumeWeightedMovingAverage();
		var atr = new AverageTrueRange { Length = 14 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var ema200 = new ExponentialMovingAverage { Length = 200 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema9, vwap, atr, rsi, ProcessMain).Start();
		
		var trendSub = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		trendSub.Bind(ema200, ProcessTrend).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema9);
			DrawIndicator(area, vwap);
			DrawIndicator(area, ema200);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessTrend(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_ema200Value = emaValue;
	}
	
	private void ProcessMain(ICandleMessage candle, decimal ema9, decimal vwap, decimal atr, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var avgVolVal = _avgVol.Process(new DecimalIndicatorValue(_avgVol, candle.TotalVolume, candle.OpenTime));
		if (!avgVolVal.IsFinal || avgVolVal is not DecimalIndicatorValue avgVol)
		return;
		
		var volumeSpike = candle.TotalVolume > avgVol.Value * VolumeSpikeMultiplier;
		
		var hour = candle.OpenTime.UtcDateTime.Hour;
		var inSession = hour >= StartHour && hour <= EndHour;
		
		var bodyStrength = Math.Abs(candle.ClosePrice - candle.OpenPrice) > atr * 0.3m;
		
		var bullishSetup = candle.ClosePrice > candle.OpenPrice &&
		candle.ClosePrice > ema9 &&
		candle.ClosePrice > vwap &&
		rsi > 50m &&
		bodyStrength &&
		volumeSpike;
		
		var bearishSetup = candle.ClosePrice < candle.OpenPrice &&
		candle.ClosePrice < ema9 &&
		candle.ClosePrice < vwap &&
		rsi < 50m &&
		bodyStrength &&
		volumeSpike;
		
		var longCondition = bullishSetup && inSession;
		var shortCondition = bearishSetup && inSession;
		
		var longOk = UseTrendFilter ? longCondition && _ema200Value is decimal ema200 && candle.ClosePrice > ema200 : longCondition;
		var shortOk = UseTrendFilter ? shortCondition && _ema200Value is decimal ema200 && candle.ClosePrice < ema200 : shortCondition;
		
		var canTrade = !_lastTradeTime.HasValue || candle.OpenTime - _lastTradeTime > TimeSpan.FromMinutes(CooldownMins);
		
		var riskAmount = AccountCapital * (RiskPercent / 100m);
		_slPoints = atr * AtrMultiplierSl;
		_tpPoints = atr * AtrMultiplierTp;
		if (_slPoints <= 0m)
		return;
		var volume = riskAmount / _slPoints;
		
		if (longOk && canTrade)
		{
			BuyMarket(volume);
			_longStop = candle.ClosePrice - _slPoints;
			_longTake = candle.ClosePrice + _tpPoints;
			_shortStop = null;
			_shortTake = null;
			_lastTradeTime = candle.OpenTime;
		}
		else if (shortOk && canTrade)
		{
			SellMarket(volume);
			_shortStop = candle.ClosePrice + _slPoints;
			_shortTake = candle.ClosePrice - _tpPoints;
			_longStop = null;
			_longTake = null;
			_lastTradeTime = candle.OpenTime;
		}
		
		if (ExitOnOpposite)
		{
			if (shortOk && Position > 0)
			SellMarket(Position);
			else if (longOk && Position < 0)
			BuyMarket(-Position);
		}
		
		if (Position > 0 && _longStop.HasValue && _longTake.HasValue)
		{
			if (UseTrailingStop && candle.ClosePrice - _longStop.Value > _slPoints / 2m)
			_longStop = Math.Max(_longStop.Value, candle.ClosePrice - _tpPoints / 2m);
			
			if (candle.LowPrice <= _longStop.Value || candle.HighPrice >= _longTake.Value)
			SellMarket(Position);
		}
		else if (Position < 0 && _shortStop.HasValue && _shortTake.HasValue)
		{
			if (UseTrailingStop && _shortStop.Value - candle.ClosePrice > _slPoints / 2m)
			_shortStop = Math.Min(_shortStop.Value, candle.ClosePrice + _tpPoints / 2m);
			
			if (candle.HighPrice >= _shortStop.Value || candle.LowPrice <= _shortTake.Value)
			BuyMarket(-Position);
		}
	}
}
