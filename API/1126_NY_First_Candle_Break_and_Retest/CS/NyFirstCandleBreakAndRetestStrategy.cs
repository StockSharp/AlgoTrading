using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// New York session first candle breakout with retest confirmation.
/// </summary>
public class NyFirstCandleBreakAndRetestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _nyStartHour;
	private readonly StrategyParam<int> _nyStartMinute;
	private readonly StrategyParam<int> _sessionLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _rewardRiskRatio;
	private readonly StrategyParam<decimal> _minBreakSize;
	private readonly StrategyParam<decimal> _retestThreshold;
	private readonly StrategyParam<int> _minCandlesAfterBreak;
	private readonly StrategyParam<int> _maxCandlesAfterBreak;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingActivation;
	private readonly StrategyParam<decimal> _trailingAmount;
	
	private decimal? _firstHigh;
	private decimal? _firstLow;
	private bool _sessionActive;
	private bool _breakAbove;
	private bool _breakBelow;
	private int _barsAfterBreak;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private bool _trailActive;
	
	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// New York session start hour.
	/// </summary>
	public int NyStartHour { get => _nyStartHour.Value; set => _nyStartHour.Value = value; }
	
	/// <summary>
	/// New York session start minute.
	/// </summary>
	public int NyStartMinute { get => _nyStartMinute.Value; set => _nyStartMinute.Value = value; }
	
	/// <summary>
	/// Session length in hours.
	/// </summary>
	public int SessionLength { get => _sessionLength.Value; set => _sessionLength.Value = value; }
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	
	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	
	/// <summary>
	/// Reward to risk ratio.
	/// </summary>
	public decimal RewardRiskRatio { get => _rewardRiskRatio.Value; set => _rewardRiskRatio.Value = value; }
	
	/// <summary>
	/// Minimum break size in ATR fractions.
	/// </summary>
	public decimal MinBreakSize { get => _minBreakSize.Value; set => _minBreakSize.Value = value; }
	
	/// <summary>
	/// Retest threshold in ATR fractions.
	/// </summary>
	public decimal RetestThreshold { get => _retestThreshold.Value; set => _retestThreshold.Value = value; }
	
	/// <summary>
	/// Minimum candles between break and retest.
	/// </summary>
	public int MinCandlesAfterBreak { get => _minCandlesAfterBreak.Value; set => _minCandlesAfterBreak.Value = value; }
	
	/// <summary>
	/// Maximum candles between break and retest.
	/// </summary>
	public int MaxCandlesAfterBreak { get => _maxCandlesAfterBreak.Value; set => _maxCandlesAfterBreak.Value = value; }
	
	/// <summary>
	/// Use EMA filter for direction confirmation.
	/// </summary>
	public bool UseEmaFilter { get => _useEmaFilter.Value; set => _useEmaFilter.Value = value; }
	
	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	
	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }
	
	/// <summary>
	/// Activation level for trailing stop as fraction of target.
	/// </summary>
	public decimal TrailingActivation { get => _trailingActivation.Value; set => _trailingActivation.Value = value; }
	
	/// <summary>
	/// Trailing stop amount in ATR fractions.
	/// </summary>
	public decimal TrailingAmount { get => _trailingAmount.Value; set => _trailingAmount.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="NyFirstCandleBreakAndRetestStrategy"/>.
	/// </summary>
	public NyFirstCandleBreakAndRetestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Working candle timeframe", "General");
		
		_nyStartHour = Param(nameof(NyStartHour), 9)
		.SetDisplay("NY Start Hour", "New York session start hour", "Session");
		
		_nyStartMinute = Param(nameof(NyStartMinute), 30)
		.SetDisplay("NY Start Minute", "New York session start minute", "Session");
		
		_sessionLength = Param(nameof(SessionLength), 4)
		.SetGreaterThanZero()
		.SetDisplay("Session Length", "Session length in hours", "Session");
		
		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR calculation period", "Risk");
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.2m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Stop loss ATR multiplier", "Risk");
		
		_rewardRiskRatio = Param(nameof(RewardRiskRatio), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Reward/Risk", "Target to stop ratio", "Risk");
		
		_minBreakSize = Param(nameof(MinBreakSize), 0.15m)
		.SetGreaterThanZero()
		.SetDisplay("Min Break", "Minimum break size in ATR", "Break");
		
		_retestThreshold = Param(nameof(RetestThreshold), 0.25m)
		.SetGreaterThanZero()
		.SetDisplay("Retest Threshold", "Retest threshold in ATR", "Break");
		
		_minCandlesAfterBreak = Param(nameof(MinCandlesAfterBreak), 2)
		.SetGreaterThanZero()
		.SetDisplay("Min Candles", "Minimum candles between break and retest", "Break");
		
		_maxCandlesAfterBreak = Param(nameof(MaxCandlesAfterBreak), 25)
		.SetGreaterThanZero()
		.SetDisplay("Max Candles", "Maximum candles between break and retest", "Break");
		
		_useEmaFilter = Param(nameof(UseEmaFilter), true)
		.SetDisplay("Use EMA", "Enable EMA trend filter", "Indicators");
		
		_emaLength = Param(nameof(EmaLength), 13)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "EMA period", "Indicators");
		
		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Trailing Stop", "Enable trailing stop", "Trade");
		
		_trailingActivation = Param(nameof(TrailingActivation), 0.6m)
		.SetGreaterThanZero()
		.SetDisplay("Trail Activation", "Activation level of trailing stop", "Trade");
		
		_trailingAmount = Param(nameof(TrailingAmount), 0.4m)
		.SetGreaterThanZero()
		.SetDisplay("Trail Amount", "Trailing stop ATR multiplier", "Trade");
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
		_firstHigh = null;
		_firstLow = null;
		_sessionActive = false;
		_breakAbove = false;
		_breakBelow = false;
		_barsAfterBreak = 0;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_trailActive = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(atr, ema, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, ema);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var start = TimeSpan.FromHours(NyStartHour) + TimeSpan.FromMinutes(NyStartMinute);
		var end = start + TimeSpan.FromHours(SessionLength);
		var time = candle.OpenTime.TimeOfDay;
		var inSession = time >= start && time <= end;
		
		if (!inSession)
		{
			_sessionActive = false;
			_firstHigh = null;
			_firstLow = null;
			_breakAbove = false;
			_breakBelow = false;
			_barsAfterBreak = 0;
			_trailActive = false;
			if (Position != 0)
			ClosePosition();
			return;
		}
		
		if (!_sessionActive)
		{
			_sessionActive = true;
			_firstHigh = candle.HighPrice;
			_firstLow = candle.LowPrice;
			_breakAbove = false;
			_breakBelow = false;
			_barsAfterBreak = 0;
			return;
		}
		
		if (_breakAbove || _breakBelow)
		_barsAfterBreak++;
		
		if (!_breakAbove && candle.HighPrice >= _firstHigh + atr * MinBreakSize)
		{
			_breakAbove = true;
			_barsAfterBreak = 0;
		}
		
		if (!_breakBelow && candle.LowPrice <= _firstLow - atr * MinBreakSize)
		{
			_breakBelow = true;
			_barsAfterBreak = 0;
		}
		
		if (_breakAbove && Position <= 0)
		{
			var retest = candle.LowPrice <= _firstHigh + atr * RetestThreshold;
			if (retest && _barsAfterBreak >= MinCandlesAfterBreak && _barsAfterBreak <= MaxCandlesAfterBreak)
			{
				if (!UseEmaFilter || candle.ClosePrice > ema)
				OpenLong(candle.ClosePrice, atr);
			}
			else if (_barsAfterBreak > MaxCandlesAfterBreak)
			{
				_breakAbove = false;
			}
		}
		else if (_breakBelow && Position >= 0)
		{
			var retest = candle.HighPrice >= _firstLow - atr * RetestThreshold;
			if (retest && _barsAfterBreak >= MinCandlesAfterBreak && _barsAfterBreak <= MaxCandlesAfterBreak)
			{
				if (!UseEmaFilter || candle.ClosePrice < ema)
				OpenShort(candle.ClosePrice, atr);
			}
			else if (_barsAfterBreak > MaxCandlesAfterBreak)
			{
				_breakBelow = false;
			}
		}
		
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
			{
				ClosePosition();
				_trailActive = false;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
			{
				ClosePosition();
				_trailActive = false;
			}
		}
		
		if (UseTrailingStop && Position != 0)
		{
			var move = Position > 0 ? candle.ClosePrice - _entryPrice : _entryPrice - candle.ClosePrice;
			var targetDistance = Math.Abs(_targetPrice - _entryPrice);
			if (!_trailActive && move >= targetDistance * TrailingActivation)
			_trailActive = true;
			
			if (_trailActive)
			{
				var newStop = Position > 0
				? candle.ClosePrice - atr * TrailingAmount
				: candle.ClosePrice + atr * TrailingAmount;
				
				if (Position > 0)
				{
					if (newStop > _stopPrice)
					_stopPrice = newStop;
					if (candle.LowPrice <= _stopPrice)
					{
						ClosePosition();
						_trailActive = false;
					}
				}
				else
				{
					if (newStop < _stopPrice)
					_stopPrice = newStop;
					if (candle.HighPrice >= _stopPrice)
					{
						ClosePosition();
						_trailActive = false;
					}
				}
			}
		}
	}
	
	private void OpenLong(decimal price, decimal atr)
	{
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		_entryPrice = price;
		_stopPrice = price - atr * AtrMultiplier;
		_targetPrice = price + (price - _stopPrice) * RewardRiskRatio;
		_trailActive = false;
	}
	
	private void OpenShort(decimal price, decimal atr)
	{
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		_entryPrice = price;
		_stopPrice = price + atr * AtrMultiplier;
		_targetPrice = price - (_stopPrice - price) * RewardRiskRatio;
		_trailActive = false;
	}
}
