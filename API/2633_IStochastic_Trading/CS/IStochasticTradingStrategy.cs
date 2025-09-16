using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale-style strategy driven by Stochastic Oscillator signals.
/// Converts the original MetaTrader IStochastic Trading expert advisor to StockSharp.
/// </summary>
public class IStochasticTradingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _gapPips;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _zoneBuy;
	private readonly StrategyParam<decimal> _zoneSell;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _entries = new();
	private StochasticOscillator _stochastic = null!;
	private decimal _pipSize;
	private PositionSide _currentDirection = PositionSide.None;

	/// <summary>
	/// Volume of the first position in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum favorable move (in pips) required to tighten the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Price gap in pips required to pyramid positions.
	/// </summary>
	public decimal GapPips
	{
		get => _gapPips.Value;
		set => _gapPips.Value = value;
	}

	/// <summary>
	/// Number of bars used for %K calculation.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Period of the %D smoothing line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing factor applied to %K.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Upper boundary that confirms long entries when %D is below it.
	/// </summary>
	public decimal ZoneBuy
	{
		get => _zoneBuy.Value;
		set => _zoneBuy.Value = value;
	}

	/// <summary>
	/// Lower boundary that confirms short entries when %D is above it.
	/// </summary>
	public decimal ZoneSell
	{
		get => _zoneSell.Value;
		set => _zoneSell.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IStochasticTradingStrategy"/> class.
	/// </summary>
	public IStochasticTradingStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume of the first entry in lots", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Distance to take profit in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Distance to stop loss in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Minimal advance before trailing adjustment", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 3)
			.SetGreaterOrEqualZero()
			.SetDisplay("Max Positions", "Maximum simultaneous martingale positions", "Trading");

		_gapPips = Param(nameof(GapPips), 7m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Gap (pips)", "Required adverse move before doubling", "Trading");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Number of bars for %K", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Smoothing period for %D", "Indicators");

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Additional smoothing applied to %K", "Indicators");

		_zoneBuy = Param(nameof(ZoneBuy), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("Buy Zone", "Upper boundary for bullish confirmation", "Signals");

		_zoneSell = Param(nameof(ZoneSell), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("Sell Zone", "Lower boundary for bearish confirmation", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");
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
		_entries.Clear();
		_currentDirection = PositionSide.None;
		_stochastic = null!;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePriceParameters();
		Volume = OrderVolume;

		_stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			KPeriod = KPeriod,
			DPeriod = DPeriod,
			Smooth = Slowing
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFinal)
			return;

		if (_pipSize <= 0m)
			UpdatePriceParameters();

		ManageExits(candle);
		UpdateTrailingStops(candle);

		var stoch = (StochasticOscillatorValue)stochasticValue;

		if (stoch.K is not decimal mainLine || stoch.D is not decimal signalLine)
			return;

		if (_entries.Count == 0)
		{
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (mainLine > signalLine && signalLine < ZoneBuy && Position <= 0)
			{
				OpenEntry(PositionSide.Long, candle.ClosePrice, OrderVolume);
			}
			else if (mainLine < signalLine && signalLine > ZoneSell && Position >= 0)
			{
				OpenEntry(PositionSide.Short, candle.ClosePrice, OrderVolume);
			}
		}
		else
		{
			ManageScaling(candle.ClosePrice);
		}
	}

	private void ManageExits(ICandleMessage candle)
	{
		if (_entries.Count == 0)
			return;

		var closePrice = candle.ClosePrice;

		for (var i = _entries.Count - 1; i >= 0; i--)
		{
			var entry = _entries[i];
			var stopTriggered = entry.Side == PositionSide.Long
				? entry.StopPrice > 0m && closePrice <= entry.StopPrice
				: entry.StopPrice > 0m && closePrice >= entry.StopPrice;

			var takeTriggered = !stopTriggered && (entry.Side == PositionSide.Long
				? entry.TakePrice > 0m && closePrice >= entry.TakePrice
				: entry.TakePrice > 0m && closePrice <= entry.TakePrice);

			if (!stopTriggered && !takeTriggered)
				continue;

			ExitEntry(i, entry);
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (_entries.Count == 0)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var trailingStep = TrailingStepPips * _pipSize;
		var closePrice = candle.ClosePrice;

		foreach (var entry in _entries)
		{
			if (entry.Side == PositionSide.Long)
			{
				var newStop = closePrice - trailingDistance;
				if (newStop <= 0m)
					continue;

				if (entry.StopPrice <= 0m || newStop >= entry.StopPrice + trailingStep)
					entry.StopPrice = newStop;
			}
			else
			{
				var newStop = closePrice + trailingDistance;

				if (entry.StopPrice <= 0m || entry.StopPrice >= newStop + trailingStep)
					entry.StopPrice = newStop;
			}
		}
	}

	private void ManageScaling(decimal price)
	{
		if (_entries.Count == 0)
			return;

		if (MaxPositions > 0 && _entries.Count >= MaxPositions)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var gapDistance = GapPips * _pipSize;
		if (gapDistance <= 0m)
			return;

		var lastEntry = _entries[^1];

		if (lastEntry.Side == PositionSide.Long)
		{
			if (lastEntry.EntryPrice - gapDistance > price)
			{
				OpenEntry(PositionSide.Long, price, lastEntry.Volume * 2m);
			}
		}
		else if (lastEntry.Side == PositionSide.Short)
		{
			if (lastEntry.EntryPrice + gapDistance < price)
			{
				OpenEntry(PositionSide.Short, price, lastEntry.Volume * 2m);
			}
		}
	}

	private void OpenEntry(PositionSide side, decimal price, decimal volume)
	{
		if (volume <= 0m)
			return;

		if (_currentDirection != PositionSide.None && _currentDirection != side)
			return;

		if (MaxPositions > 0 && _entries.Count >= MaxPositions)
			return;

		if (side == PositionSide.Long)
			BuyMarket(volume);
		else
			SellMarket(volume);

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		var stopPrice = 0m;
		var takePrice = 0m;

		if (stopDistance > 0m)
			stopPrice = side == PositionSide.Long ? price - stopDistance : price + stopDistance;

		if (takeDistance > 0m)
			takePrice = side == PositionSide.Long ? price + takeDistance : price - takeDistance;

		_entries.Add(new PositionEntry
		{
			Side = side,
			EntryPrice = price,
			Volume = volume,
			StopPrice = stopPrice,
			TakePrice = takePrice
		});

		_currentDirection = side;
	}

	private void ExitEntry(int index, PositionEntry entry)
	{
		if (entry.Volume <= 0m)
			return;

		if (entry.Side == PositionSide.Long)
			SellMarket(entry.Volume);
		else
			BuyMarket(entry.Volume);

		_entries.RemoveAt(index);

		if (_entries.Count == 0)
			_currentDirection = PositionSide.None;
	}

	private void UpdatePriceParameters()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var pipFactor = 1m;
		var decimals = Security?.Decimals;
		if (decimals == 3 || decimals == 5)
			pipFactor = 10m;

		_pipSize = priceStep * pipFactor;
		if (_pipSize <= 0m)
			_pipSize = 1m;
	}

	private sealed class PositionEntry
	{
		public PositionSide Side { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal Volume { get; set; }
		public decimal StopPrice { get; set; }
		public decimal TakePrice { get; set; }
	}

	private enum PositionSide
	{
		None,
		Long,
		Short
	}
}
