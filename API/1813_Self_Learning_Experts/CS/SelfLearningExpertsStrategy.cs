using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Self-learning strategy based on binary price patterns.
/// It records historical pattern outcomes and trades
/// when probability exceeds a threshold.
/// </summary>
public class SelfLearningExpertsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _probability;
	private readonly StrategyParam<decimal> _forgetRate;
	private readonly StrategyParam<int> _patternSize;
	private readonly StrategyParam<bool> _replaceStops;
	private readonly StrategyParam<int> _trailing;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	
	private int _pattern;
	private int _historyCount;
	private decimal? _stopLoss;
	private decimal? _takeProfit;
	
	private const int MaxPatternSize = 12;
	private const int MaxPatternCount = 1 << MaxPatternSize;
	private readonly decimal[] _upPower = new decimal[MaxPatternCount];
	private readonly decimal[] _downPower = new decimal[MaxPatternCount];
	
	/// <summary>
	/// Probability threshold required for trading.
	/// </summary>
	public decimal ProbabilityThreshold
	{
		get => _probability.Value;
		set => _probability.Value = value;
	}
	
	/// <summary>
	/// Forgetting rate applied to historical statistics.
	/// </summary>
	public decimal ForgetRate
	{
		get => _forgetRate.Value;
		set => _forgetRate.Value = value;
	}
	
	/// <summary>
	/// Number of candles used to build pattern key.
	/// </summary>
	public int PatternSize
	{
		get => _patternSize.Value;
		set => _patternSize.Value = value;
	}
	
	/// <summary>
	/// Move stops when new signal appears.
	/// </summary>
	public bool ReplaceStops
	{
		get => _replaceStops.Value;
		set => _replaceStops.Value = value;
	}
	
	/// <summary>
	/// Trailing distance in price steps.
	/// </summary>
	public int Trailing
	{
		get => _trailing.Value;
		set => _trailing.Value = value;
	}
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initialize parameters with defaults.
	/// </summary>
	public SelfLearningExpertsStrategy()
	{
		_probability = Param(nameof(ProbabilityThreshold), 0.8m)
		.SetDisplay("Probability", "Threshold for trade", "General");
		
		_forgetRate = Param(nameof(ForgetRate), 1.05m)
		.SetDisplay("Forget rate", "Decay factor for statistics", "General");
		
		_patternSize = Param(nameof(PatternSize), 10)
		.SetDisplay("Pattern size", "Number of steps in pattern", "Pattern");
		
		_replaceStops = Param(nameof(ReplaceStops), false)
		.SetDisplay("Replace stops", "Move stops on new signal", "Risk");
		
		_trailing = Param(nameof(Trailing), 0)
		.SetDisplay("Trailing", "Trailing distance in steps", "Risk");
		
		_volume = Param(nameof(Volume), 1m)
		.SetDisplay("Volume", "Order volume", "General");
		
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
		_pattern = 0;
		_historyCount = 0;
		_stopLoss = null;
		_takeProfit = null;
		Array.Clear(_upPower);
		Array.Clear(_downPower);
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		UpdateStops(candle);
		
		var direction = candle.ClosePrice > candle.OpenPrice ? 1 : 0;
		var mask = (1 << PatternSize) - 1;
		
		if (_historyCount >= PatternSize)
		{
			var up = _upPower[_pattern] / ForgetRate;
			var down = _downPower[_pattern] / ForgetRate;
			
			if (direction == 1)
			up += 1;
			else
			down += 1;
			
			_upPower[_pattern] = up;
			_downPower[_pattern] = down;
			
			var total = up + down;
			if (total > 0)
			{
				var probUp = up / total;
				var step = Security?.PriceStep ?? 1m;
				var tradeVolume = Volume + Math.Abs(Position);
				
				if (probUp >= ProbabilityThreshold && Position <= 0)
				{
					BuyMarket(tradeVolume);
					SetInitialStops(candle.ClosePrice, step, true);
				}
				else if ((1 - probUp) >= ProbabilityThreshold && Position >= 0)
				{
					SellMarket(tradeVolume);
					SetInitialStops(candle.ClosePrice, step, false);
				}
			}
		}
		
		_pattern = ((_pattern << 1) | direction) & mask;
		_historyCount++;
	}
	
	private void SetInitialStops(decimal price, decimal step, bool isBuy)
	{
		var offset = Trailing * step;
		if (isBuy)
		{
			_stopLoss = price - offset;
			_takeProfit = price + offset;
		}
		else
		{
			_stopLoss = price + offset;
			_takeProfit = price - offset;
		}
	}
	
	private void UpdateStops(ICandleMessage candle)
	{
		if (Position == 0 || Trailing <= 0 || Security?.PriceStep == null)
		return;
		
		var step = Security.PriceStep.Value;
		var offset = Trailing * step;
		
		if (Position > 0)
		{
			var newStop = candle.ClosePrice - offset;
			if (_stopLoss == null || newStop > _stopLoss)
			_stopLoss = newStop;
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + offset;
			if (_stopLoss == null || newStop < _stopLoss)
			_stopLoss = newStop;
		}
		
		if (ReplaceStops)
		{
			if (Position > 0)
			_takeProfit = candle.ClosePrice + offset;
			else if (Position < 0)
			_takeProfit = candle.ClosePrice - offset;
		}
		
		if (_stopLoss == null && _takeProfit == null)
		return;
		
		if (Position > 0)
		{
			if (_stopLoss != null && candle.LowPrice <= _stopLoss)
			SellMarket(Position);
			else if (_takeProfit != null && candle.HighPrice >= _takeProfit)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (_stopLoss != null && candle.HighPrice >= _stopLoss)
			BuyMarket(-Position);
			else if (_takeProfit != null && candle.LowPrice <= _takeProfit)
			BuyMarket(-Position);
		}
	}
}

