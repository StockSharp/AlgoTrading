using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price Volume Trend crossover with its EMA.
/// Enters long when PVT crosses above EMA, short when crosses below.
/// </summary>
public class PvtCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _ema;
	private decimal _pvt;
	private decimal _prevPvt;
	private decimal _prevEma;
	private decimal _prevClose;
	private bool _isInitialized;
	
	/// <summary>
	/// EMA length for PVT smoothing.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}
	
	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="PvtCrossoverStrategy"/>.
	/// </summary>
	public PvtCrossoverStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "EMA period for PVT smoothing", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_pvt = 0;
		_prevPvt = 0;
		_prevEma = 0;
		_prevClose = 0;
		_isInitialized = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		
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
		
		if (_prevClose != 0)
		{
			var change = (candle.ClosePrice - _prevClose) / _prevClose;
			_pvt += change * candle.TotalVolume;
		}
		
		_prevClose = candle.ClosePrice;
		
		var emaValue = _ema.Process(_pvt, candle.OpenTime, true).ToDecimal();
		
		if (!_ema.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		{
			_prevPvt = _pvt;
			_prevEma = emaValue;
			_isInitialized = _ema.IsFormed;
			return;
		}
		
		if (!_isInitialized)
		{
			_prevPvt = _pvt;
			_prevEma = emaValue;
			_isInitialized = true;
			return;
		}
		
		var wasBelow = _prevPvt < _prevEma;
		var isBelow = _pvt < emaValue;
		
		if (wasBelow && !isBelow && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!wasBelow && isBelow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		
		_prevPvt = _pvt;
		_prevEma = emaValue;
	}
}
