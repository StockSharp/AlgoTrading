using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bulls vs Bears crossover strategy.
/// Opens long or short positions when the distance from high and low to a moving average crosses.
/// </summary>
public class BullsVsBearsCrossoverStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _openLong;
	private readonly StrategyParam<bool> _openShort;
	private readonly StrategyParam<bool> _closeLong;
	private readonly StrategyParam<bool> _closeShort;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _ma = null!;
	private decimal _prevBull;
	private decimal _prevBear;
	private decimal _entryPrice;

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool OpenLong
	{
		get => _openLong.Value;
		set => _openLong.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool OpenShort
	{
		get => _openShort.Value;
		set => _openShort.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool CloseLong
	{
		get => _closeLong.Value;
		set => _closeLong.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool CloseShort
	{
		get => _closeShort.Value;
		set => _closeShort.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BullsVsBearsCrossoverStrategy"/>.
	/// </summary>
	public BullsVsBearsCrossoverStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageTypeEnum.SMA)
			.SetDisplay("MA Type", "Moving average type", "General")
			.SetCanOptimize(true);

		_maLength = Param(nameof(MaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "General")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Loss in price steps", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Profit in price steps", "Risk")
			.SetCanOptimize(true);

		_openLong = Param(nameof(OpenLong), true)
			.SetDisplay("Open Long", "Allow long entries", "General");

		_openShort = Param(nameof(OpenShort), true)
			.SetDisplay("Open Short", "Allow short entries", "General");

		_closeLong = Param(nameof(CloseLong), true)
			.SetDisplay("Close Long", "Allow closing long positions", "General");

		_closeShort = Param(nameof(CloseShort), true)
			.SetDisplay("Close Short", "Allow closing short positions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe to process", "General");
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
		_prevBull = 0m;
		_prevBear = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMovingAverage(MaType, MaLength);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		var step = Security.PriceStep ?? 1m;
		var bull = (candle.HighPrice - maValue) / step;
		var bear = (maValue - candle.LowPrice) / step;

		if (candle.State != CandleStates.Finished || !_ma.IsFormed)
		{
			_prevBull = bull;
			_prevBear = bear;
			return;
		}

		var crossDown = _prevBull > _prevBear && bull <= bear;
		var crossUp = _prevBull < _prevBear && bull >= bear;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevBull = bull;
			_prevBear = bear;
			return;
		}

		if (crossDown)
		{
			if (CloseShort && Position < 0)
				BuyMarket(-Position);

			if (OpenLong && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (crossUp)
		{
			if (CloseLong && Position > 0)
				SellMarket(Position);

			if (OpenShort && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (Position > 0)
		{
			var tp = _entryPrice + TakeProfit * step;
			var sl = _entryPrice - StopLoss * step;
			if (candle.ClosePrice >= tp || candle.ClosePrice <= sl)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var tp = _entryPrice - TakeProfit * step;
			var sl = _entryPrice + StopLoss * step;
			if (candle.ClosePrice <= tp || candle.ClosePrice >= sl)
				BuyMarket(-Position);
		}

		_prevBull = bull;
		_prevBear = bear;
	}

	private static IIndicator CreateMovingAverage(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.SMA => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.EMA => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.SMMA => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.WMA => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
}

/// <summary>
/// Moving average types.
/// </summary>
public enum MovingAverageTypeEnum
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	SMA,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	EMA,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	SMMA,

	/// <summary>
	/// Weighted moving average.
	/// </summary>
	WMA
}
