using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy.
/// Buys when the short SMA crosses above the long SMA and sells on the opposite signal.
/// </summary>
public class MovingAverageCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevShort;
	private decimal _prevLong;
	private bool _isFirst;
	
	/// <summary>
	/// Short SMA period length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}
	
	/// <summary>
	/// Long SMA period length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}
	
	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageCrossoverStrategy"/> class.
	/// </summary>
	public MovingAverageCrossoverStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Short MA Length", "Period of the short moving average", "General")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_longLength = Param(nameof(LongLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("Long MA Length", "Period of the long moving average", "General")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 5);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_prevShort = 0m;
		_prevLong = 0m;
		_isFirst = true;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var shortMa = new SMA { Length = ShortLength };
		var longMa = new SMA { Length = LongLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(shortMa, longMa, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		if (_isFirst)
		{
			_prevShort = shortValue;
			_prevLong = longValue;
			_isFirst = false;
			return;
		}
		
		var crossUp = _prevShort <= _prevLong && shortValue > longValue;
		var crossDown = _prevShort >= _prevLong && shortValue < longValue;
		
		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		
		_prevShort = shortValue;
		_prevLong = longValue;
	}
}
