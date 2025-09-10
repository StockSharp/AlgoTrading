	using System;
	using System.Collections.Generic;
	
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	
	namespace StockSharp.Samples.Strategies;
	
	/// <summary>
	/// BTC DCA AHR999 Strategy - buys on Mondays based on AHR999 index thresholds.
	/// </summary>
	public class BtcDcaAhr999Strategy : Strategy
	{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _usdInvest1;
	private readonly StrategyParam<decimal> _usdInvest2;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	
	private readonly SMA _logSma = new();
	private long _barIndex;
	private decimal _totalInvested;
	private decimal _totalQuantity;
	private decimal _lastPrice;
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Investment amount when AHR999 < 1.2.
	/// </summary>
	public decimal UsdInvest1
	{
	get => _usdInvest1.Value;
	set => _usdInvest1.Value = value;
	}
	
	/// <summary>
	/// Investment amount when AHR999 < 0.45.
	/// </summary>
	public decimal UsdInvest2
	{
	get => _usdInvest2.Value;
	set => _usdInvest2.Value = value;
	}
	
	/// <summary>
	/// Length for geometric mean calculation.
	/// </summary>
	public int Length
	{
	get => _length.Value;
	set => _length.Value = value;
	}
	
	/// <summary>
	/// Start date of accumulation.
	/// </summary>
	public DateTimeOffset StartDate
	{
	get => _startDate.Value;
	set => _startDate.Value = value;
	}
	
	/// <summary>
	/// End date of accumulation.
	/// </summary>
	public DateTimeOffset EndDate
	{
	get => _endDate.Value;
	set => _endDate.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public BtcDcaAhr999Strategy()
	{
	_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to use", "General");
	
	_usdInvest1 = Param(nameof(UsdInvest1), 100m)
	.SetRange(0.01m, 1000000m)
	.SetDisplay("USD Invest 1", "Amount when AHR999 < 1.2", "DCA");
	
	_usdInvest2 = Param(nameof(UsdInvest2), 1000m)
	.SetRange(0.01m, 1000000m)
	.SetDisplay("USD Invest 2", "Amount when AHR999 < 0.45", "DCA");
	
	_length = Param(nameof(Length), 200)
	.SetGreaterThanZero()
	.SetDisplay("Length", "Geometric mean length", "Indicator");
	
	_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2024, 2, 1)))
	.SetDisplay("Start Date", "Start date of accumulation", "DCA");
	
	_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2025, 12, 31)))
	.SetDisplay("End Date", "End date of accumulation", "DCA");
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
	
	_logSma.Length = Length;
	_barIndex = 0;
	_totalInvested = 0;
	_totalQuantity = 0;
	_lastPrice = 0;
	
	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();
	
	StartProtection();
	
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
	
	_barIndex++;
	
	var scaleClose = candle.ClosePrice / 10000m;
	var logClose = (decimal)Math.Log((double)scaleClose);
	var logAvg = _logSma.Process(logClose);
	if (!_logSma.IsFormed)
	{
	_lastPrice = candle.ClosePrice;
	return;
	}
	
	var gma = (decimal)Math.Exp((double)logAvg);
	var avgIndex = scaleClose / gma;
	
	var bitcoinAge = _barIndex + 561m;
	var estimatePrice = (decimal)Math.Pow(10, 5.84 * Math.Log10((double)bitcoinAge) - 17.01);
	var estimateIndex = candle.ClosePrice / estimatePrice;
	var ahr999 = avgIndex * estimateIndex;
	
	var inRange = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;
	var isMonday = candle.OpenTime.DayOfWeek == DayOfWeek.Monday;
	
	if (inRange && isMonday && IsOnline)
	{
	var qty100 = UsdInvest1 / candle.ClosePrice;
	var qty1000 = UsdInvest2 / candle.ClosePrice;
	
	if (ahr999 < 0.45m)
	{
	RegisterOrder(CreateOrder(Sides.Buy, candle.ClosePrice, qty1000));
	_totalInvested += UsdInvest2;
	_totalQuantity += qty1000;
	}
	else if (ahr999 < 1.2m)
	{
	RegisterOrder(CreateOrder(Sides.Buy, candle.ClosePrice, qty100));
	_totalInvested += UsdInvest1;
	_totalQuantity += qty100;
	}
	}
	
	_lastPrice = candle.ClosePrice;
	}
	
	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
	var portfolioValue = _totalQuantity * _lastPrice;
	var profit = portfolioValue - _totalInvested;
	var percent = _totalInvested == 0 ? 0 : profit / _totalInvested * 100m;
	LogInfo($"Spent: {_totalInvested:0.##}, Qty: {_totalQuantity:0.######}, Value: {portfolioValue:0.##}, PnL: {profit:0.##} ({percent:0.##}%)");
	base.OnStopped(time);
	}
	}
