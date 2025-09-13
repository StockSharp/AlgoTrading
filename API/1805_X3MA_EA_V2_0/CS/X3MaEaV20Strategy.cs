using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average crossover strategy.
/// Uses fast, medium and slow moving averages for signals.
/// </summary>
public class X3MaEaV20Strategy : Strategy
{
	private readonly StrategyParam<bool> _enableEntryMediumSlowCross;
	private readonly StrategyParam<bool> _enableExitFastSlowCross;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _mediumMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevFast;
	private decimal _prevMedium;
	private decimal _prevSlow;
	private bool _isFirst = true;
	
	/// <summary>
	/// Enable entries when the medium MA crosses the slow MA.
	/// </summary>
	public bool EnableEntryMediumSlowCross
	{
		get => _enableEntryMediumSlowCross.Value;
		set => _enableEntryMediumSlowCross.Value = value;
	}
	
	/// <summary>
	/// Enable exits when the fast MA crosses the slow MA.
	/// </summary>
	public bool EnableExitFastSlowCross
	{
		get => _enableExitFastSlowCross.Value;
		set => _enableExitFastSlowCross.Value = value;
	}
	
	/// <summary>
	/// Period for the fast moving average.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}
	
	/// <summary>
	/// Period for the medium moving average.
	/// </summary>
	public int MediumMaLength
	{
		get => _mediumMaLength.Value;
		set => _mediumMaLength.Value = value;
	}
	
	/// <summary>
	/// Period for the slow moving average.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}
	
	/// <summary>
	/// Type of candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="X3MaEaV20Strategy"/>.
	/// </summary>
	public X3MaEaV20Strategy()
	{
		_enableEntryMediumSlowCross = Param(nameof(EnableEntryMediumSlowCross), true)
		.SetDisplay("Enable Entry Medium/Slow Cross", "Open positions on medium/slow crossover", "General");
		
		_enableExitFastSlowCross = Param(nameof(EnableExitFastSlowCross), true)
		.SetDisplay("Enable Exit Fast/Slow Cross", "Close positions on fast/slow crossover", "General");
		
		_fastMaLength = Param(nameof(FastMaLength), 2)
		.SetDisplay("Fast MA Length", "Period of fast moving average", "Moving Averages")
		.SetCanOptimize(true);
		
		_mediumMaLength = Param(nameof(MediumMaLength), 8)
		.SetDisplay("Medium MA Length", "Period of medium moving average", "Moving Averages")
		.SetCanOptimize(true);
		
		_slowMaLength = Param(nameof(SlowMaLength), 16)
		.SetDisplay("Slow MA Length", "Period of slow moving average", "Moving Averages")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		
		StartProtection();
		
		var fastMa = new SimpleMovingAverage { Length = FastMaLength };
		var mediumMa = new SimpleMovingAverage { Length = MediumMaLength };
		var slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(fastMa, mediumMa, slowMa, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, mediumMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal medium, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (_isFirst)
		{
			_prevFast = fast;
			_prevMedium = medium;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (EnableEntryMediumSlowCross && _prevMedium <= _prevSlow && medium > slow)
		{
			if (Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		}
		else
		{
			var bullish = _prevFast <= _prevMedium && fast > medium && fast >= slow && medium >= slow;
			var bearish = _prevFast >= _prevMedium && fast < medium && fast <= slow && medium <= slow;
			
			if (bullish && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (bearish && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		
		if (EnableExitFastSlowCross)
		{
			if (Position > 0 && _prevFast >= _prevSlow && fast < slow)
			SellMarket(Position);
			else if (Position < 0 && _prevFast <= _prevSlow && fast > slow)
			BuyMarket(-Position);
		}
		
		_prevFast = fast;
		_prevMedium = medium;
		_prevSlow = slow;
	}
}
