using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual MACD confirmation strategy converted from the original AG.mq4 expert.
/// The first MACD pair acts as the trigger, while the second (slower) pair filters entries and exits.
/// </summary>
public class AgMacdDualStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _signalSmaLength;
	private readonly StrategyParam<int> _maxOpenOrders;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _primaryMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _secondaryMacd = null!;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AgMacdDualStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base lot size used for market orders", "Execution");

		_fastEmaLength = Param(nameof(FastEmaLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length of the primary MACD", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length of the primary MACD", "Indicators");

		_signalSmaLength = Param(nameof(SignalSmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Signal SMA", "Signal smoothing length for both MACD calculations", "Indicators");

		_maxOpenOrders = Param(nameof(MaxOpenOrders), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Max Open Orders", "Maximum number of simultaneous orders and positions (0 disables the limit)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to build the MACD inputs", "Data");
	}

	/// <summary>
	/// Target volume for entries and scaling.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by the primary MACD.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by the primary MACD.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Signal SMA length shared by both MACD calculations.
	/// </summary>
	public int SignalSmaLength
	{
		get => _signalSmaLength.Value;
		set => _signalSmaLength.Value = value;
	}

	/// <summary>
	/// Maximum number of open orders (0 means unlimited).
	/// </summary>
	public int MaxOpenOrders
	{
		get => _maxOpenOrders.Value;
		set => _maxOpenOrders.Value = value;
	}

	/// <summary>
	/// Candle type supplying the MACD indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_primaryMacd = null!;
		_secondaryMacd = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_primaryMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = new ExponentialMovingAverage { Length = FastEmaLength },
				LongMa = new ExponentialMovingAverage { Length = SlowEmaLength }
			},
			SignalMa = new ExponentialMovingAverage { Length = SignalSmaLength }
		};

		var secondaryFast = Math.Max(1, SlowEmaLength * 2);
		var secondarySlow = Math.Max(1, FastEmaLength * 2);
		var secondarySignal = Math.Max(1, SignalSmaLength * 2);

		_secondaryMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = new ExponentialMovingAverage { Length = secondaryFast },
				LongMa = new ExponentialMovingAverage { Length = secondarySlow }
			},
			SignalMa = new ExponentialMovingAverage { Length = secondarySignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_primaryMacd, _secondaryMacd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _primaryMacd);
			DrawOwnTrades(area);

			var secondaryArea = CreateChartArea();
			if (secondaryArea != null)
			{
				DrawIndicator(secondaryArea, _secondaryMacd);
			}
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue primaryValue, IIndicatorValue secondaryValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var primaryTyped = (MovingAverageConvergenceDivergenceSignalValue)primaryValue;
		if (primaryTyped.Macd is not decimal primaryMacd || primaryTyped.Signal is not decimal primarySignal)
			return;

		var secondaryTyped = (MovingAverageConvergenceDivergenceSignalValue)secondaryValue;
		if (secondaryTyped.Macd is not decimal secondaryMacd || secondaryTyped.Signal is not decimal secondarySignal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			var exitLong = secondaryMacd < secondarySignal && primarySignal > 0m;
			if (exitLong)
			{
				SellMarket(Position);
			}
			return;
		}

		if (Position < 0)
		{
			var exitShort = secondaryMacd > secondarySignal && primarySignal < 0m;
			if (exitShort)
			{
				BuyMarket(Math.Abs(Position));
			}
			return;
		}

		if (!HasCapacityForEntry())
			return;

		var shortSetup = primaryMacd < primarySignal && primarySignal > 0m && secondaryMacd < secondarySignal && secondarySignal > 0m;
		var longSetup = primaryMacd > primarySignal && primarySignal < 0m && secondaryMacd > secondarySignal && secondarySignal < 0m;

		if (shortSetup)
		{
			SellMarket();
		}
		else if (longSetup)
		{
			BuyMarket();
		}
	}

	private bool HasCapacityForEntry()
	{
		if (MaxOpenOrders <= 0)
			return true;

		var activeOrders = 0;
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
			{
				activeOrders++;
			}
		}

		var openPositions = Position != 0m ? 1 : 0;
		return activeOrders + openPositions < MaxOpenOrders;
	}
}
