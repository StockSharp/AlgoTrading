using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading on order book sentiment.
/// </summary>
public class SessionOrderSentimentStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<int> _minTraders;
	private readonly StrategyParam<decimal> _diffVolumesEx;
	private readonly StrategyParam<decimal> _diffTradersEx;
	private readonly StrategyParam<decimal> _minDiffVolumesEx;
	private readonly StrategyParam<decimal> _minDiffTradersEx;
	private readonly StrategyParam<int> _sleepMinutes;
	private readonly StrategyParam<int> _tpPips;
	private readonly StrategyParam<int> _slPips;

	private decimal _diffVolumes;
	private decimal _diffTraders;
	private int _pos;
	private DateTimeOffset _lastCheckTime;

	/// <summary>
	/// Minimum total volume on either side of the book.
	/// </summary>
	public decimal MinVolume { get => _minVolume.Value; set => _minVolume.Value = value; }

	/// <summary>
	/// Minimum number of orders on either side of the book.
	/// </summary>
	public int MinTraders { get => _minTraders.Value; set => _minTraders.Value = value; }

	/// <summary>
	/// Volume ratio required for entry.
	/// </summary>
	public decimal DiffVolumesEx { get => _diffVolumesEx.Value; set => _diffVolumesEx.Value = value; }

	/// <summary>
	/// Order count ratio required for entry.
	/// </summary>
	public decimal DiffTradersEx { get => _diffTradersEx.Value; set => _diffTradersEx.Value = value; }

	/// <summary>
	/// Volume ratio used after position entry.
	/// </summary>
	public decimal MinDiffVolumesEx { get => _minDiffVolumesEx.Value; set => _minDiffVolumesEx.Value = value; }

	/// <summary>
	/// Order count ratio used after position entry.
	/// </summary>
	public decimal MinDiffTradersEx { get => _minDiffTradersEx.Value; set => _minDiffTradersEx.Value = value; }

	/// <summary>
	/// Delay between order book checks in minutes.
	/// </summary>
	public int SleepMinutes { get => _sleepMinutes.Value; set => _sleepMinutes.Value = value; }

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public int TpPips { get => _tpPips.Value; set => _tpPips.Value = value; }

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public int SlPips { get => _slPips.Value; set => _slPips.Value = value; }

	/// <summary>
	/// Initialize <see cref="SessionOrderSentimentStrategy"/>.
	/// </summary>
	public SessionOrderSentimentStrategy()
	{
		_minVolume = Param(nameof(MinVolume), 20000m)
			.SetDisplay("Min Volume", "Minimum total volume", "General")
			.SetGreaterThanZero();

		_minTraders = Param(nameof(MinTraders), 1000)
			.SetDisplay("Min Orders", "Minimum orders count", "General")
			.SetGreaterThanZero();

		_diffVolumesEx = Param(nameof(DiffVolumesEx), 2m)
			.SetDisplay("Entry Volume Ratio", null, "General");

		_diffTradersEx = Param(nameof(DiffTradersEx), 1.5m)
			.SetDisplay("Entry Order Ratio", null, "General");

		_minDiffVolumesEx = Param(nameof(MinDiffVolumesEx), 1.5m)
			.SetDisplay("Exit Volume Ratio", null, "General");

		_minDiffTradersEx = Param(nameof(MinDiffTradersEx), 1.3m)
			.SetDisplay("Exit Order Ratio", null, "General");

		_sleepMinutes = Param(nameof(SleepMinutes), 5)
			.SetDisplay("Sleep Minutes", "Minutes between checks", "General")
			.SetGreaterThanZero();

		_tpPips = Param(nameof(TpPips), 500)
			.SetDisplay("Take Profit", "Profit target in points", "Risk");

		_slPips = Param(nameof(SlPips), 500)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		Volume = 1;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.OrderBook)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_diffVolumes = DiffVolumesEx;
		_diffTraders = DiffTradersEx;
		_lastCheckTime = DateTimeOffset.MinValue;

		var subscription = SubscribeOrderBook();
		subscription
			.Bind(ProcessDepth)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TpPips * step, UnitTypes.Point),
			stopLoss: new Unit(SlPips * step, UnitTypes.Point));
	}

	private void ProcessDepth(IOrderBookMessage depth)
	{
		var now = depth.ServerTime;
		if (now - _lastCheckTime < TimeSpan.FromMinutes(SleepMinutes))
			return;

		_lastCheckTime = now;

		var buyOrders = depth.Bids?.Length ?? 0;
		var sellOrders = depth.Asks?.Length ?? 0;

		var buyVolume = 0m;
		if (depth.Bids != null)
		{
			foreach (var q in depth.Bids)
				buyVolume += q.Volume;
		}

		var sellVolume = 0m;
		if (depth.Asks != null)
		{
			foreach (var q in depth.Asks)
				sellVolume += q.Volume;
		}

		if (buyOrders == 0) buyOrders = 1;
		if (sellOrders == 0) sellOrders = 1;
		if (buyVolume == 0) buyVolume = 1;
		if (sellVolume == 0) sellVolume = 1;

		var diffTradersCurr = (decimal)buyOrders / sellOrders;
		var diffVolumesCurr = buyVolume / sellVolume;

		if (CheckTradingTime(now) &&
			diffVolumesCurr >= _diffVolumes && diffTradersCurr >= _diffTraders &&
			(buyOrders >= MinTraders || sellOrders >= MinTraders) &&
			(buyVolume >= MinVolume || sellVolume >= MinVolume))
		{
			if (Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_pos = 1;
				_diffVolumes = MinDiffVolumesEx;
				_diffTraders = MinDiffTradersEx;
			}
		}
		else
		{
			diffTradersCurr = (decimal)sellOrders / buyOrders;
			diffVolumesCurr = sellVolume / buyVolume;

			if (CheckTradingTime(now) &&
				diffVolumesCurr >= _diffVolumes && diffTradersCurr >= _diffTraders &&
				(buyOrders >= MinTraders || sellOrders >= MinTraders) &&
				(buyVolume >= MinVolume || sellVolume >= MinVolume))
			{
				if (Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					_pos = 2;
					_diffVolumes = MinDiffVolumesEx;
					_diffTraders = MinDiffTradersEx;
				}
			}
		}

		diffTradersCurr = (decimal)sellOrders / buyOrders;
		diffVolumesCurr = sellVolume / buyVolume;

		if ((_pos == 2 && (diffVolumesCurr < _diffVolumes || diffTradersCurr < _diffTraders)) ||
		(_pos == 1 && (diffVolumesCurr > 1m / _diffVolumes || diffTradersCurr > 1m / _diffTraders)))
		{
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(-Position);

		_pos = 0;
		}

		if (_pos == 0 || !CheckTradingTime(now))
		{
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(-Position);

		_diffVolumes = DiffVolumesEx;
		_diffTraders = DiffTradersEx;
		_pos = 0;
		}
	}

	private static bool CheckTradingTime(DateTimeOffset time)
	{
	if (time.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
	return false;

	if (time.Hour < 10)
	return false;

	var tradeTime = time.TimeOfDay;
	return tradeTime >= new TimeSpan(10, 15, 30) && tradeTime < new TimeSpan(23, 35, 30);
	}
}