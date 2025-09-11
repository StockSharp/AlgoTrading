using System;
using System.Collections.Generic;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Connors VIX Reversal III strategy.
/// Trades contrarian signals based on VIX moving average breakouts.
/// </summary>
public class ConnorsVixReversalIIIStrategy : Strategy
{
	private readonly StrategyParam<int> _lengthMa;
	private readonly StrategyParam<decimal> _percentThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _vixSecurity;
	
	private decimal _previousVixMa;
	private SMA _vixSma = null!;
	
	/// <summary>
	/// Length of VIX moving average.
	/// </summary>
	public int LengthMA
	{
		get => _lengthMa.Value;
		set => _lengthMa.Value = value;
	}
	
	/// <summary>
	/// Percentage threshold for signals.
	/// </summary>
	public decimal PercentThreshold
	{
		get => _percentThreshold.Value;
		set => _percentThreshold.Value = value;
	}
	
	/// <summary>
	/// Candle type for data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Security providing VIX data.
	/// </summary>
	public Security VixSecurity
	{
		get => _vixSecurity.Value;
		set => _vixSecurity.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="ConnorsVixReversalIIIStrategy"/>.
	/// </summary>
	public ConnorsVixReversalIIIStrategy()
	{
		_lengthMa = Param(nameof(LengthMA), 10)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "Length of VIX moving average", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_percentThreshold = Param(nameof(PercentThreshold), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Percent Threshold", "Percentage threshold for signals", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5m, 20m, 1m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "Data");
		
		_vixSecurity = Param<Security>(nameof(VixSecurity))
		.SetDisplay("VIX Security", "Security providing VIX data", "Data")
		.SetRequired();
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, CandleType),
		(VixSecurity, CandleType)
		];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousVixMa = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_vixSma = new SMA { Length = LengthMA };
		
		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Start();
		
		var vixSubscription = SubscribeCandles(CandleType, security: VixSecurity);
		vixSubscription
		.Bind(_vixSma, ProcessVixCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, vixSubscription);
			DrawIndicator(area, _vixSma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessVixCandle(ICandleMessage candle, decimal vixMa)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!_vixSma.IsFormed)
		{
			_previousVixMa = vixMa;
			return;
		}
		
		var vixClose = candle.ClosePrice;
		var vixHigh = candle.HighPrice;
		var vixLow = candle.LowPrice;
		
		var buySignal = vixLow > vixMa && vixClose > vixMa * (1 + PercentThreshold / 100m);
		var sellSignal = vixHigh < vixMa && vixClose < vixMa * (1 - PercentThreshold / 100m);
		
		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		
		if (_previousVixMa != 0m)
		{
			if (Position > 0 && vixLow < _previousVixMa)
			SellMarket(Position);
			
			if (Position < 0 && vixHigh > _previousVixMa)
			BuyMarket(Math.Abs(Position));
		}
		
		_previousVixMa = vixMa;
	}
}
