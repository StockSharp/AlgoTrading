using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Ozymandias indicator.
/// Opens a position when the indicator changes its direction and closes the opposite one.
/// </summary>
public class OzymandiasStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyEntry;
	private readonly StrategyParam<bool> _sellEntry;
	private readonly StrategyParam<bool> _buyExit;
	private readonly StrategyParam<bool> _sellExit;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private SimpleMovingAverage _hma = null!;
	private SimpleMovingAverage _lma = null!;
	private AverageTrueRange _atr = null!;
	
	private int _trend;
	private int _nextTrend;
	private decimal _maxl;
	private decimal _minh;
	private decimal _baseLine;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int? _prevDirection;
	
	/// <summary>
	/// Lookback length for calculations.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyEntry { get => _buyEntry.Value; set => _buyEntry.Value = value; }
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellEntry { get => _sellEntry.Value; set => _sellEntry.Value = value; }
	
	/// <summary>
	/// Allow closing long positions on signal.
	/// </summary>
	public bool BuyExit { get => _buyExit.Value; set => _buyExit.Value = value; }
	
	/// <summary>
	/// Allow closing short positions on signal.
	/// </summary>
	public bool SellExit { get => _sellExit.Value; set => _sellExit.Value = value; }
	
	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	
	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	
	/// <summary>
	/// Initialize the strategy parameters.
	/// </summary>
	public OzymandiasStrategy()
	{
	_length = Param(nameof(Length), 2)
	.SetGreaterThanZero()
	.SetDisplay("Length", "Lookback period", "Indicator")
	.SetCanOptimize(true);
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("Candle Type", "Timeframe for candles", "General");
	
	_buyEntry = Param(nameof(BuyEntry), true)
	.SetDisplay("Buy Entry", "Allow opening long positions", "Trading");
	
	_sellEntry = Param(nameof(SellEntry), true)
	.SetDisplay("Sell Entry", "Allow opening short positions", "Trading");
	
	_buyExit = Param(nameof(BuyExit), true)
	.SetDisplay("Buy Exit", "Allow closing long positions", "Trading");
	
	_sellExit = Param(nameof(SellExit), true)
	.SetDisplay("Sell Exit", "Allow closing short positions", "Trading");
	
	_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
	.SetDisplay("Take Profit", "Take profit distance", "Risk");
	
	_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
	.SetDisplay("Stop Loss", "Stop loss distance", "Risk");
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
	
	_trend = 0;
	_nextTrend = 0;
	_maxl = 0m;
	_minh = decimal.MaxValue;
	_baseLine = 0m;
	_prevHigh = 0m;
	_prevLow = 0m;
	_prevDirection = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	_highest = new Highest { Length = Length };
	_lowest = new Lowest { Length = Length };
	_hma = new SimpleMovingAverage { Length = Length };
	_lma = new SimpleMovingAverage { Length = Length };
	_atr = new AverageTrueRange { Length = 100 };
	
	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(ProcessCandle)
	.Start();
	
	StartProtection(new Unit(TakeProfitPoints, UnitTypes.Price), new Unit(StopLossPoints, UnitTypes.Price));
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _highest);
	DrawIndicator(area, _lowest);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	var hh = _highest.Process(candle).ToDecimal();
	var ll = _lowest.Process(candle).ToDecimal();
	var hma = _hma.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
	var lma = _lma.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();
	var atrHalf = _atr.Process(candle).ToDecimal() / 2m;
	
	if (_prevHigh == 0m && _prevLow == 0m)
	{
	_prevHigh = candle.HighPrice;
	_prevLow = candle.LowPrice;
	_baseLine = candle.ClosePrice;
	return;
	}
	
	var trend0 = _trend;
	
	if (_nextTrend == 1)
	{
	_maxl = Math.Max(ll, _maxl);
	if (hma < _maxl && candle.ClosePrice < _prevLow)
	{
	trend0 = 1;
	_nextTrend = 0;
	_minh = hh;
	}
	}
	
	if (_nextTrend == 0)
	{
	_minh = Math.Min(hh, _minh);
	if (lma > _minh && candle.ClosePrice > _prevHigh)
	{
	trend0 = 0;
	_nextTrend = 1;
	_maxl = ll;
	}
	}
	
	int direction;
	if (trend0 == 0)
	{
	_baseLine = _trend != 0 ? _baseLine : Math.Max(_maxl, _baseLine);
	direction = 1;
	}
	else
	{
	_baseLine = _trend != 1 ? _baseLine : Math.Min(_minh, _baseLine);
	direction = 0;
	}
	
	var upLine = _baseLine + atrHalf;
	var downLine = _baseLine - atrHalf;
	
	if (_prevDirection is int prevDir)
	{
	if (direction == 1)
	{
	if (SellExit && Position < 0)
	BuyMarket(Math.Abs(Position));
	if (BuyEntry && prevDir == 0 && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));
	}
	else
	{
	if (BuyExit && Position > 0)
	SellMarket(Position);
	if (SellEntry && prevDir == 1 && Position >= 0)
	SellMarket(Volume + Math.Abs(Position));
	}
	}
	
	_prevDirection = direction;
	_trend = trend0;
	_prevHigh = candle.HighPrice;
	_prevLow = candle.LowPrice;
	}
	}
