using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple trading system based on shifted moving average and price comparisons.
/// Generates buy and sell signals when the current close meets several conditions
/// relative to previous closes and the moving average.
/// </summary>
public class SimpleTradingSystemStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<PriceTypeEnum> _priceType;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _volume;

	private IIndicator? _ma;
	private decimal[] _maBuffer = Array.Empty<decimal>();
	private decimal[] _closeBuffer = Array.Empty<decimal>();
	private int _sign;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Shift applied when comparing previous values.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Price type for the moving average.
	/// </summary>
	public PriceTypeEnum PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
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
	public bool BuyPositionOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPositionOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on sell signal.
	/// </summary>
	public bool BuyPositionClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on buy signal.
	/// </summary>
	public bool SellPositionClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Take profit in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="SimpleTradingSystemStrategy"/>.
	/// </summary>
	public SimpleTradingSystemStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageTypeEnum.EMA)
			.SetDisplay("MA Type", "Moving average type", "Parameters");

		_maPeriod = Param(nameof(MaPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Parameters")
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 4)
			.SetGreaterOrEqual(0)
			.SetDisplay("MA Shift", "Shift for comparisons", "Parameters")
			.SetCanOptimize(true);

		_priceType = Param(nameof(PriceType), PriceTypeEnum.Close)
			.SetDisplay("Price Type", "Source price for MA", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_buyOpen = Param(nameof(BuyPositionOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellPositionOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyPositionClose), true)
			.SetDisplay("Buy Close", "Allow closing longs on sell signal", "Trading");

		_sellClose = Param(nameof(SellPositionClose), true)
			.SetDisplay("Sell Close", "Allow closing shorts on buy signal", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
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

		_ma = null;
		_maBuffer = Array.Empty<decimal>();
		_closeBuffer = Array.Empty<decimal>();
		_sign = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMa(MaType, MaPeriod);

		_maBuffer = new decimal[MaShift + 1];
		_closeBuffer = new decimal[MaPeriod + MaShift + 1];

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = GetPrice(candle);
		var maValue = _ma!.Process(price, candle.OpenTime, true).ToDecimal();

		Shift(_maBuffer, maValue);
		Shift(_closeBuffer, candle.ClosePrice);

		if (_closeBuffer[0] == 0m)
			return; // not enough history yet

		var ma0 = _maBuffer[^1];
		var ma1 = _maBuffer[^1 - MaShift];

		var close = _closeBuffer[^1];
		var closeShift = _closeBuffer[^1 - MaShift];
		var closeSum = _closeBuffer[0];
		var open = candle.OpenPrice;

		var buySignal = _sign < 1 && ma0 <= ma1 && close >= closeShift && close <= closeSum && close < open;
		var sellSignal = _sign > -1 && ma0 >= ma1 && close <= closeShift && close >= closeSum && close > open;

		if (buySignal)
		{
			if (SellPositionClose && Position < 0)
				BuyMarket(Math.Abs(Position));
			if (BuyPositionOpen && IsFormedAndOnlineAndAllowTrading())
				BuyMarket(Volume);
			_sign = 1;
		}
		else if (sellSignal)
		{
			if (BuyPositionClose && Position > 0)
				SellMarket(Math.Abs(Position));
			if (SellPositionOpen && IsFormedAndOnlineAndAllowTrading())
				SellMarket(Volume);
			_sign = -1;
		}
	}

	private static void Shift(decimal[] array, decimal value)
	{
		for (var i = 0; i < array.Length - 1; i++)
			array[i] = array[i + 1];
		array[^1] = value;
	}

	private decimal GetPrice(ICandleMessage candle)
	{
		return PriceType switch
		{
			PriceTypeEnum.Close => candle.ClosePrice,
			PriceTypeEnum.High => candle.HighPrice,
			PriceTypeEnum.Open => candle.OpenPrice,
			PriceTypeEnum.Low => candle.LowPrice,
			PriceTypeEnum.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceTypeEnum.Center => (candle.HighPrice + candle.LowPrice) / 2m,
			_ => candle.ClosePrice
		};
	}

	private static IIndicator CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.SMA => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.EMA => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.DEMA => new DoubleExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.TEMA => new TripleExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.WMA => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.VWMA => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}
}

public enum MovingAverageTypeEnum
{
	SMA,
	EMA,
	DEMA,
	TEMA,
	WMA,
	VWMA
}

public enum PriceTypeEnum
{
	Close,
	High,
	Open,
	Low,
	Typical,
	Center
}
