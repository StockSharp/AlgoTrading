using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CandlesticksBW strategy based on Bill Williams' color classification of candles.
/// Uses Awesome and Accelerator oscillators to detect momentum shifts.
/// </summary>
public class CandlesticksBwStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private readonly SimpleMovingAverage _aoFast = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlow = new() { Length = 34 };
	private readonly SimpleMovingAverage _acMa = new() { Length = 5 };

	private decimal _prevAo;
	private decimal _prevAc;
	private bool _hasPrev;

	private readonly List<int> _colorHistory = new();

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Number of bars back used for signal generation.
	/// </summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Enable opening of long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }

	/// <summary>
	/// Enable opening of short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	/// <summary>
	/// Enable closing of long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }

	/// <summary>
	/// Enable closing of short positions.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CandlesticksBwStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Offset for signal evaluation", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Allow Long Exit", "Enable closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Allow Short Exit", "Enable closing short positions", "Trading");
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

		StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate median price (HL2)
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;

		var aoFastValue = _aoFast.Process(hl2);
		var aoSlowValue = _aoSlow.Process(hl2);
		if (!aoFastValue.IsFinal || !aoSlowValue.IsFinal)
			return;

		var ao = aoFastValue.GetValue<decimal>() - aoSlowValue.GetValue<decimal>();
		var acMaValue = _acMa.Process(ao);
		if (!acMaValue.IsFinal)
			return;

		var ac = ao - acMaValue.GetValue<decimal>();

		int color;
		if (_hasPrev && ao >= _prevAo && ac >= _prevAc)
			color = candle.OpenPrice <= candle.ClosePrice ? 0 : 1; // Up momentum
		else if (_hasPrev && ao <= _prevAo && ac <= _prevAc)
			color = candle.OpenPrice >= candle.ClosePrice ? 5 : 4; // Down momentum
		else
			color = candle.OpenPrice <= candle.ClosePrice ? 2 : 3; // Transition phase

		_prevAo = ao;
		_prevAc = ac;
		_hasPrev = true;

		_colorHistory.Add(color);

		if (_colorHistory.Count <= SignalBar + 1)
			return;

		var value0 = _colorHistory[^ (SignalBar + 1)];
		var value1 = _colorHistory[^ (SignalBar + 2)];

		if (value1 < 2)
		{
			if (SellPosClose && Position < 0)
				ClosePosition(); // Close short positions
			if (BuyPosOpen && value0 > 1 && Position <= 0)
				BuyMarket(Volume); // Open long position
		}
		else if (value1 > 3)
		{
			if (BuyPosClose && Position > 0)
				ClosePosition(); // Close long positions
			if (SellPosOpen && value0 < 4 && Position >= 0)
				SellMarket(Volume); // Open short position
		}
	}
}
