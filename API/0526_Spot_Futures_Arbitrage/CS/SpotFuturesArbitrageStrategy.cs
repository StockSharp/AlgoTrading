using System;
using System.Collections.Generic;

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
	private decimal _spotPrice;
	private decimal _futurePrice;
	private bool _isLong;
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

		_spotPrice = 0m;
		_futurePrice = 0m;
		_isLong = false;
		_entryTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (Spot == null || Future == null)
			throw new InvalidOperationException("Both spot and futures securities must be set.");

		base.OnStarted(time);

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

		var avg = _spreadAverage.Process(spread, candle.ServerTime, true).ToDecimal();
		var std = _spreadStd.Process(spread, candle.ServerTime, true).ToDecimal();

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

		var spotPos = PositionBy(Spot);
		var futPos = PositionBy(Future);
		var hasPosition = spotPos != 0m || futPos != 0m;

		if (!hasPosition)
		{
			var volume = ComputeVolume();
			if (volume <= 0m)
				return;

			if (spread >= entryLong)
			{
				Buy(Spot, volume);
				Sell(Future, volume);
				_isLong = true;
				_entryTime = now;
			}
			else if (spread <= entryShort)
			{
				Sell(Spot, volume);
				Buy(Future, volume);
				_isLong = false;
				_entryTime = now;
			}
		}
		else
		{
			var timeExpired = (now - _entryTime) >= TimeSpan.FromHours(MaxHoldHours);
			var shouldExit = _isLong ? spread < entryLong * exitThreshold : spread > entryShort * exitThreshold;

			if (shouldExit || timeExpired)
			{
				if (spotPos != 0m)
				{
					RegisterOrder(new Order
					{
						Security = Spot,
						Portfolio = Portfolio,
						Side = spotPos > 0m ? Sides.Sell : Sides.Buy,
						Volume = Math.Abs(spotPos),
						Type = OrderTypes.Market,
					});
				}

				if (futPos != 0m)
				{
					RegisterOrder(new Order
					{
						Security = Future,
						Portfolio = Portfolio,
						Side = futPos > 0m ? Sides.Sell : Sides.Buy,
						Volume = Math.Abs(futPos),
						Type = OrderTypes.Market,
					});
				}

				_isLong = false;
				_entryTime = default;
			}
		}
	}

	private decimal ComputeVolume()
	{
		var equity = Portfolio.CurrentValue ?? 0m;
		if (_spotPrice <= 0m || equity <= 0m)
			return 0m;

		return equity * 0.3m / _spotPrice;
	}

	private void Buy(Security security, decimal volume)
	{
		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = Sides.Buy,
			Volume = volume,
			Type = OrderTypes.Market,
		});
	}

	private void Sell(Security security, decimal volume)
	{
		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = Sides.Sell,
			Volume = volume,
			Type = OrderTypes.Market,
		});
	}

	private decimal PositionBy(Security security) => GetPositionValue(security, Portfolio) ?? 0m;
}
