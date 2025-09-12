using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI cyclic smoothed strategy using dynamic percentile bands.
/// </summary>
public class RsiCyclicSmoothedStrategy : Strategy
{
	private readonly StrategyParam<int> _dominantCycle;
	private readonly StrategyParam<int> _vibration;
	private readonly StrategyParam<decimal> _leveling;
	private readonly StrategyParam<DataType> _candleType;
	
	private RelativeStrengthIndex _rsi;
	
	private decimal _prevCrsi;
	private decimal[] _rsiHistory;
	private int _rsiIndex;
	private int _rsiHistorySize;
	
	private decimal[] _crsiHistory;
	private int _crsiIndex;
	private int _crsiCount;
	
	private decimal _lowBand;
	private decimal _highBand;
	
	public RsiCyclicSmoothedStrategy()
	{
		_dominantCycle = Param(nameof(DominantCycleLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Dominant Cycle", "Dominant cycle length", "General")
		.SetCanOptimize(true);
		
		_vibration = Param(nameof(Vibration), 10)
		.SetGreaterThanZero()
		.SetDisplay("Vibration", "Vibration factor", "General")
		.SetCanOptimize(true);
		
		_leveling = Param(nameof(Leveling), 10m)
		.SetGreaterThan(0m)
		.SetDisplay("Leveling", "Percentile for bands", "General")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Candle type for strategy", "General");
	}
	
	public int DominantCycleLength { get => _dominantCycle.Value; set => _dominantCycle.Value = value; }
	public int Vibration { get => _vibration.Value; set => _vibration.Value = value; }
	public decimal Leveling { get => _leveling.Value; set => _leveling.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var cycleLen = Math.Max(DominantCycleLength / 2, 1);
		var phasingLag = Math.Max((Vibration - 1) / 2, 0);
		
		_rsiHistorySize = phasingLag + 1;
		_rsiHistory = new decimal[_rsiHistorySize];
		
		var cyclicMemory = DominantCycleLength * 2;
		_crsiHistory = new decimal[cyclicMemory];
		
		_rsi = new RelativeStrengthIndex
		{
			Length = cycleLen
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, ProcessCandle)
		.Start();
		
		StartProtection(
		new Unit(0, UnitTypes.Absolute),
		new Unit(1m, UnitTypes.Percent),
		false
		);
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var phasingLag = Math.Max((Vibration - 1) / 2, 0);
		_rsiHistory[_rsiIndex] = rsiValue;
		_rsiIndex = (_rsiIndex + 1) % _rsiHistorySize;
		var rsiLag = _rsiHistory[_rsiIndex];
		
		var torque = 2m / (Vibration + 1);
		var prevCrsi = _prevCrsi;
		var crsi = torque * (2m * rsiValue - rsiLag) + (1m - torque) * prevCrsi;
		
		_crsiHistory[_crsiIndex] = crsi;
		_crsiIndex = (_crsiIndex + 1) % _crsiHistory.Length;
		if (_crsiCount < _crsiHistory.Length)
		_crsiCount++;
		
		if (_crsiCount < _crsiHistory.Length)
		{
			_prevCrsi = crsi;
			return;
		}
		
		decimal lmax = decimal.MinValue;
		decimal lmin = decimal.MaxValue;
		for (int i = 0; i < _crsiCount; i++)
		{
			var val = _crsiHistory[i];
			if (val > lmax)
			lmax = val;
			if (val < lmin)
			lmin = val;
		}
		
		var mstep = (lmax - lmin) / 100m;
		var aperc = Leveling / 100m;
		
		decimal db = lmin;
		for (int steps = 0; steps <= 100; steps++)
		{
			var testValue = lmin + mstep * steps;
			var below = 0;
			for (int m = 0; m < _crsiCount; m++)
			{
				if (_crsiHistory[m] < testValue)
				below++;
			}
			
			var ratio = (decimal)below / _crsiCount;
			if (ratio >= aperc)
			{
				db = testValue;
				break;
			}
		}
		
		decimal ub = lmax;
		for (int steps = 0; steps <= 100; steps++)
		{
			var testValue = lmax - mstep * steps;
			var above = 0;
			for (int m = 0; m < _crsiCount; m++)
			{
				if (_crsiHistory[m] >= testValue)
				above++;
			}
			
			var ratio = (decimal)above / _crsiCount;
			if (ratio >= aperc)
			{
				ub = testValue;
				break;
			}
		}
		
		_lowBand = db;
		_highBand = ub;
		
		if (prevCrsi <= _lowBand && crsi > _lowBand && Position <= 0)
		{
			if (Position < 0)
			BuyMarket(-Position);
			
			BuyMarket(Volume);
		}
		else if (prevCrsi >= _highBand && crsi < _highBand && Position >= 0)
		{
			if (Position > 0)
			SellMarket(Position);
			
			SellMarket(Volume);
		}
		
		_prevCrsi = crsi;
	}
}

