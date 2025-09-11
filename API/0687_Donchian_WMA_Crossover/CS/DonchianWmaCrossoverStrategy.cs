
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian low crossing above WMA during year 2025 with adjustable take profit.
/// Long positions only.
/// </summary>
public class DonchianWmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianLength;
	private readonly StrategyParam<int> _wmaLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private bool _initialized;
	private decimal _prevDonLow;
	private decimal _prevWma;
	
	private static readonly DateTimeOffset _startDate = new(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
	private static readonly DateTimeOffset _endDate = new(new DateTime(2025, 12, 31, 23, 59, 0, DateTimeKind.Utc));
	
	/// <summary>
	/// Donchian channel length.
	/// </summary>
	public int DonchianLength { get => _donchianLength.Value; set => _donchianLength.Value = value; }
	
	/// <summary>
	/// Weighted moving average length.
	/// </summary>
	public int WmaLength { get => _wmaLength.Value; set => _wmaLength.Value = value; }
	
	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize <see cref="DonchianWmaCrossoverStrategy"/>.
	/// </summary>
	public DonchianWmaCrossoverStrategy()
	{
		_donchianLength = Param(nameof(DonchianLength), 7)
		.SetGreaterThanZero()
		.SetDisplay("Donchian Length", "Period for Donchian channel", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);
		
		_wmaLength = Param(nameof(WmaLength), 62)
		.SetGreaterThanZero()
		.SetDisplay("WMA Length", "Period for weighted moving average", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 1);
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit as decimal", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.005m, 0.05m, 0.005m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_initialized = false;
		_prevDonLow = 0m;
		_prevWma = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var donchian = new DonchianChannels { Length = DonchianLength };
		var wma = new WeightedMovingAverage { Length = WmaLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(donchian, wma, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue wmaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var dc = (DonchianChannelsValue)donchianValue;
		if (dc.LowerBand is not decimal donLow)
		return;
		
		var wma = wmaValue.ToDecimal();
		
		var in2025 = candle.OpenTime >= _startDate && candle.OpenTime <= _endDate;
		
		if (!_initialized)
		{
			_prevDonLow = donLow;
			_prevWma = wma;
			_initialized = true;
			return;
		}
		
		var crossUp = _prevDonLow <= _prevWma && donLow > wma;
		var crossDown = _prevDonLow >= _prevWma && donLow < wma;
		var wmaUp = wma > _prevWma;
		
		if (crossUp && in2025 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0)
		{
			var exitTp = candle.ClosePrice >= PositionPrice * (1 + TakeProfitPercent);
			var exitX = crossDown && !wmaUp;
			var exitAll = exitTp || exitX || !in2025;
			
			if (exitAll)
			SellMarket(Position);
		}
		
		_prevDonLow = donLow;
		_prevWma = wma;
	}
}
