using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining RSI, Stochastic Oscillator and WMA.
/// </summary>
public class RsiStochasticWmaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<int> _wmaLength;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevK;
	private decimal _prevD;
	private bool _hasPrev;
	
	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}
	
	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}
	
	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
	}
	
	/// <summary>
	/// WMA length.
	/// </summary>
	public int WmaLength
	{
		get => _wmaLength.Value;
		set => _wmaLength.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RsiStochasticWmaStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "RSI calculation length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);
		
		_stochK = Param(nameof(StochK), 14)
		.SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);
		
		_stochD = Param(nameof(StochD), 3)
		.SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);
		
		_wmaLength = Param(nameof(WmaLength), 21)
		.SetDisplay("WMA Length", "Weighted moving average length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_hasPrev = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var wma = new WeightedMovingAverage { Length = WmaLength };
		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochK;
		stochastic.D.Length = StochD;
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(rsi, wma, stochastic, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue wmaValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!rsiValue.IsFinal || !wmaValue.IsFinal || !stochValue.IsFinal)
		return;
		
		var rsi = rsiValue.GetValue<decimal>();
		var wma = wmaValue.GetValue<decimal>();
		var stoch = (StochasticOscillatorValue)stochValue;
		
		if (stoch.K is not decimal k || stoch.D is not decimal d)
		return;
		
		var stochCrossUp = _hasPrev && k > d && _prevK <= _prevD;
		var stochCrossDown = _hasPrev && k < d && _prevK >= _prevD;
		
		var priceAboveWma = candle.ClosePrice > wma;
		var priceBelowWma = candle.ClosePrice < wma;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevK = k;
			_prevD = d;
			_hasPrev = true;
			return;
		}
		
		if (rsi < 30m && stochCrossUp && priceAboveWma && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (rsi > 70m && stochCrossDown && priceBelowWma && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		
		_prevK = k;
		_prevD = d;
		_hasPrev = true;
	}
}

