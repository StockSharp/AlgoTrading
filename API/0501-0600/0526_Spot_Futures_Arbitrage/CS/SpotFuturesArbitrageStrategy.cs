using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Spot-futures arbitrage strategy using spread thresholds.
/// Opens long spot/short futures or short spot/long futures based on spread deviation.
/// </summary>
public class SpotFuturesArbitrageStrategy : Strategy
{
	private readonly StrategyParam<Security> _spot;
	private readonly StrategyParam<Security> _future;
	private readonly StrategyParam<decimal> _minSpreadPct;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<bool> _adaptive;
	private readonly StrategyParam<int> _maxHoldHours;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _spreadAverage;
	private StandardDeviation _spreadStd;
	private Order _spotOrder;
	private Order _futureOrder;
	private decimal _spotPrice;
	private decimal _futurePrice;
	private decimal _entryVolume;
	private bool _isLong;
	private bool _inPosition;
	private DateTimeOffset _entryTime;

	/// <summary>
	/// Spot security.
	/// </summary>
	public Security Spot
	{
		get => _spot.Value;
		set => _spot.Value = value;
	}

	/// <summary>
	/// Futures security.
	/// </summary>
	public Security Future
	{
		get => _future.Value;
		set => _future.Value = value;
	}

	/// <summary>
	/// Minimum spread percentage to enter.
	/// </summary>
	public decimal MinSpreadPct
	{
		get => _minSpreadPct.Value;
		set => _minSpreadPct.Value = value;
	}

	/// <summary>
	/// Lookback period for spread statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Enable adaptive thresholds.
	/// </summary>
	public bool AdaptiveThreshold
	{
		get => _adaptive.Value;
		set => _adaptive.Value = value;
	}

	/// <summary>
	/// Maximum holding time in hours.
	/// </summary>
	public int MaxHoldHours
	{
		get => _maxHoldHours.Value;
		set => _maxHoldHours.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public SpotFuturesArbitrageStrategy()
	{
		_spot = Param<Security>(nameof(Spot), null)
			.SetDisplay("Spot", "Spot security", "General");

		_future = Param<Security>(nameof(Future), null)
			.SetDisplay("Future", "Futures security", "General");

		_minSpreadPct = Param(nameof(MinSpreadPct), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("Min Spread %", "Minimum spread percentage to enter", "General");

		_lookback = Param(nameof(LookbackPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Period for spread statistics", "General");

		_adaptive = Param(nameof(AdaptiveThreshold), true)
			.SetDisplay("Adaptive Threshold", "Use dynamic thresholds", "General");

		_maxHoldHours = Param(nameof(MaxHoldHours), 6)
			.SetGreaterThanZero()
			.SetDisplay("Max Hold Hours", "Maximum holding time in hours", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Spot == null || Future == null)
			throw new InvalidOperationException("Both spot and futures securities must be set.");

		return [(Spot, CandleType), (Future, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_spotOrder = null;
		_futureOrder = null;
		_spotPrice = 0m;
		_futurePrice = 0m;
		_entryVolume = 0m;
		_isLong = false;
		_inPosition = false;
		_entryTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (Spot == null || Future == null)
			throw new InvalidOperationException("Both spot and futures securities must be set.");

		base.OnStarted2(time);

		_spreadAverage = new SMA { Length = LookbackPeriod };
		_spreadStd = new StandardDeviation { Length = LookbackPeriod };

		var spotSub = SubscribeCandles(CandleType, true, Spot)
			.Bind(c => ProcessCandle(c, true))
			.Start();

		SubscribeCandles(CandleType, true, Future)
			.Bind(c => ProcessCandle(c, false))
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, spotSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, bool isSpot)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (isSpot)
			_spotPrice = candle.ClosePrice;
		else
			_futurePrice = candle.ClosePrice;

		if (_spotPrice <= 0m || _futurePrice <= 0m)
			return;

		var spread = (_futurePrice - _spotPrice) / _spotPrice;

		var avg = _spreadAverage.Process(new DecimalIndicatorValue(_spreadAverage, spread, candle.ServerTime)).ToDecimal();
		var std = _spreadStd.Process(new DecimalIndicatorValue(_spreadStd, spread, candle.ServerTime)).ToDecimal();

		var minSpread = MinSpreadPct / 100m;
		var entryLong = minSpread;
		var entryShort = -minSpread;

		if (AdaptiveThreshold && _spreadAverage.IsFormed && _spreadStd.IsFormed)
		{
			entryLong = Math.Max(minSpread, avg + std * 1.5m);
			entryShort = Math.Min(-minSpread, avg - std * 1.5m);
		}

		var exitThreshold = 0.6m;
		var now = candle.CloseTime;

		// The pair is opened and closed as a whole, so a new decision may not be taken while
		// an earlier market order of either leg is still working.
		if (IsWorking(_spotOrder) || IsWorking(_futureOrder))
			return;

		if (!_inPosition)
		{
			var volume = Volume;

			if (spread >= entryLong)
			{
				_spotOrder = Register(Spot, Sides.Buy, volume);
				_futureOrder = Register(Future, Sides.Sell, volume);
				_isLong = true;
				_inPosition = true;
				_entryVolume = volume;
				_entryTime = now;
			}
			else if (spread <= entryShort)
			{
				_spotOrder = Register(Spot, Sides.Sell, volume);
				_futureOrder = Register(Future, Sides.Buy, volume);
				_isLong = false;
				_inPosition = true;
				_entryVolume = volume;
				_entryTime = now;
			}
		}
		else
		{
			var timeExpired = (now - _entryTime) >= TimeSpan.FromHours(MaxHoldHours);
			var shouldExit = _isLong ? spread < entryLong * exitThreshold : spread > entryShort * exitThreshold;

			if (shouldExit || timeExpired)
			{
				// Each leg is closed with exactly the volume it was opened with, so the
				// order size never depends on the results of the previous cycle.
				_spotOrder = Register(Spot, _isLong ? Sides.Sell : Sides.Buy, _entryVolume);
				_futureOrder = Register(Future, _isLong ? Sides.Buy : Sides.Sell, _entryVolume);

				_isLong = false;
				_inPosition = false;
				_entryVolume = 0m;
				_entryTime = default;
			}
		}
	}

	private static bool IsWorking(Order order)
		=> order != null && order.State is not (OrderStates.Done or OrderStates.Failed);

	private Order Register(Security security, Sides side, decimal volume)
	{
		var order = new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
		};

		RegisterOrder(order);
		return order;
	}
}
