using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price Flip strategy.
/// Buys when previous close is above inverted price and fast MA crosses above slow MA.
/// Sells when previous close is below inverted price and fast MA crosses below slow MA.
/// </summary>
public class PriceFlipStrategy : Strategy
{
	private readonly StrategyParam<int> _tickerMaxLookback;
	private readonly StrategyParam<int> _tickerMinLookback;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<DataType> _candleType;
	
	private Highest _tickerMax = null!;
	private Lowest _tickerMin = null!;
	private SMA _fastMa = null!;
	private SMA _slowMa = null!;
	
	private decimal _prevFastMa;
	private decimal _prevSlowMa;
	private decimal _prevClose;
	private bool _isInitialized;
	
	/// <summary>
	/// Lookback period for highest high calculation.
	/// </summary>
	public int TickerMaxLookback
	{
		get => _tickerMaxLookback.Value;
		set => _tickerMaxLookback.Value = value;
	}
	
	/// <summary>
	/// Lookback period for lowest low calculation.
	/// </summary>
	public int TickerMinLookback
	{
		get => _tickerMinLookback.Value;
		set => _tickerMinLookback.Value = value;
	}
	
	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}
	
	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}
	
	/// <summary>
	/// Use slow MA as trend filter.
	/// </summary>
	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}
	
	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public PriceFlipStrategy()
	{
		_tickerMaxLookback = Param(nameof(TickerMaxLookback), 100)
		.SetGreaterThanZero()
		.SetDisplay("Ticker Max Lookback", "Lookback for highest high", "Indicators")
		.SetCanOptimize(true);
		
		_tickerMinLookback = Param(nameof(TickerMinLookback), 100)
		.SetGreaterThanZero()
		.SetDisplay("Ticker Min Lookback", "Lookback for lowest low", "Indicators")
		.SetCanOptimize(true);
		
		_fastMaLength = Param(nameof(FastMaLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Length", "Length of fast moving average", "Indicators")
		.SetCanOptimize(true);
		
		_slowMaLength = Param(nameof(SlowMaLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Length", "Length of slow moving average", "Indicators")
		.SetCanOptimize(true);
		
		_useTrendFilter = Param(nameof(UseTrendFilter), true)
		.SetDisplay("Use Trend Filter", "Enable trend filter based on slow MA", "Strategy Parameters")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "Data");
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
		_prevFastMa = _prevSlowMa = _prevClose = 0m;
		_isInitialized = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_tickerMax = new Highest { Length = TickerMaxLookback };
		_tickerMin = new Lowest { Length = TickerMinLookback };
		_fastMa = new SMA { Length = FastMaLength };
		_slowMa = new SMA { Length = SlowMaLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_tickerMax, _tickerMin, _fastMa, _slowMa, ProcessCandle)
		.Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal maxValue, decimal minValue, decimal fastMaValue, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!_isInitialized)
		{
			_prevFastMa = fastMaValue;
			_prevSlowMa = slowMaValue;
			_prevClose = candle.ClosePrice;
			_isInitialized = true;
			return;
		}
		
		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_tickerMax.IsFormed || !_tickerMin.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		{
			_prevFastMa = fastMaValue;
			_prevSlowMa = slowMaValue;
			_prevClose = candle.ClosePrice;
			return;
		}
		
		var invertedPrice = maxValue + minValue - candle.ClosePrice;
		
		var trendUp = UseTrendFilter ? candle.ClosePrice > slowMaValue : true;
		var trendDown = UseTrendFilter ? candle.ClosePrice < slowMaValue : true;
		
		var bullishCross = _prevFastMa <= _prevSlowMa && fastMaValue > slowMaValue;
		var bearishCross = _prevFastMa >= _prevSlowMa && fastMaValue < slowMaValue;
		
		if (_prevClose > invertedPrice && bullishCross && trendUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevClose < invertedPrice && bearishCross && trendDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		
		_prevFastMa = fastMaValue;
		_prevSlowMa = slowMaValue;
		_prevClose = candle.ClosePrice;
	}
}

