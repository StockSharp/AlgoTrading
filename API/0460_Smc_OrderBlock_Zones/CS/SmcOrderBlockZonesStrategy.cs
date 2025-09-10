using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMC Order Block Zones strategy.
/// </summary>
public class SmcOrderBlockZonesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<int> _swingHighLength;
	private readonly StrategyParam<int> _swingLowLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _orderBlockLength;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private SimpleMovingAverage _sma;
	private Highest _swingHighIndicator;
	private Lowest _swingLowIndicator;
	private Highest _orderBlockHighIndicator;
	private Lowest _orderBlockLowIndicator;

	private decimal? _swingHigh;
	private decimal? _swingLow;

	/// <summary>
	/// Trade direction.
	/// </summary>
	public enum TradeDirection
	{
		LongOnly,
		ShortOnly,
		Both
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trade direction filter.
	/// </summary>
	public TradeDirection Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Swing high length.
	/// </summary>
	public int SwingHighLength
	{
		get => _swingHighLength.Value;
		set => _swingHighLength.Value = value;
	}

	/// <summary>
	/// Swing low length.
	/// </summary>
	public int SwingLowLength
	{
		get => _swingLowLength.Value;
		set => _swingLowLength.Value = value;
	}

	/// <summary>
	/// SMA length.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Order block length.
	/// </summary>
	public int OrderBlockLength
	{
		get => _orderBlockLength.Value;
		set => _orderBlockLength.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SmcOrderBlockZonesStrategy"/> class.
	/// </summary>
	public SmcOrderBlockZonesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_direction = Param(nameof(Direction), TradeDirection.Both)
		.SetDisplay("Trade Direction", "Allowed trade side", "General");

		_swingHighLength = Param(nameof(SwingHighLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Swing High Length", "Bars for swing high", "Parameters");

		_swingLowLength = Param(nameof(SwingLowLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Swing Low Length", "Bars for swing low", "Parameters");

		_smaLength = Param(nameof(SmaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("SMA Length", "Moving average length", "Parameters");

		_orderBlockLength = Param(nameof(OrderBlockLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Order Block Length", "Bars for order block", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetNotNegative()
		.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");
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

		_sma = new SimpleMovingAverage { Length = SmaLength };
		_swingHighIndicator = new Highest { Length = SwingHighLength };
		_swingLowIndicator = new Lowest { Length = SwingLowLength };
		_orderBlockHighIndicator = new Highest { Length = OrderBlockLength };
		_orderBlockLowIndicator = new Lowest { Length = OrderBlockLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var smaValue = _sma.Process(candle).ToNullableDecimal();
		var swingHighValue = _swingHighIndicator.Process(new DecimalIndicatorValue(_swingHighIndicator, candle.HighPrice)).ToNullableDecimal();
		var swingLowValue = _swingLowIndicator.Process(new DecimalIndicatorValue(_swingLowIndicator, candle.LowPrice)).ToNullableDecimal();
		var orderBlockHigh = _orderBlockHighIndicator.Process(new DecimalIndicatorValue(_orderBlockHighIndicator, candle.HighPrice)).ToNullableDecimal();
		var orderBlockLow = _orderBlockLowIndicator.Process(new DecimalIndicatorValue(_orderBlockLowIndicator, candle.LowPrice)).ToNullableDecimal();

		if (smaValue is not decimal sma ||
		swingHighValue is not decimal swingHighCurr ||
		swingLowValue is not decimal swingLowCurr ||
		orderBlockHigh is not decimal obHigh ||
		orderBlockLow is not decimal obLow)
		return;

		if (candle.HighPrice == swingHighCurr)
		_swingHigh = swingHighCurr;

		if (candle.LowPrice == swingLowCurr)
		_swingLow = swingLowCurr;

		if (_swingHigh is not decimal swingHigh || _swingLow is not decimal swingLow)
		return;

		if (!_sma.IsFormed || !_orderBlockHighIndicator.IsFormed || !_orderBlockLowIndicator.IsFormed || !_swingHighIndicator.IsFormed || !_swingLowIndicator.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var equilibrium = (swingHigh + swingLow) / 2m;
		var premiumZone = swingHigh;
		var discountZone = swingLow;

		var buySignal = candle.ClosePrice < equilibrium && candle.ClosePrice > discountZone && candle.ClosePrice > sma;
		var sellSignal = candle.ClosePrice > equilibrium && candle.ClosePrice < premiumZone && candle.ClosePrice < sma;

		var buySignalOb = buySignal && candle.ClosePrice >= obLow;
		var sellSignalOb = sellSignal && candle.ClosePrice <= obHigh;

		switch (Direction)
		{
			case TradeDirection.LongOnly:
			if (Position > 0 && sellSignalOb)
			SellMarket(Position);
			if (buySignalOb && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			break;

			case TradeDirection.ShortOnly:
			if (Position < 0 && buySignalOb)
			BuyMarket(-Position);
			if (sellSignalOb && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
			break;

			case TradeDirection.Both:
			if (buySignalOb && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			if (sellSignalOb && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
			break;
		}
	}
}

