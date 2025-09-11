using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMC strategy using swing zones and SMA trend filter.
/// </summary>
public class SmcStrategy : Strategy
{
	private readonly StrategyParam<int> _swingHighLength;
	private readonly StrategyParam<int> _swingLowLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _orderBlockLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _swingHighIndicator;
	private Lowest _swingLowIndicator;
	private SimpleMovingAverage _sma;
	private Highest _orderBlockHighIndicator;
	private Lowest _orderBlockLowIndicator;

	private decimal? _swingHigh;
	private decimal? _swingLow;

	public int SwingHighLength { get => _swingHighLength.Value; set => _swingHighLength.Value = value; }
	public int SwingLowLength { get => _swingLowLength.Value; set => _swingLowLength.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int OrderBlockLength { get => _orderBlockLength.Value; set => _orderBlockLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SmcStrategy()
	{
		_swingHighLength = Param(nameof(SwingHighLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Swing High Length", "Period to detect swing highs", "General")
			.SetCanOptimize(true);

		_swingLowLength = Param(nameof(SwingLowLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Swing Low Length", "Period to detect swing lows", "General")
			.SetCanOptimize(true);

		_smaLength = Param(nameof(SmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length for trend SMA", "General")
			.SetCanOptimize(true);

		_orderBlockLength = Param(nameof(OrderBlockLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Order Block Length", "Lookback for order blocks", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_swingHighIndicator = default;
		_swingLowIndicator = default;
		_sma = default;
		_orderBlockHighIndicator = default;
		_orderBlockLowIndicator = default;
		_swingHigh = default;
		_swingLow = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_swingHighIndicator = new Highest { Length = SwingHighLength };
		_swingLowIndicator = new Lowest { Length = SwingLowLength };
		_sma = new SimpleMovingAverage { Length = SmaLength };
		_orderBlockHighIndicator = new Highest { Length = OrderBlockLength };
		_orderBlockLowIndicator = new Lowest { Length = OrderBlockLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_swingHighIndicator, _swingLowIndicator, _sma, _orderBlockHighIndicator, _orderBlockLowIndicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _swingHighIndicator);
			DrawIndicator(area, _swingLowIndicator);
			DrawIndicator(area, _orderBlockHighIndicator);
			DrawIndicator(area, _orderBlockLowIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal swingHighValue, decimal swingLowValue, decimal smaValue, decimal orderBlockHigh, decimal orderBlockLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.HighPrice == swingHighValue)
			_swingHigh = swingHighValue;

		if (candle.LowPrice == swingLowValue)
			_swingLow = swingLowValue;

		if (_swingHigh is null || _swingLow is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var equilibrium = (_swingHigh.Value + _swingLow.Value) / 2m;
		var premiumZone = _swingHigh.Value;
		var discountZone = _swingLow.Value;

		var buySignal = candle.ClosePrice < equilibrium && candle.ClosePrice > discountZone && candle.ClosePrice > smaValue;
		var sellSignal = candle.ClosePrice > equilibrium && candle.ClosePrice < premiumZone && candle.ClosePrice < smaValue;

		var buySignalOb = buySignal && candle.ClosePrice >= orderBlockLow;
		var sellSignalOb = sellSignal && candle.ClosePrice <= orderBlockHigh;

		if (buySignalOb && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (sellSignalOb && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}
	}
}
