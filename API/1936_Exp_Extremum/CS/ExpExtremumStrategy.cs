using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Extremum reversal strategy using highest/lowest comparison.
/// Opens trades when the sign of price extremes changes.
/// </summary>
public class ExpExtremumStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private Lowest _minHigh = null!;
	private Highest _maxLow = null!;

	private bool _upPrev1;
	private bool _dnPrev1;
	private bool _upPrev2;
	private bool _dnPrev2;

	/// <summary>
	/// Indicator period.
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
	/// Initializes a new instance of the <see cref="ExpExtremumStrategy"/> class.
	/// </summary>
	public ExpExtremumStrategy()
	{
		_length = Param(nameof(Length), 20)
		.SetGreaterThanZero()
		.SetDisplay("Period", "Indicator period", "General")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame of the Extremum indicator", "General");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Buy Entry", "Permission to buy", "Signals");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Sell Entry", "Permission to sell", "Signals");

		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Close Long", "Permission to exit long positions", "Signals");

		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Close Short", "Permission to exit short positions", "Signals");
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

		_minHigh = new Lowest { Length = Length };
		_maxLow = new Highest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var minHighVal = _minHigh.Process(new DecimalIndicatorValue(_minHigh, candle.HighPrice, candle.OpenTime));
		var maxLowVal = _maxLow.Process(new DecimalIndicatorValue(_maxLow, candle.LowPrice, candle.OpenTime));

		if (!minHighVal.IsFinal || !maxLowVal.IsFinal)
		return;

		var minHigh = ((DecimalIndicatorValue)minHighVal).Value;
		var maxLow = ((DecimalIndicatorValue)maxLowVal).Value;

		var n = candle.HighPrice - minHigh;
		var m = candle.LowPrice - maxLow;
		var sum = n + m;

		var up = sum > 0m;
		var dn = sum < 0m;

		if (BuyPosClose && _dnPrev2 && Position > 0)
		ClosePosition();

		if (SellPosClose && _upPrev2 && Position < 0)
		ClosePosition();

		if (BuyPosOpen && _upPrev2 && _dnPrev1 && Position <= 0)
		BuyMarket();

		if (SellPosOpen && _dnPrev2 && _upPrev1 && Position >= 0)
		SellMarket();

		_upPrev2 = _upPrev1;
		_dnPrev2 = _dnPrev1;
		_upPrev1 = up;
		_dnPrev1 = dn;
	}
}
