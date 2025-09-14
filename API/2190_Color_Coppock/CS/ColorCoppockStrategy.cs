namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the Color Coppock oscillator.
/// </summary>
public class ColorCoppockStrategy : Strategy
{
	private readonly StrategyParam<int> _roc1Period;
	private readonly StrategyParam<int> _roc2Period;
	private readonly StrategyParam<int> _smoothingPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private RateOfChange _roc1;
	private RateOfChange _roc2;
	private SimpleMovingAverage _sma;
	
	private decimal _prevValue;
	private decimal _prevPrevValue;
	private bool _isFormed;
	
	/// <summary>
	/// First ROC period.
	/// </summary>
	public int Roc1Period { get => _roc1Period.Value; set => _roc1Period.Value = value; }
	
	/// <summary>
	/// Second ROC period.
	/// </summary>
	public int Roc2Period { get => _roc2Period.Value; set => _roc2Period.Value = value; }
	
	/// <summary>
	/// Smoothing period for the summed ROC values.
	/// </summary>
	public int SmoothingPeriod { get => _smoothingPeriod.Value; set => _smoothingPeriod.Value = value; }
	
	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ColorCoppockStrategy"/>.
	/// </summary>
	public ColorCoppockStrategy()
	{
		_roc1Period = Param(nameof(Roc1Period), 14)
		.SetDisplay("ROC1 Period", "First ROC calculation period", "Parameters")
		.SetCanOptimize(true);
		
		_roc2Period = Param(nameof(Roc2Period), 10)
		.SetDisplay("ROC2 Period", "Second ROC calculation period", "Parameters")
		.SetCanOptimize(true);
		
		_smoothingPeriod = Param(nameof(SmoothingPeriod), 12)
		.SetDisplay("Smoothing Period", "SMA period for ROC sum", "Parameters")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for processing", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_prevValue = 0m;
		_prevPrevValue = 0m;
		_isFormed = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_roc1 = new RateOfChange { Length = Roc1Period };
		_roc2 = new RateOfChange { Length = Roc2Period };
		_sma = new SimpleMovingAverage { Length = SmoothingPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
		
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
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var price = candle.ClosePrice;
		var roc1Value = _roc1.Process(price, candle.OpenTime, true).ToDecimal();
		var roc2Value = _roc2.Process(price, candle.OpenTime, true).ToDecimal();
		var coppock = _sma.Process(roc1Value + roc2Value, candle.OpenTime, true).ToDecimal();
		
		if (!_isFormed)
		{
			_prevValue = coppock;
			_prevPrevValue = coppock;
			_isFormed = true;
			return;
		}
		
		if (_prevValue < _prevPrevValue)
		{
			if (coppock > _prevValue)
			{
				if (Position < 0)
				BuyMarket(Math.Abs(Position));
				if (Position == 0)
				BuyMarket(Volume);
			}
		}
		else if (_prevValue > _prevPrevValue)
		{
			if (coppock < _prevValue)
			{
				if (Position > 0)
				SellMarket(Math.Abs(Position));
				if (Position == 0)
				SellMarket(Volume);
			}
		}
		
		_prevPrevValue = _prevValue;
		_prevValue = coppock;
	}
}
