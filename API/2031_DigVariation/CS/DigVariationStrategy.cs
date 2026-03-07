using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
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
	private int _cooldown;

	/// <summary>
	/// Initializes a new instance of the <see cref="DigVariationStrategy"/> class.
	/// </summary>
	public DigVariationStrategy()
	{
		_period = this.Param("Period", 20).SetDisplay("Period", "Period", "General");
		_buyOpen = this.Param("BuyOpen", true).SetDisplay("Open Long", "Open Long", "General");
		_sellOpen = this.Param("SellOpen", true).SetDisplay("Open Short", "Open Short", "General");
		_buyClose = this.Param("BuyClose", true).SetDisplay("Close Long", "Close Long", "General");
		_sellClose = this.Param("SellClose", true).SetDisplay("Close Short", "Close Short", "General");
		_stopLoss = this.Param("StopLoss", 1000m).SetDisplay("Stop Loss", "Stop Loss", "General");
		_takeProfit = this.Param("TakeProfit", 2000m).SetDisplay("Take Profit", "Take Profit", "General");
		_candleType2 = this.Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle", "General");
		_cooldownPeriod = this.Param(nameof(CooldownPeriod), 200).SetDisplay("Cooldown", "Cooldown between trades in candles", "General");
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

	private readonly StrategyParam<DataType> _candleType2;
	private readonly StrategyParam<int> _cooldownPeriod;

	public DataType CandleType { get => _candleType2.Value; set => _candleType2.Value = value; }

	/// <summary>
	/// Cooldown period between trades in candles.
	/// </summary>
	public int CooldownPeriod { get => _cooldownPeriod.Value; set => _cooldownPeriod.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev = default;
		_prevPrev = default;
		_initialized = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(new Unit(StopLoss, UnitTypes.Absolute), new Unit(TakeProfit, UnitTypes.Absolute));

		var ema = new ExponentialMovingAverage { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
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

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrev = _prev;
			_prev = smaValue;
			return;
		}

		var wasRising = _prev > _prevPrev;
		var wasFalling = _prev < _prevPrev;

		if (wasRising)
		{
			if (SellClose && Position < 0)
			{
				BuyMarket();
				_cooldown = CooldownPeriod;
			}
			else if (BuyOpen && Position <= 0 && smaValue > _prev)
			{
				BuyMarket();
				_cooldown = CooldownPeriod;
			}
		}

		if (wasFalling)
		{
			if (BuyClose && Position > 0)
			{
				SellMarket();
				_cooldown = CooldownPeriod;
			}
			else if (SellOpen && Position >= 0 && smaValue < _prev)
			{
				SellMarket();
				_cooldown = CooldownPeriod;
			}
		}

		_prevPrev = _prev;
		_prev = smaValue;
	}
}
