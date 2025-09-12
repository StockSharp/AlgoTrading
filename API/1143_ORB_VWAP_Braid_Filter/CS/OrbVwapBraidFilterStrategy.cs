using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opening range breakout strategy with VWAP and Braid filter.
/// </summary>
public class OrbVwapBraidFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _ema1 = null!;
	private ExponentialMovingAverage _ema2 = null!;
	private ExponentialMovingAverage _ema3 = null!;
	private AverageTrueRange _atr = null!;
	private VolumeWeightedMovingAverage _vwap = null!;
	
	private decimal? _orbHigh;
	private decimal? _orbLow;
	private decimal _preMarketHigh;
	private decimal _preMarketLow;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasTraded;
	private DateTime _currentDay;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;
	
	/// <summary>
	/// Operation start hour.
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	
	/// <summary>
	/// Operation start minute.
	/// </summary>
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	
	/// <summary>
	/// Operation end hour.
	/// </summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	
	/// <summary>
	/// Operation end minute.
	/// </summary>
	public int EndMinute { get => _endMinute.Value; set => _endMinute.Value = value; }
	
	/// <summary>
	/// Risk reward ratio.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	
	/// <summary>
	/// Main candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="OrbVwapBraidFilterStrategy"/> class.
	/// </summary>
	public OrbVwapBraidFilterStrategy()
	{
		_startHour = Param(nameof(StartHour), 9)
		.SetDisplay("Start Hour", "Operation start hour", "General");
		_startMinute = Param(nameof(StartMinute), 35)
		.SetDisplay("Start Minute", "Operation start minute", "General");
		_endHour = Param(nameof(EndHour), 11)
		.SetDisplay("End Hour", "Operation end hour", "General");
		_endMinute = Param(nameof(EndMinute), 0)
		.SetDisplay("End Minute", "Operation end minute", "General");
		_riskReward = Param(nameof(RiskReward), 2m)
		.SetDisplay("Risk Reward", "Risk reward ratio", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Main timeframe", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var thirty = TimeSpan.FromMinutes(30).TimeFrame();
		var daily = TimeSpan.FromDays(1).TimeFrame();
		return [(Security, CandleType), (Security, thirty), (Security, daily)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_orbHigh = null;
		_orbLow = null;
		_preMarketHigh = 0m;
		_preMarketLow = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_hasTraded = false;
		_currentDay = default;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		_ema1 = new ExponentialMovingAverage { Length = 3 };
		_ema2 = new ExponentialMovingAverage { Length = 7 };
		_ema3 = new ExponentialMovingAverage { Length = 14 };
		_atr = new AverageTrueRange { Length = 14 };
		_vwap = new VolumeWeightedMovingAverage();
		
		var sub5 = SubscribeCandles(CandleType);
		sub5.Bind(_ema1, _ema3, _atr, _vwap, ProcessCandle).Start();
		
		var sub30 = SubscribeCandles(TimeSpan.FromMinutes(30).TimeFrame());
		sub30.Bind(ProcessPreMarket).Start();
		
		var subD = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		subD.Bind(ProcessDaily).Start();
	}
	
	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
	
	private void ProcessPreMarket(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (candle.OpenTime.Date != _currentDay)
		{
			_preMarketHigh = candle.HighPrice;
			_preMarketLow = candle.LowPrice;
			_currentDay = candle.OpenTime.Date;
			return;
		}
		
		var minutes = candle.OpenTime.Hour * 60 + candle.OpenTime.Minute;
		if (minutes < 9 * 60 + 30)
		{
			_preMarketHigh = Math.Max(_preMarketHigh, candle.HighPrice);
			_preMarketLow = _preMarketLow == 0m ? candle.LowPrice : Math.Min(_preMarketLow, candle.LowPrice);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal ema1, decimal ema3, decimal atrValue, decimal vwapValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var ema2Val = _ema2.Process(candle.OpenPrice);
		if (!ema2Val.IsFinal)
		return;
		
		if (!_ema1.IsFormed || !_ema2.IsFormed || !_ema3.IsFormed || !_atr.IsFormed || !_vwap.IsFormed)
		return;
		
		if (candle.OpenTime.Date != _currentDay)
		{
			_orbHigh = null;
			_orbLow = null;
			_hasTraded = false;
			_currentDay = candle.OpenTime.Date;
		}
		
		if (candle.OpenTime.Hour == 9 && candle.OpenTime.Minute == 30)
		{
			_orbHigh = candle.HighPrice;
			_orbLow = candle.LowPrice;
		}
		else if (candle.OpenTime.Hour == 9 && candle.OpenTime.Minute < 35 && _orbHigh.HasValue && _orbLow.HasValue)
		{
			_orbHigh = Math.Max(_orbHigh.Value, candle.HighPrice);
			_orbLow = Math.Min(_orbLow.Value, candle.LowPrice);
		}
		
		var ema2 = ema2Val.GetValue<decimal>();
		var braidMax = Math.Max(Math.Max(ema1, ema2), ema3);
		var braidMin = Math.Min(Math.Min(ema1, ema2), ema3);
		var braidDif = braidMax - braidMin;
		var filter = atrValue * 0.4m;
		var braidBull = ema1 > ema2 && braidDif > filter;
		var braidBear = ema2 > ema1 && braidDif > filter;
		
		var minutes = candle.OpenTime.Hour * 60 + candle.OpenTime.Minute;
		var start = StartHour * 60 + StartMinute;
		var end = EndHour * 60 + EndMinute;
		var inTime = minutes >= start && minutes <= end;
		
		if (Position > 0)
		{
			if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake)
			ClosePosition();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake)
			ClosePosition();
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (_hasTraded || !_orbHigh.HasValue || !_orbLow.HasValue)
		return;
		
		var longCond = inTime && candle.ClosePrice > _orbHigh.Value && candle.ClosePrice > vwapValue && braidBull;
		var shortCond = inTime && candle.ClosePrice < _orbLow.Value && candle.ClosePrice < vwapValue && braidBear;
		
		if (longCond)
		{
			_longStop = _orbLow.Value;
			var r = candle.ClosePrice - _longStop;
			var tpBase = candle.ClosePrice + r * RiskReward;
			_longTake = Math.Min(tpBase, Math.Min(_prevHigh, _preMarketHigh));
			BuyMarket();
			_hasTraded = true;
		}
		else if (shortCond)
		{
			_shortStop = _orbHigh.Value;
			var r = _shortStop - candle.ClosePrice;
			var tpBase = candle.ClosePrice - r * RiskReward;
			_shortTake = Math.Max(tpBase, Math.Max(_prevLow, _preMarketLow));
			SellMarket();
			_hasTraded = true;
		}
	}
}
