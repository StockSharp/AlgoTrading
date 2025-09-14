using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with volume spike confirmation.
/// </summary>
public class SyndicateTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useSessionFilter;
	private readonly StrategyParam<int> _sessionStartHour;
	private readonly StrategyParam<int> _sessionStartMinute;
	private readonly StrategyParam<int> _sessionEndHour;
	private readonly StrategyParam<int> _sessionEndMinute;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private SimpleMovingAverage _volumeMa;
	private decimal _prevFast;
	private decimal _prevSlow;
	
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public bool UseSessionFilter { get => _useSessionFilter.Value; set => _useSessionFilter.Value = value; }
	public int SessionStartHour { get => _sessionStartHour.Value; set => _sessionStartHour.Value = value; }
	public int SessionStartMinute { get => _sessionStartMinute.Value; set => _sessionStartMinute.Value = value; }
	public int SessionEndHour { get => _sessionEndHour.Value; set => _sessionEndHour.Value = value; }
	public int SessionEndMinute { get => _sessionEndMinute.Value; set => _sessionEndMinute.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public SyndicateTraderStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "General");
		
		_slowEmaLength = Param(nameof(SlowEmaLength), 30)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "General");
		
		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Volume MA", "Volume MA length", "General");
		
		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Mult", "Volume spike multiplier", "General");
		
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
		.SetDisplay("Take Profit", "Take profit in price points", "Risk");
		
		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
		.SetDisplay("Stop Loss", "Stop loss in price points", "Risk");
		
		_useSessionFilter = Param(nameof(UseSessionFilter), false)
		.SetDisplay("Use Session", "Enable session filter", "Session");
		
		_sessionStartHour = Param(nameof(SessionStartHour), 0)
		.SetDisplay("Start Hour", "Session start hour", "Session");
		
		_sessionStartMinute = Param(nameof(SessionStartMinute), 0)
		.SetDisplay("Start Minute", "Session start minute", "Session");
		
		_sessionEndHour = Param(nameof(SessionEndHour), 23)
		.SetDisplay("End Hour", "Session end hour", "Session");
		
		_sessionEndMinute = Param(nameof(SessionEndMinute), 59)
		.SetDisplay("End Minute", "Session end minute", "Session");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_volumeMa = new SimpleMovingAverage { Length = VolumeMaLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		StartProtection(
		takeProfit: new Unit(TakeProfitPoints, UnitTypes.Price),
		stopLoss: new Unit(StopLossPoints, UnitTypes.Price));
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (UseSessionFilter)
		{
			var time = candle.OpenTime.TimeOfDay;
			var start = new TimeSpan(SessionStartHour, SessionStartMinute, 0);
			var end = new TimeSpan(SessionEndHour, SessionEndMinute, 0);
			if (time < start || time > end)
			{
				ClosePosition();
				return;
			}
		}
		
		var fast = _fastEma.Process(candle).ToDecimal();
		var slow = _slowEma.Process(candle).ToDecimal();
		var volumeAvg = _volumeMa.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		
		var crossUp = fast > slow && _prevFast <= _prevSlow;
		var crossDown = fast < slow && _prevFast >= _prevSlow;
		var volumeSpike = candle.TotalVolume > volumeAvg * VolumeMultiplier;
		
		_prevFast = fast;
		_prevSlow = slow;
		
		if (!volumeSpike)
		return;
		
		if (crossUp && Position <= 0)
		BuyMarket();
		else if (crossDown && Position >= 0)
		SellMarket();
	}
	
	private void ClosePosition()
	{
		if (Position > 0)
		SellMarket();
		else if (Position < 0)
		BuyMarket();
	}
}
