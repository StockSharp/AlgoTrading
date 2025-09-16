using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double smoothed moving average crossover strategy.
/// The price is smoothed by a fast EMA and then by a slow EMA.
/// Positions are opened on crossovers of these two series.
/// </summary>
public class MaByMaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private bool _isInitialized;
	private bool _wasFastBelowSlow;
	
	/// <summary>
	/// Fast EMA period length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	
	/// <summary>
	/// Slow EMA period length applied to fast EMA output.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	
	/// <summary>
	/// Allow long positions.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}
	
	/// <summary>
	/// Allow short positions.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
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
	/// Constructor.
	/// </summary>
	public MaByMaStrategy()
	{
		_fastLength = Param(nameof(FastLength), 7)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Length", "Period for first EMA", "Indicator");
		
		_slowLength = Param(nameof(SlowLength), 7)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Length", "Period for second EMA", "Indicator");
		
		_enableLong = Param(nameof(EnableLong), true)
		.SetDisplay("Enable Long", "Allow long entries", "Trading");
		
		_enableShort = Param(nameof(EnableShort), true)
		.SetDisplay("Enable Short", "Allow short entries", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		
		_fastMa = new ExponentialMovingAverage { Length = FastLength };
		_slowMa = new ExponentialMovingAverage { Length = SlowLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var slowValue = _slowMa.Process(fastValue, candle.OpenTime, true).ToDecimal();
		
		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		return;
		
		if (!_isInitialized)
		{
			_wasFastBelowSlow = fastValue < slowValue;
			_isInitialized = true;
			return;
		}
		
		var isFastBelowSlow = fastValue < slowValue;
		
		if (_wasFastBelowSlow && !isFastBelowSlow)
		{
			// Fast EMA crossed above slow EMA
			if (EnableLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!_wasFastBelowSlow && isFastBelowSlow)
		{
			// Fast EMA crossed below slow EMA
			if (EnableShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}
		
		_wasFastBelowSlow = isFastBelowSlow;
	}
}
