using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple moving average based strategy inspired by DigVariation indicator.
/// Opens a position when the moving average trend reverses.
/// </summary>
public class DigVariationStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal _prev;
	private decimal _prevPrev;
	private bool _initialized;

	/// <summary>
	/// Initializes a new instance of the <see cref="DigVariationStrategy"/> class.
	/// </summary>
	public DigVariationStrategy()
	{
		_period = this.Param("Period", 12).SetDisplay("Period").SetCanOptimize();
		_buyOpen = this.Param("BuyOpen", true).SetDisplay("Open Long");
		_sellOpen = this.Param("SellOpen", true).SetDisplay("Open Short");
		_buyClose = this.Param("BuyClose", true).SetDisplay("Close Long");
		_sellClose = this.Param("SellClose", true).SetDisplay("Close Short");
		_stopLoss = this.Param("StopLoss", 1000m).SetDisplay("Stop Loss").SetCanOptimize();
		_takeProfit = this.Param("TakeProfit", 2000m).SetDisplay("Take Profit").SetCanOptimize();
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow long exits.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow short exits.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Stop loss value.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit value.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(StopLoss, TakeProfit);

		var sma = new SimpleMovingAverage { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prev = smaValue;
			_prevPrev = smaValue;
			_initialized = true;
			return;
		}

		var wasRising = _prev > _prevPrev;
		var wasFalling = _prev < _prevPrev;

		if (wasRising)
		{
			if (SellClose && Position < 0)
				BuyMarket();

			if (BuyOpen && smaValue > _prev)
				BuyMarket();
		}

		if (wasFalling)
		{
			if (BuyClose && Position > 0)
				SellMarket();

			if (SellOpen && smaValue < _prev)
				SellMarket();
		}

		_prevPrev = _prev;
		_prev = smaValue;
	}
}

