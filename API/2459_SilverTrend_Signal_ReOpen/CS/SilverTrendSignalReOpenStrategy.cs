namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy using SilverTrend indicator with optional position re-opening.
/// </summary>
public class SilverTrendSignalReOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ssp;
	private readonly StrategyParam<int> _risk;
	private readonly StrategyParam<decimal> _priceStep;
	private readonly StrategyParam<int> _posTotal;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private decimal _entryPrice;
	private decimal _lastReopenPrice;
	private int _positionsCount;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// SilverTrend SSP parameter.
	/// </summary>
	public int Ssp { get => _ssp.Value; set => _ssp.Value = value; }

	/// <summary>
	/// SilverTrend risk parameter.
	/// </summary>
	public int Risk { get => _risk.Value; set => _risk.Value = value; }

	/// <summary>
	/// Price step for re-opening additional positions.
	/// </summary>
	public decimal PriceStep { get => _priceStep.Value; set => _priceStep.Value = value; }

	/// <summary>
	/// Maximum number of positions in one direction.
	/// </summary>
	public int PosTotal { get => _posTotal.Value; set => _posTotal.Value = value; }

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Allow opening buy positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }

	/// <summary>
	/// Allow opening sell positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	/// <summary>
	/// Allow closing buy positions on opposite signal.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }

	/// <summary>
	/// Allow closing sell positions on opposite signal.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	public SilverTrendSignalReOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_ssp = Param(nameof(Ssp), 9)
			.SetGreaterThanZero()
			.SetDisplay("SSP", "SilverTrend SSP parameter", "Indicators");

		_risk = Param(nameof(Risk), 3)
			.SetGreaterThanZero()
			.SetDisplay("Risk", "SilverTrend risk parameter", "Indicators");

		_priceStep = Param(nameof(PriceStep), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Price Step", "Distance to add position", "Trading");

		_posTotal = Param(nameof(PosTotal), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of positions", "Trading");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance", "Trading");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Open Buys", "Allow opening buy positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Open Sells", "Allow opening sell positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Buys", "Close buys on opposite signal", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Sells", "Close sells on opposite signal", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var indicator = new SilverTrendSignalIndicator
		{
			Ssp = Ssp,
			Risk = Risk
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(indicator, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;

		var st = (SilverTrendSignalValue)value;
		var buySignal = st.Buy != 0m;
		var sellSignal = st.Sell != 0m;

		if (Position > 0)
		{
			var stopPrice = _entryPrice - StopLoss;
			var takePrice = _entryPrice + TakeProfit;

			if ((sellSignal && SellPosClose) || candle.LowPrice <= stopPrice || candle.HighPrice >= takePrice)
			{
				ClosePosition();
				_positionsCount = 0;
				_entryPrice = 0m;
				_lastReopenPrice = 0m;
			}
			else if (PriceStep > 0m && candle.ClosePrice - _lastReopenPrice >= PriceStep && _positionsCount < PosTotal)
			{
				BuyMarket();
				_lastReopenPrice = candle.ClosePrice;
				_positionsCount++;
			}
		}
		else if (Position < 0)
		{
			var stopPrice = _entryPrice + StopLoss;
			var takePrice = _entryPrice - TakeProfit;

			if ((buySignal && BuyPosClose) || candle.HighPrice >= stopPrice || candle.LowPrice <= takePrice)
			{
				ClosePosition();
				_positionsCount = 0;
				_entryPrice = 0m;
				_lastReopenPrice = 0m;
			}
			else if (PriceStep > 0m && _lastReopenPrice - candle.ClosePrice >= PriceStep && _positionsCount < PosTotal)
			{
				SellMarket();
				_lastReopenPrice = candle.ClosePrice;
				_positionsCount++;
			}
		}

		if (Position == 0)
		{
			if (buySignal && BuyPosOpen)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_lastReopenPrice = _entryPrice;
				_positionsCount = 1;
			}
			else if (sellSignal && SellPosOpen)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_lastReopenPrice = _entryPrice;
				_positionsCount = 1;
			}
		}
	}
}

/// <summary>
/// SilverTrend indicator returning buy and sell signals.
/// </summary>
public class SilverTrendSignalIndicator : BaseIndicator<decimal>
{
	public int Ssp { get; set; } = 9;
	public int Risk { get; set; } = 3;

	private bool _old;
	private bool _uptrend;

	private readonly List<decimal> _high = new();
	private readonly List<decimal> _low = new();
	private readonly List<decimal> _close = new();

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new SilverTrendSignalValue(this, input, 0m, 0m);

		_high.Insert(0, candle.HighPrice);
		_low.Insert(0, candle.LowPrice);
		_close.Insert(0, candle.ClosePrice);

		var len = Ssp + 1;
		if (_high.Count < len)
			return new SilverTrendSignalValue(this, input, 0m, 0m);

		var k = 33 - Risk;

		decimal avgRange = 0m;
		for (var i = 0; i < len; i++)
			avgRange += Math.Abs(_high[i] - _low[i]);

		var range = avgRange / len;

		var ssMax = _low[0];
		var ssMin = _close[0];
		for (var i = 0; i < Ssp; i++)
		{
			var priceHigh = _high[i];
			if (ssMax < priceHigh)
				ssMax = priceHigh;

			var priceLow = _low[i];
			if (ssMin >= priceLow)
				ssMin = priceLow;
		}

		var smin = ssMin + (ssMax - ssMin) * k / 100m;
		var smax = ssMax - (ssMax - ssMin) * k / 100m;

		var uptrend = _uptrend;
		if (candle.ClosePrice < smin)
			uptrend = false;
		if (candle.ClosePrice > smax)
			uptrend = true;

		decimal buy = 0m, sell = 0m;
		if (uptrend != _old)
		{
			if (uptrend)
				buy = candle.LowPrice - range * 0.5m;
			else
				sell = candle.HighPrice + range * 0.5m;
		}

		_old = uptrend;
		_uptrend = uptrend;

		return new SilverTrendSignalValue(this, input, buy, sell);
	}
}

/// <summary>
/// Indicator value for <see cref="SilverTrendSignalIndicator"/>.
/// </summary>
public class SilverTrendSignalValue : ComplexIndicatorValue
{
	public SilverTrendSignalValue(IIndicator indicator, IIndicatorValue input, decimal buy, decimal sell)
		: base(indicator, input, (nameof(Buy), buy), (nameof(Sell), sell))
	{
	}

	/// <summary>
	/// Buy signal level.
	/// </summary>
	public decimal Buy => (decimal)GetValue(nameof(Buy));

	/// <summary>
	/// Sell signal level.
	/// </summary>
	public decimal Sell => (decimal)GetValue(nameof(Sell));
}
