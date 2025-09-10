using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades the spread between Bitcoin futures and spot across three timeframes using Z-score.
/// </summary>
public class BitcoinFuturesSpotTriFrameStrategy : Strategy
{
	private readonly StrategyParam<Security> _spot;
	private readonly StrategyParam<DataType> _candleType1;
	private readonly StrategyParam<DataType> _candleType2;
	private readonly StrategyParam<DataType> _candleType3;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _longThreshold;
	private readonly StrategyParam<decimal> _shortThreshold;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _useHoldDays;
	private readonly StrategyParam<int> _holdDays;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	
	private SimpleMovingAverage _sma1 = null!;
	private SimpleMovingAverage _sma2 = null!;
	private SimpleMovingAverage _sma3 = null!;
	private StandardDeviation _std1 = null!;
	private StandardDeviation _std2 = null!;
	private StandardDeviation _std3 = null!;
	
	private decimal? _futures1;
	private decimal? _spot1;
	private decimal? _futures2;
	private decimal? _spot2;
	private decimal? _futures3;
	private decimal? _spot3;
	
	private decimal? _z1;
	private decimal? _z2;
	private decimal? _z3;
	
	private DateTimeOffset _entryTime;
	private TimeSpan _holdPeriod;
	
	/// <summary>
	/// Futures security.
	/// </summary>
	public Security Futures
	{
		get => Security;
		set => Security = value;
	}
	
	/// <summary>
	/// Spot security.
	/// </summary>
	public Security Spot
	{
		get => _spot.Value;
		set => _spot.Value = value;
	}
	
	/// <summary>
	/// First candle type.
	/// </summary>
	public DataType CandleType1
	{
		get => _candleType1.Value;
		set => _candleType1.Value = value;
	}
	
	/// <summary>
	/// Second candle type.
	/// </summary>
	public DataType CandleType2
	{
		get => _candleType2.Value;
		set => _candleType2.Value = value;
	}
	
	/// <summary>
	/// Third candle type.
	/// </summary>
	public DataType CandleType3
	{
		get => _candleType3.Value;
		set => _candleType3.Value = value;
	}
	
	/// <summary>
	/// Period for mean and standard deviation.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}
	
	/// <summary>
	/// Long entry Z-score threshold.
	/// </summary>
	public decimal LongThreshold
	{
		get => _longThreshold.Value;
		set => _longThreshold.Value = value;
	}
	
	/// <summary>
	/// Short entry Z-score threshold.
	/// </summary>
	public decimal ShortThreshold
	{
		get => _shortThreshold.Value;
		set => _shortThreshold.Value = value;
	}
	
	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}
	
	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}
	
	/// <summary>
	/// Use hold days.
	/// </summary>
	public bool UseHoldDays
	{
		get => _useHoldDays.Value;
		set => _useHoldDays.Value = value;
	}
	
	/// <summary>
	/// Number of hold days.
	/// </summary>
	public int HoldDays
	{
		get => _holdDays.Value;
		set => _holdDays.Value = value;
	}
	
	/// <summary>
	/// Use take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}
	
	/// <summary>
	/// Use stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}
	
	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}
	
	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public BitcoinFuturesSpotTriFrameStrategy()
	{
		_spot = Param(nameof(Spot));
		_candleType1 = Param(nameof(CandleType1), TimeSpan.FromMinutes(60).TimeFrame())
		.SetDisplay("Candle Type 1", "First timeframe", "Parameters");
		_candleType2 = Param(nameof(CandleType2), TimeSpan.FromMinutes(120).TimeFrame())
		.SetDisplay("Candle Type 2", "Second timeframe", "Parameters");
		_candleType3 = Param(nameof(CandleType3), TimeSpan.FromMinutes(180).TimeFrame())
		.SetDisplay("Candle Type 3", "Third timeframe", "Parameters");
		_smaPeriod = Param(nameof(SmaPeriod), 100)
		.SetRange(10, 200)
		.SetDisplay("SMA Period", "Period for mean and deviation", "Parameters")
		.SetCanOptimize(true);
		_longThreshold = Param(nameof(LongThreshold), 3m)
		.SetRange(1m, 5m)
		.SetDisplay("Long Z-Score", "Long entry Z-score", "Parameters")
		.SetCanOptimize(true);
		_shortThreshold = Param(nameof(ShortThreshold), -3m)
		.SetRange(-5m, -1m)
		.SetDisplay("Short Z-Score", "Short entry Z-score", "Parameters")
		.SetCanOptimize(true);
		_enableLong = Param(nameof(EnableLong), true)
		.SetDisplay("Enable Long", "Allow long trades", "Parameters");
		_enableShort = Param(nameof(EnableShort), true)
		.SetDisplay("Enable Short", "Allow short trades", "Parameters");
		_useHoldDays = Param(nameof(UseHoldDays), true)
		.SetDisplay("Use Hold Days", "Close position after hold days", "Risk");
		_holdDays = Param(nameof(HoldDays), 5)
		.SetRange(1, 60)
		.SetDisplay("Hold Days", "Number of days to hold position", "Risk")
		.SetCanOptimize(true);
		_useTakeProfit = Param(nameof(UseTakeProfit), false)
		.SetDisplay("Use Take Profit", "Enable take profit", "Risk");
		_useStopLoss = Param(nameof(UseStopLoss), false)
		.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 30m)
		.SetRange(5m, 100m)
		.SetDisplay("Take Profit (%)", "Take profit percent", "Risk")
		.SetCanOptimize(true);
		_stopLossPercent = Param(nameof(StopLossPercent), 20m)
		.SetRange(5m, 100m)
		.SetDisplay("Stop Loss (%)", "Stop loss percent", "Risk")
		.SetCanOptimize(true);
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new (Security, DataType)[]
		{
			(Security, CandleType1),
			(Security, CandleType2),
			(Security, CandleType3),
			(Spot, CandleType1),
			(Spot, CandleType2),
			(Spot, CandleType3)
		};
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_futures1 = _futures2 = _futures3 = null;
		_spot1 = _spot2 = _spot3 = null;
		_z1 = _z2 = _z3 = null;
		_entryTime = default;
		_holdPeriod = default;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		if (Security == null || Spot == null)
		throw new InvalidOperationException("Securities are not specified.");
		
		_sma1 = new SimpleMovingAverage { Length = SmaPeriod };
		_sma2 = new SimpleMovingAverage { Length = SmaPeriod };
		_sma3 = new SimpleMovingAverage { Length = SmaPeriod };
		_std1 = new StandardDeviation { Length = SmaPeriod };
		_std2 = new StandardDeviation { Length = SmaPeriod };
		_std3 = new StandardDeviation { Length = SmaPeriod };
		
		SubscribeCandles(CandleType1, true, Futures).Bind(c => ProcessFutures(c, 1)).Start();
		SubscribeCandles(CandleType1, true, Spot).Bind(c => ProcessSpot(c, 1)).Start();
		SubscribeCandles(CandleType2, true, Futures).Bind(c => ProcessFutures(c, 2)).Start();
		SubscribeCandles(CandleType2, true, Spot).Bind(c => ProcessSpot(c, 2)).Start();
		SubscribeCandles(CandleType3, true, Futures).Bind(c => ProcessFutures(c, 3)).Start();
		SubscribeCandles(CandleType3, true, Spot).Bind(c => ProcessSpot(c, 3)).Start();
		
		var take = UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(0, UnitTypes.Absolute);
		var stop = UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(0, UnitTypes.Absolute);
		StartProtection(take, stop, false);
		
		_holdPeriod = TimeSpan.FromDays(HoldDays);
	}
	
	private void ProcessFutures(ICandleMessage candle, int tf)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (tf == 1)
		_futures1 = candle.ClosePrice;
		else if (tf == 2)
		_futures2 = candle.ClosePrice;
		else
		_futures3 = candle.ClosePrice;
		
		Update(tf, candle.ServerTime);
	}
	
	private void ProcessSpot(ICandleMessage candle, int tf)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (tf == 1)
		_spot1 = candle.ClosePrice;
		else if (tf == 2)
		_spot2 = candle.ClosePrice;
		else
		_spot3 = candle.ClosePrice;
		
		Update(tf, candle.ServerTime);
	}
	
	private void Update(int tf, DateTimeOffset time)
	{
		switch (tf)
		{
			case 1:
			if (_futures1 is not decimal f1 || _spot1 is not decimal s1)
			return;
			var spread1 = f1 - s1;
			_z1 = CalculateZScore(spread1, _sma1, _std1, time);
			break;
			case 2:
			if (_futures2 is not decimal f2 || _spot2 is not decimal s2)
			return;
			var spread2 = f2 - s2;
			_z2 = CalculateZScore(spread2, _sma2, _std2, time);
			break;
			default:
			if (_futures3 is not decimal f3 || _spot3 is not decimal s3)
			return;
			var spread3 = f3 - s3;
			_z3 = CalculateZScore(spread3, _sma3, _std3, time);
			break;
		}
		
		CheckSignal(time);
	}
	
	private decimal? CalculateZScore(decimal spread, SimpleMovingAverage sma, StandardDeviation std, DateTimeOffset time)
	{
		var mean = sma.Process(spread, time, true).ToDecimal();
		var deviation = std.Process(spread, time, true).ToDecimal();
		if (!sma.IsFormed || !std.IsFormed || deviation == 0)
		return null;
		return (spread - mean) / deviation;
	}
	
	private void CheckSignal(DateTimeOffset time)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		if (_z1 is not decimal z1 || _z2 is not decimal z2 || _z3 is not decimal z3)
		return;
		
		var longCond = z1 > LongThreshold && z2 > LongThreshold && z3 > LongThreshold;
		var shortCond = z1 < ShortThreshold && z2 < ShortThreshold && z3 < ShortThreshold;
		
		if (Position == 0)
		{
			if (EnableLong && longCond)
			{
				BuyMarket(Volume);
				_entryTime = time;
			}
			else if (EnableShort && shortCond)
			{
				SellMarket(Volume);
				_entryTime = time;
			}
		}
		else
		{
			if (UseHoldDays && time - _entryTime >= _holdPeriod)
			{
				ClosePosition();
				return;
			}
			
			if (Position > 0 && EnableShort && shortCond)
			{
				var volume = Volume + Position;
				SellMarket(volume);
				_entryTime = time;
			}
			else if (Position < 0 && EnableLong && longCond)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_entryTime = time;
			}
		}
	}
	
	private void ClosePosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0)
		return;
		if (Position > 0)
		SellMarket(volume);
		else
		BuyMarket(volume);
	}
}

