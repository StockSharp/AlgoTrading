using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on the smoothed difference between Stochastic %K and %D.
/// Opens long when the diff turns upward and short when it turns downward.
/// </summary>
public class StochasticDiffStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	
	private StochasticOscillator _stochastic = null!;
	private ExponentialMovingAverage _smoothing = null!;
	private decimal? _prevDiff;
	private decimal? _prevPrevDiff;
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}
	
	/// <summary>
	/// %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}
	
	/// <summary>
	/// %K slowing periods.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}
	
	/// <summary>
	/// Smoothing length for the %K-%D difference.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}
	
	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public StochasticDiffStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
		.SetDisplay("Candle Type", "Candle type for analysis", "General")
		.SetCanOptimize(false);
		
		_kPeriod = Param(nameof(KPeriod), 5)
		.SetDisplay("%K Period", "Stochastic %K period", "Stochastic");
		
		_dPeriod = Param(nameof(DPeriod), 3)
		.SetDisplay("%D Period", "Stochastic %D period", "Stochastic");
		
		_slowing = Param(nameof(Slowing), 3)
		.SetDisplay("Slowing", "%K slowing periods", "Stochastic");
		
		_smoothingLength = Param(nameof(SmoothingLength), 13)
		.SetDisplay("Smoothing Length", "Length for diff smoothing", "Stochastic");
		
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
		.SetCanOptimize(false);
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
		.SetCanOptimize(false);
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			K = { Length = Slowing },
			D = { Length = DPeriod }
		};
		
		_smoothing = new ExponentialMovingAverage { Length = SmoothingLength };
		
		StartProtection(
		takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_stochastic, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _smoothing);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal kValue, decimal dValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var diff = kValue - dValue;
		var value = _smoothing.Process(diff);
		
		if (!_smoothing.IsFormed)
		return;
		
		var current = value.GetValue<decimal>();
		
		if (_prevPrevDiff.HasValue && _prevDiff.HasValue)
		{
			var turningUp = _prevDiff < _prevPrevDiff && current >= _prevDiff;
			var turningDown = _prevDiff > _prevPrevDiff && current <= _prevDiff;
			
			if (turningUp && Position <= 0)
			BuyMarket();
			else if (turningDown && Position >= 0)
			SellMarket();
		}
		
		_prevPrevDiff = _prevDiff;
		_prevDiff = current;
	}
}

