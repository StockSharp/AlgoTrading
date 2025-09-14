using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossing of two smoothed RSI lines that approximate the ColorZerolagJJRSX oscillator.
/// A downward cross from the fast line to the slow line opens a long position and closes shorts.
/// An upward cross opens a short position and closes longs.
/// </summary>
public class ColorZerolagJjrsxStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex? _fastRsi;
	private RelativeStrengthIndex? _slowRsi;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ColorZerolagJjrsxStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast JJRSX period", "Indicator")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow JJRSX period", "Indicator")
			.SetCanOptimize(true);

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Longs", "Close long positions on opposite signal", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Shorts", "Close short positions on opposite signal", "Trading");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator", "General");
	}

	/// <summary>
	/// Fast oscillator period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow oscillator period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Close long positions on opposite signal.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Close short positions on opposite signal.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastRsi = new RelativeStrengthIndex { Length = FastPeriod };
		_slowRsi = new RelativeStrengthIndex { Length = SlowPeriod };

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Price),
			stopLoss: new Unit(StopLoss, UnitTypes.Price));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastRsi, _slowRsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastRsi);
			DrawIndicator(area, _slowRsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossDown = _prevFast > _prevSlow && fast < slow;
		var crossUp = _prevFast < _prevSlow && fast > slow;

		if (crossDown)
		{
			if (SellClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (BuyOpen && Position <= 0)
				BuyMarket(Volume);
		}
		else if (crossUp)
		{
			if (BuyClose && Position > 0)
				SellMarket(Math.Abs(Position));

			if (SellOpen && Position >= 0)
				SellMarket(Volume);
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}

