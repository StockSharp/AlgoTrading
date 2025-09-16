namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Xbug Free v4 strategy based on moving average crossing median price.
/// </summary>
public class XbugFreeV4Strategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stopPoints;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _prevSma;
	private decimal? _prevPrice;
	private decimal? _prev2Sma;
	private decimal? _prev2Price;
	private decimal? _stopLoss;
	private decimal? _takeProfit;
	
	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}
	
	/// <summary>
	/// Stop and take profit distance in points.
	/// </summary>
	public int StopPoints
	{
		get => _stopPoints.Value;
		set => _stopPoints.Value = value;
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="XbugFreeV4Strategy"/> class.
	/// </summary>
	public XbugFreeV4Strategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 19)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average length", "Parameters")
		.SetCanOptimize();
		
		_stopPoints = Param(nameof(StopPoints), 270)
		.SetDisplay("Stop Points", "Distance for take profit and stop loss", "Risk");
		
		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		
		_prevSma = null;
		_prevPrice = null;
		_prev2Sma = null;
		_prev2Price = null;
		_stopLoss = null;
		_takeProfit = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var sma = new SimpleMovingAverage
		{
			Length = MaPeriod,
			CandlePrice = CandlePrice.Median,
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(sma, ProcessCandle)
		.Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		
		if (_prevSma is decimal prevSma && _prevPrice is decimal prevPrice && _prev2Sma is decimal prev2Sma && _prev2Price is decimal prev2Price)
		{
			var buySignal = smaValue > median && prevSma > prevPrice && prev2Sma < prev2Price;
			var sellSignal = smaValue < median && prevSma < prevPrice && prev2Sma > prev2Price;
			
			var offset = StopPoints * (Security?.PriceStep ?? 1);
			
			if (buySignal && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				var entry = candle.ClosePrice;
				_stopLoss = entry - offset;
				_takeProfit = entry + offset;
			}
			else if (sellSignal && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				var entry = candle.ClosePrice;
				_stopLoss = entry + offset;
				_takeProfit = entry - offset;
			}
		}
		
		if (Position > 0 && _stopLoss is decimal sl && _takeProfit is decimal tp)
		{
			if (candle.LowPrice <= sl || candle.HighPrice >= tp)
			{
				SellMarket(Position);
				_stopLoss = null;
				_takeProfit = null;
			}
		}
		else if (Position < 0 && _stopLoss is decimal sl2 && _takeProfit is decimal tp2)
		{
			if (candle.HighPrice >= sl2 || candle.LowPrice <= tp2)
			{
				BuyMarket(-Position);
				_stopLoss = null;
				_takeProfit = null;
			}
		}
		
		_prev2Sma = _prevSma;
		_prev2Price = _prevPrice;
		_prevSma = smaValue;
		_prevPrice = median;
	}
}
