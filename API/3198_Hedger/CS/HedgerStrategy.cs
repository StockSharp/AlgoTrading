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
/// Converted version of the MetaTrader expert advisor "hedger".
/// Replicates the drawdown-triggered hedging cycle using the StockSharp high level API.
/// </summary>
public class HedgerStrategy : Strategy
{
	private sealed class PositionRecord
	{
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
		public bool IsHedge { get; set; }
	}

	private readonly StrategyParam<int> _drawdownOpenPips;
	private readonly StrategyParam<int> _drawdownClosePips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<bool> _startWithLong;
	private readonly StrategyParam<bool> _enableVerboseLogging;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionRecord> _longPositions = new();
	private readonly List<PositionRecord> _shortPositions = new();

	private decimal _openDistance;
	private decimal _closeDistance;
	private decimal _pipSize;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public HedgerStrategy()
	{
		_drawdownOpenPips = Param(nameof(DrawdownOpenPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Open Hedge (pips)", "Drawdown in pips that opens the hedge", "Hedging")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_drawdownClosePips = Param(nameof(DrawdownClosePips), 30)
			.SetGreaterThanZero()
			.SetDisplay("Close Hedge (pips)", "Drawdown in pips that closes the hedge", "Hedging")
			.SetCanOptimize(true)
			.SetOptimize(5, 120, 5);

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Volume of the seed trade", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 1m, 0.01m);

		_startWithLong = Param(nameof(StartWithLong), true)
			.SetDisplay("Start With Long", "Automatically open the initial long when flat", "Trading");

		_enableVerboseLogging = Param(nameof(EnableVerboseLogging), false)
			.SetDisplay("Verbose Logging", "Log every hedging action", "Diagnostics");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to monitor drawdown", "Data");
	}

	/// <summary>
	/// Drawdown in pips that opens the hedge.
	/// </summary>
	public int DrawdownOpenPips
	{
		get => _drawdownOpenPips.Value;
		set => _drawdownOpenPips.Value = value;
	}

	/// <summary>
	/// Drawdown in pips that closes the hedge.
	/// </summary>
	public int DrawdownClosePips
	{
		get => _drawdownClosePips.Value;
		set => _drawdownClosePips.Value = value;
	}

	/// <summary>
	/// Volume of the initial seed trade.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Automatically open the first long trade when no exposure exists.
	/// </summary>
	public bool StartWithLong
	{
		get => _startWithLong.Value;
		set => _startWithLong.Value = value;
	}

	/// <summary>
	/// Enable detailed logging of hedging decisions.
	/// </summary>
	public bool EnableVerboseLogging
	{
		get => _enableVerboseLogging.Value;
		set => _enableVerboseLogging.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate drawdown.
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

		_longPositions.Clear();
		_shortPositions.Clear();
		_openDistance = 0m;
		_closeDistance = 0m;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		RecalculateDistances();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		if (price <= 0m)
			return;

		RecalculateDistances();

		CloseHedgesIfNeeded(price);
		OpenHedgesIfNeeded(price);
		OpenInitialLongIfNeeded(price);
	}

	private void CloseHedgesIfNeeded(decimal price)
	{
		var index = 0;
		while (index < _shortPositions.Count)
		{
			var record = _shortPositions[index];
			if (!record.IsHedge)
			{
				index++;
				continue;
			}

			var loss = price - record.EntryPrice;
			if (loss >= _closeDistance)
			{
				var volume = AlignVolume(record.Volume);
				if (volume > 0m)
				{
					LogSignal($"Closing short hedge at {price:0.#####} ({loss / _pipSize:0.##} pips)");
					ExecuteOrder(Sides.Buy, volume, price, false, true);
					continue;
				}
			}

			index++;
		}

		index = 0;
		while (index < _longPositions.Count)
		{
			var record = _longPositions[index];
			if (!record.IsHedge)
			{
				index++;
				continue;
			}

			var loss = record.EntryPrice - price;
			if (loss >= _closeDistance)
			{
				var volume = AlignVolume(record.Volume);
				if (volume > 0m)
				{
					LogSignal($"Closing long hedge at {price:0.#####} ({loss / _pipSize:0.##} pips)");
					ExecuteOrder(Sides.Sell, volume, price, false, true);
					continue;
				}
			}

			index++;
		}
	}

	private void OpenHedgesIfNeeded(decimal price)
	{
		if (!HasHedge(_shortPositions))
		{
			for (var i = 0; i < _longPositions.Count; i++)
			{
				var record = _longPositions[i];
				if (record.IsHedge)
					continue;

				var drawdown = record.EntryPrice - price;
				if (drawdown >= _openDistance)
				{
					var volume = AlignVolume(record.Volume);
					if (volume > 0m)
					{
						LogSignal($"Opening short hedge at {price:0.#####} ({drawdown / _pipSize:0.##} pips)");
						ExecuteOrder(Sides.Sell, volume, price, true, false);
					}

					break;
				}
			}
		}

		if (!HasHedge(_longPositions))
		{
			for (var i = 0; i < _shortPositions.Count; i++)
			{
				var record = _shortPositions[i];
				if (record.IsHedge)
					continue;

				var drawdown = price - record.EntryPrice;
				if (drawdown >= _openDistance)
				{
					var volume = AlignVolume(record.Volume);
					if (volume > 0m)
					{
						LogSignal($"Opening long hedge at {price:0.#####} ({drawdown / _pipSize:0.##} pips)");
						ExecuteOrder(Sides.Buy, volume, price, true, false);
					}

					break;
				}
			}
		}
	}

	private void OpenInitialLongIfNeeded(decimal price)
	{
		if (!StartWithLong)
			return;

		if (_longPositions.Count > 0 || _shortPositions.Count > 0)
			return;

		var volume = AlignVolume(InitialVolume);
		if (volume <= 0m)
			return;

		LogSignal($"Opening initial long at {price:0.#####}");
		ExecuteOrder(Sides.Buy, volume, price, false, false);
	}

	private void ExecuteOrder(Sides side, decimal volume, decimal price, bool isHedge, bool closeHedgeFirst)
	{
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		UpdatePositions(side, volume, price, isHedge, closeHedgeFirst);
	}

	private void UpdatePositions(Sides side, decimal volume, decimal price, bool isHedge, bool closeHedgeFirst)
	{
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
		{
			var remaining = ReducePositions(_shortPositions, volume, closeHedgeFirst);
			if (remaining > 0m)
			{
				_longPositions.Add(new PositionRecord
				{
					Volume = remaining,
					EntryPrice = price,
					IsHedge = isHedge
				});
			}
		}
		else
		{
			var remaining = ReducePositions(_longPositions, volume, closeHedgeFirst);
			if (remaining > 0m)
			{
				_shortPositions.Add(new PositionRecord
				{
					Volume = remaining,
					EntryPrice = price,
					IsHedge = isHedge
				});
			}
		}
	}

	private decimal ReducePositions(List<PositionRecord> positions, decimal volume, bool preferHedge)
	{
		if (volume <= 0m || positions.Count == 0)
			return volume;

		var remaining = volume;
		if (preferHedge)
		{
			remaining = ReduceByCategory(positions, remaining, true);
			remaining = ReduceByCategory(positions, remaining, false);
		}
		else
		{
			remaining = ReduceByCategory(positions, remaining, false);
			remaining = ReduceByCategory(positions, remaining, true);
		}

		return remaining;
	}

	private static decimal ReduceByCategory(List<PositionRecord> positions, decimal volume, bool hedge)
	{
		var remaining = volume;
		var index = 0;
		while (remaining > 0m && index < positions.Count)
		{
			var record = positions[index];
			if (record.IsHedge != hedge)
			{
				index++;
				continue;
			}

			var qty = Math.Min(record.Volume, remaining);
			record.Volume -= qty;
			remaining -= qty;

			if (record.Volume <= 0m)
			{
				positions.RemoveAt(index);
				continue;
			}

			index++;
		}

		return remaining;
	}

	private bool HasHedge(List<PositionRecord> positions)
	{
		for (var i = 0; i < positions.Count; i++)
		{
			if (positions[i].IsHedge)
				return true;
		}

		return false;
	}

	private void RecalculateDistances()
	{
		if (_pipSize > 0m && _openDistance > 0m && _closeDistance > 0m)
			return;

		_pipSize = GetPipSize();
		_openDistance = DrawdownOpenPips * _pipSize;
		_closeDistance = DrawdownClosePips * _pipSize;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 1m;

		var step = priceStep;
		var digits = 0;
		while (step < 1m && digits < 10)
		{
			step *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
			return priceStep * 10m;

		return priceStep;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps <= 0m)
				return 0m;

			volume = steps * step;
		}

		var min = security.VolumeMin ?? 0m;
		if (min > 0m && volume < min)
			volume = min;

		var max = security.VolumeMax ?? 0m;
		if (max > 0m && volume > max)
			volume = max;

		return volume;
	}

	private void LogSignal(string message)
	{
		if (!EnableVerboseLogging)
			return;

		LogInfo(message);
	}
}

