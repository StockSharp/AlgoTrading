using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Schaff DeMarker Trend Cycle oscillator.
/// The algorithm opens long positions when the oscillator leaves the upper zone
/// and closes shorts. It opens short positions when the oscillator leaves the
/// lower zone and closes longs.
/// </summary>
public class ColorSchaffDeMarkerTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastDeMarker;
	private readonly StrategyParam<int> _slowDeMarker;
	private readonly StrategyParam<int> _cycle;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	
	private Highest _macdHigh = null!;
	private Lowest _macdLow = null!;
	private Highest _stHigh = null!;
	private Lowest _stLow = null!;
	
	private decimal _prevSt;
	private decimal _prevStc;
	private bool _st1Pass;
	private bool _st2Pass;
	private int _prevColor;
	
	private const decimal Factor = 0.5m;
	
	/// <summary>
	/// Fast DeMarker period.
	/// </summary>
	public int FastDeMarker
	{
		get => _fastDeMarker.Value;
		set => _fastDeMarker.Value = value;
	}
	
	/// <summary>
	/// Slow DeMarker period.
	/// </summary>
	public int SlowDeMarker
	{
		get => _slowDeMarker.Value;
		set => _slowDeMarker.Value = value;
	}
	
	/// <summary>
	/// Cycle length for oscillator calculations.
	/// </summary>
	public int Cycle
	{
		get => _cycle.Value;
		set => _cycle.Value = value;
	}
	
	/// <summary>
	/// Upper threshold.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}
	
	/// <summary>
	/// Lower threshold.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}
	
	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}
	
	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}
	
	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ColorSchaffDeMarkerTrendCycleStrategy"/> class.
	/// </summary>
	public ColorSchaffDeMarkerTrendCycleStrategy()
	{
		_fastDeMarker = Param(nameof(FastDeMarker), 23)
		.SetGreaterThanZero()
		.SetDisplay("Fast DeMarker", "Fast DeMarker period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);
		
		_slowDeMarker = Param(nameof(SlowDeMarker), 50)
		.SetGreaterThanZero()
		.SetDisplay("Slow DeMarker", "Slow DeMarker period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 100, 10);
		
		_cycle = Param(nameof(Cycle), 10)
		.SetGreaterThanZero()
		.SetDisplay("Cycle", "Cycle length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_highLevel = Param(nameof(HighLevel), 60m)
		.SetDisplay("High Level", "Upper threshold", "Levels");
		
		_lowLevel = Param(nameof(LowLevel), -60m)
		.SetDisplay("Low Level", "Lower threshold", "Levels");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Open Long", "Allow long entries", "Trading");
		
		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Open Short", "Allow short entries", "Trading");
		
		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Close Long", "Allow closing longs", "Trading");
		
		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Close Short", "Allow closing shorts", "Trading");
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
		_prevSt = 0m;
		_prevStc = 0m;
		_st1Pass = false;
		_st2Pass = false;
		_prevColor = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fast = new DeMarker { Length = FastDeMarker };
		var slow = new DeMarker { Length = SlowDeMarker };
		
		_macdHigh = new Highest { Length = Cycle };
		_macdLow = new Lowest { Length = Cycle };
		_stHigh = new Highest { Length = Cycle };
		_stLow = new Lowest { Length = Cycle };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(fast, slow, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var macd = fastValue - slowValue;
		var macdHigh = _macdHigh.Process(macd);
		var macdLow = _macdLow.Process(macd);
		if (!_macdHigh.IsFormed || !_macdLow.IsFormed)
		return;
		
		decimal st;
		if (macdHigh == macdLow)
		st = _prevSt;
		else
		st = (macd - macdLow) / (macdHigh - macdLow) * 100m;
		
		if (_st1Pass)
		st = Factor * (st - _prevSt) + _prevSt;
		_prevSt = st;
		_st1Pass = true;
		
		var stHigh = _stHigh.Process(st);
		var stLow = _stLow.Process(st);
		if (!_stHigh.IsFormed || !_stLow.IsFormed)
		return;
		
		decimal stc;
		if (stHigh == stLow)
		stc = _prevStc;
		else
		stc = (st - stLow) / (stHigh - stLow) * 200m - 100m;
		
		if (_st2Pass)
		stc = Factor * (stc - _prevStc) + _prevStc;
		
		var dStc = stc - _prevStc;
		_prevStc = stc;
		_st2Pass = true;
		
		int color;
		if (stc > 0)
		{
			if (stc > HighLevel)
			color = dStc >= 0 ? 7 : 6;
			else
			color = dStc >= 0 ? 5 : 4;
		}
		else
		{
			if (stc < LowLevel)
			color = dStc < 0 ? 0 : 1;
			else
			color = dStc < 0 ? 2 : 3;
		}
		
		if (_prevColor > 5)
		{
			if (SellPosClose && Position < 0)
			BuyMarket(Math.Abs(Position));
			if (BuyPosOpen && color < 6 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		}
		
		if (_prevColor < 2)
		{
			if (BuyPosClose && Position > 0)
			SellMarket(Math.Abs(Position));
			if (SellPosOpen && color > 1 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}
		
		_prevColor = color;
	}
}
