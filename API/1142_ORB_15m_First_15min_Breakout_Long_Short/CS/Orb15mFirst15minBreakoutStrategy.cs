using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// First 15-minute breakout at session open.
/// </summary>
public class Orb15mFirst15minBreakoutStrategy : Strategy
{
	private readonly StrategyParam<bool> _useLongs;
	private readonly StrategyParam<bool> _useShorts;
	private readonly StrategyParam<decimal> _riskPct;
	private readonly StrategyParam<bool> _tpTenR;
	private readonly StrategyParam<decimal> _rMultiple;
	private readonly StrategyParam<int> _sessionOpenHour;
	private readonly StrategyParam<int> _sessionOpenMinute;
	private readonly StrategyParam<int> _sessionEndHour;
	private readonly StrategyParam<int> _sessionEndMinute;
	private readonly StrategyParam<DataType> _candleType;
	
	private int _lastTradeYmd;
	private readonly TimeZoneInfo _tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
	
	public Orb15mFirst15minBreakoutStrategy()
	{
		_useLongs = Param(nameof(UseLongs), true)
		.SetDisplay("Use Longs", "Allow long trades", "General");
		_useShorts = Param(nameof(UseShorts), true)
		.SetDisplay("Use Shorts", "Allow short trades", "General");
		_riskPct = Param(nameof(RiskPct), 1m)
		.SetDisplay("Risk %", "Risk per trade percent", "Risk");
		_tpTenR = Param(nameof(TpTenR), true)
		.SetDisplay("Take Profit 10R", "Use 10R take profit or exit at session end", "Risk");
		_rMultiple = Param(nameof(RMultiple), 10m)
		.SetDisplay("R Multiple", "Take profit multiple of risk", "Risk");
		_sessionOpenHour = Param(nameof(SessionOpenHour), 15)
		.SetDisplay("Session Open Hour", "Session open hour", "Session");
		_sessionOpenMinute = Param(nameof(SessionOpenMinute), 30)
		.SetDisplay("Session Open Minute", "Session open minute", "Session");
		_sessionEndHour = Param(nameof(SessionEndHour), 22)
		.SetDisplay("Session End Hour", "Session end hour", "Session");
		_sessionEndMinute = Param(nameof(SessionEndMinute), 0)
		.SetDisplay("Session End Minute", "Session end minute", "Session");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candle timeframe", "General");
	}
	
	public bool UseLongs { get => _useLongs.Value; set => _useLongs.Value = value; }
	public bool UseShorts { get => _useShorts.Value; set => _useShorts.Value = value; }
	public decimal RiskPct { get => _riskPct.Value; set => _riskPct.Value = value; }
	public bool TpTenR { get => _tpTenR.Value; set => _tpTenR.Value = value; }
	public decimal RMultiple { get => _rMultiple.Value; set => _rMultiple.Value = value; }
	public int SessionOpenHour { get => _sessionOpenHour.Value; set => _sessionOpenHour.Value = value; }
	public int SessionOpenMinute { get => _sessionOpenMinute.Value; set => _sessionOpenMinute.Value = value; }
	public int SessionEndHour { get => _sessionEndHour.Value; set => _sessionEndHour.Value = value; }
	public int SessionEndMinute { get => _sessionEndMinute.Value; set => _sessionEndMinute.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(OnProcessCandle).Start();
	}
	
	private void OnProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var localTime = TimeZoneInfo.ConvertTime(candle.OpenTime.UtcDateTime, _tz);
		var todayYmd = localTime.Year * 10000 + localTime.Month * 100 + localTime.Day;
		var tradedToday = _lastTradeYmd == todayYmd;
		
		if (localTime.Hour == SessionOpenHour && localTime.Minute == SessionOpenMinute && !tradedToday && Position == 0)
		{
			var entry = candle.ClosePrice;
			
			if (UseLongs && entry > candle.OpenPrice)
			{
				var stop = candle.LowPrice;
				var risk = entry - stop;
				var volume = CalculateVolume(risk);
				
				BuyMarket(volume);
				SellStop(volume, stop);
				
				if (TpTenR)
				{
					var tp = entry + (RMultiple * risk);
					SellLimit(volume, tp);
				}
				
				_lastTradeYmd = todayYmd;
			}
			else if (UseShorts && entry < candle.OpenPrice)
			{
				var stop = candle.HighPrice;
				var risk = stop - entry;
				var volume = CalculateVolume(risk);
				
				SellMarket(volume);
				BuyStop(volume, stop);
				
				if (TpTenR)
				{
					var tp = entry - (RMultiple * risk);
					BuyLimit(volume, tp);
				}
				
				_lastTradeYmd = todayYmd;
			}
		}
		
		var endTime = TimeZoneInfo.ConvertTime(candle.CloseTime.UtcDateTime, _tz);
		if (endTime.Hour == SessionEndHour && endTime.Minute == SessionEndMinute && Position != 0)
		CloseAll();
	}
	
	private decimal CalculateVolume(decimal risk)
	{
		if (risk <= 0)
		return Volume;
		
		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var capitalToRisk = portfolioValue * (RiskPct / 100m);
		var qty = Math.Floor(capitalToRisk / risk);
		return qty < 1m ? 1m : qty;
	}
}
