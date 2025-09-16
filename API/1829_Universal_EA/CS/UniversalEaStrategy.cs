using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that buys when the stochastic oscillator crosses up in oversold zone
/// and sells when it crosses down in overbought zone.
/// </summary>
public class UniversalEaStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevK;
	private decimal _prevD;
	
	/// <summary>
	/// %K calculation period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}
	
	/// <summary>
	/// %D smoothing period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}
	
	/// <summary>
	/// %K slowing period.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}
	
	/// <summary>
	/// Oversold threshold for %K.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}
	
	/// <summary>
	/// Overbought threshold for %K.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}
	
	/// <summary>
	/// Type of candles to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="UniversalEaStrategy"/>.
	/// </summary>
	public UniversalEaStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Stochastic calculation period", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);
		
		_dPeriod = Param(nameof(DPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "%D smoothing period", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(1, 20, 1);
		
		_slowing = Param(nameof(Slowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Slowing", "%K slowing period", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(1, 20, 1);
		
		_oversold = Param(nameof(Oversold), 20m)
		.SetDisplay("Oversold", "%K oversold threshold", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);
		
		_overbought = Param(nameof(Overbought), 80m)
		.SetDisplay("Overbought", "%K overbought threshold", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);
		
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
		_prevK = 0m;
		_prevD = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			K = { Length = Slowing },
			D = { Length = DPeriod },
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(stochastic, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var d = stoch.D;
		
		// Buy when %K crosses above %D in the oversold zone
		if (_prevK < _prevD && k > d && k < Oversold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		// Sell when %K crosses below %D in the overbought zone
		else if (_prevK > _prevD && k < d && k > Overbought && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		
		_prevK = k;
		_prevD = d;
	}
}
