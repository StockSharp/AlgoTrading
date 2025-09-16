namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Chaikin oscillator with linear weighted moving averages.
/// </summary>
public class SpectrAnalysisChaikinStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;
	
	private WeightedMovingAverage _fastWma;
	private WeightedMovingAverage _slowWma;
	private decimal _prev1;
	private decimal _prev2;
	private bool _isFormed;
	
	/// <summary>
	/// Fast MA period.
	/// </summary>
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	
	/// <summary>
	/// Slow MA period.
	/// </summary>
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	
	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	
	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }
	
	/// <summary>
	/// Candle type used for calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="SpectrAnalysisChaikinStrategy"/> class.
	/// </summary>
	public SpectrAnalysisChaikinStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Fast MA period", "Indicator");
		
		_slowMaPeriod = Param(nameof(SlowMaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Slow MA period", "Indicator");
		
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Buy Position Open", "Allow opening long positions", "Trading");
		
		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Sell Position Open", "Allow opening short positions", "Trading");
		
		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Buy Position Close", "Allow closing long positions", "Trading");
		
		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Sell Position Close", "Allow closing short positions", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "Data");
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
		_prev1 = 0m;
		_prev2 = 0m;
		_isFormed = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var ad = new AccumulationDistributionLine();
		_fastWma = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowWma = new WeightedMovingAverage { Length = SlowMaPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ad, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ad);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal adValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var fast = _fastWma.Process(new DecimalIndicatorValue(_fastWma, adValue, candle.Time));
		var slow = _slowWma.Process(new DecimalIndicatorValue(_slowWma, adValue, candle.Time));
		
		if (!fast.IsFinal || !slow.IsFinal)
		return;
		
		var curr = fast.ToDecimal() - slow.ToDecimal();
		
		if (_isFormed)
		{
			if (_prev1 < _prev2)
			{
				if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));
				
				if (BuyPosOpen && curr >= _prev1 && Position <= 0)
				BuyMarket();
			}
			else if (_prev1 > _prev2)
			{
				if (BuyPosClose && Position > 0)
				SellMarket(Position);
				
				if (SellPosOpen && curr <= _prev1 && Position >= 0)
				SellMarket();
			}
		}
		
		_prev2 = _prev1;
		_prev1 = curr;
		_isFormed = true;
	}
}