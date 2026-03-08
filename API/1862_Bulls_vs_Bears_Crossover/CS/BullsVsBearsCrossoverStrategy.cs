using System;
using System.Collections.Generic;

using Ecng.Common;

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
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypes
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

	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _openLong;
	private readonly StrategyParam<bool> _openShort;
	private readonly StrategyParam<bool> _closeLong;
	private readonly StrategyParam<bool> _closeShort;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minSpreadSteps;
	private readonly StrategyParam<int> _cooldownBars;

	private IIndicator _ma = null!;
	private decimal _prevBull;
	private decimal _prevBear;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public bool OpenLong
	{
		get => _openLong.Value;
		set => _openLong.Value = value;
	}

	public bool OpenShort
	{
		get => _openShort.Value;
		set => _openShort.Value = value;
	}

	public bool CloseLong
	{
		get => _closeLong.Value;
		set => _closeLong.Value = value;
	}

	public bool CloseShort
	{
		get => _closeShort.Value;
		set => _closeShort.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal MinSpreadSteps
	{
		get => _minSpreadSteps.Value;
		set => _minSpreadSteps.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public BullsVsBearsCrossoverStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageTypes.SMA)
			.SetDisplay("MA Type", "Moving average type", "General");

		_maLength = Param(nameof(MaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Loss in price steps", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Profit in price steps", "Risk");

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

		_minSpreadSteps = Param(nameof(MinSpreadSteps), 60m)
			.SetDisplay("Minimum Spread", "Minimum spread between bull and bear power in price steps", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_ma = null!;
		_prevBull = 0m;
		_prevBear = 0m;
		_entryPrice = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		StartProtection(null, null);
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

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var spread = Math.Abs(bull - bear);
		var crossDown = _prevBull > _prevBear && bull <= bear && spread >= MinSpreadSteps;
		var crossUp = _prevBull < _prevBear && bull >= bear && spread >= MinSpreadSteps;

		if (_cooldownRemaining == 0)
		{
			if (crossDown)
			{
				if (CloseShort && Position < 0)
					BuyMarket();

				if (OpenLong && Position <= 0)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
					_cooldownRemaining = CooldownBars;
				}
			}
			else if (crossUp)
			{
				if (CloseLong && Position > 0)
					SellMarket();

				if (OpenShort && Position >= 0)
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
					_cooldownRemaining = CooldownBars;
				}
			}
		}

		if (Position > 0)
		{
			var tp = _entryPrice + TakeProfit * step;
			var sl = _entryPrice - StopLoss * step;
			if (candle.ClosePrice >= tp || candle.ClosePrice <= sl)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			var tp = _entryPrice - TakeProfit * step;
			var sl = _entryPrice + StopLoss * step;
			if (candle.ClosePrice <= tp || candle.ClosePrice >= sl)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevBull = bull;
		_prevBear = bear;
	}

	private static IIndicator CreateMovingAverage(MovingAverageTypes type, int length)
	{
		return type switch
		{
			MovingAverageTypes.SMA => new SimpleMovingAverage { Length = length },
			MovingAverageTypes.EMA => new ExponentialMovingAverage { Length = length },
			MovingAverageTypes.SMMA => new SmoothedMovingAverage { Length = length },
			MovingAverageTypes.WMA => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
}
