using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on XMA Candles indicator.
/// Opens a long position when smoothed candles turn bullish and
/// opens a short position when smoothed candles turn bearish.
/// </summary>
public class XMACandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private ExponentialMovingAverage _openMa;
	private ExponentialMovingAverage _closeMa;
	private int _prevColor = -1;

	/// <summary>
	/// Length of smoothing for moving averages.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allows opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allows opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Allows closing long positions.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allows closing short positions.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="XMACandlesStrategy"/>.
	/// </summary>
	public XMACandlesStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetDisplay("Length", "Smoothing length", "Parameters")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Parameters");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Parameters");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Parameters");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetDisplay("Stop Loss %", "Stop loss in percent", "Protection");

		_takeProfit = Param(nameof(TakeProfit), 4m)
			.SetDisplay("Take Profit %", "Take profit in percent", "Protection");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_openMa = new ExponentialMovingAverage { Length = Length };
		_closeMa = new ExponentialMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Percent),
			takeProfit: new Unit(TakeProfit, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		var openValue = _openMa.Process(candle.OpenPrice);
		var closeValue = _closeMa.Process(candle.ClosePrice);

		if (!openValue.IsFinal || !closeValue.IsFinal)
			return;

		var openMa = openValue.GetValue<decimal>();
		var closeMa = closeValue.GetValue<decimal>();

		// determine candle color based on smoothed values
		var currentColor = openMa < closeMa ? 2 : openMa > closeMa ? 0 : 1;

		if (currentColor == 2 && _prevColor != 2)
		{
			// bullish change: close shorts and open long
			if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));
			if (BuyPosOpen && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (currentColor == 0 && _prevColor != 0)
		{
			// bearish change: close longs and open short
			if (BuyPosClose && Position > 0)
				SellMarket(Math.Abs(Position));
			if (SellPosOpen && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevColor = currentColor;
	}
}
