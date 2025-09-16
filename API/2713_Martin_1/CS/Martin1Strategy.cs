using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedging martingale strategy converted from the MetaTrader script "Martin 1".
/// Adds pyramid entries in profit and opens opposite hedges with increased volume after drawdowns.
/// </summary>
public class Martin1Strategy : Strategy
{
	private sealed class PositionRecord
	{
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<int> _maxMultiplications;
	private readonly StrategyParam<Sides> _startDirection;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	private readonly List<PositionRecord> _longPositions = new();
	private readonly List<PositionRecord> _shortPositions = new();

	private decimal _currentVolume;
	private int _multiplicationCount;

	/// <summary>
	/// Candle type for driving the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable the trading hour filter.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Inclusive hour (exchange time) when the trading window opens.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Inclusive hour (exchange time) when the trading window closes.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the current volume after each hedging step.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum number of hedging multiplications that can be triggered.
	/// </summary>
	public int MaxMultiplications
	{
		get => _maxMultiplications.Value;
		set => _maxMultiplications.Value = value;
	}

	/// <summary>
	/// Direction of the very first position that is opened when flat.
	/// </summary>
	public Sides StartDirection
	{
		get => _startDirection.Value;
		set => _startDirection.Value = value;
	}

	/// <summary>
	/// Minimum floating profit that triggers closing all positions.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Base order volume used for the initial trade.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Martin1Strategy"/> class.
	/// </summary>
	public Martin1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate conditions", "General");

		_useTradingHours = Param(nameof(UseTradingHours), true)
			.SetDisplay("Use Trading Hours", "Restrict entries to a time window", "General");

		_startHour = Param(nameof(StartHour), 2)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour to start monitoring for new trades", "General");

		_endHour = Param(nameof(EndHour), 21)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Hour to stop opening hedges/pyramids", "General");

		_lotMultiplier = Param(nameof(LotMultiplier), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Factor applied to volume after a loss", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 3m, 0.1m);

		_maxMultiplications = Param(nameof(MaxMultiplications), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Multiplications", "Maximum hedging steps", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_startDirection = Param(nameof(StartDirection), Sides.Buy)
			.SetDisplay("Start Direction", "Side of the initial order", "Trading");

		_minProfit = Param(nameof(MinProfit), 1.5m)
			.SetDisplay("Min Profit", "Floating profit target to flatten", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 10m, 0.5m);

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Baseline order size", "Money Management");

		_stopLossPips = Param(nameof(StopLossPips), 40)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance before hedging the opposite side", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance to pyramid in the same direction", "Risk");
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
		_multiplicationCount = 0;
		_currentVolume = AdjustVolume(InitialVolume);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (UseTradingHours && StartHour >= EndHour)
			throw new InvalidOperationException("Start hour must be less than end hour when the filter is enabled.");

		_longPositions.Clear();
		_shortPositions.Clear();
		_multiplicationCount = 0;
		_currentVolume = AdjustVolume(InitialVolume);

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

		var closePrice = candle.ClosePrice;
		var totalProfit = CalculateOpenProfit(closePrice);
		var withinHours = !UseTradingHours || IsWithinTradingHours(candle.CloseTime);

		if (withinHours)
		{
			if (_longPositions.Count > 0)
				EvaluateLongPositions(closePrice);

			if (_shortPositions.Count > 0)
				EvaluateShortPositions(closePrice);
		}

		if (_longPositions.Count == 0 && _shortPositions.Count == 0)
		{
			ResetMartingale();
			OpenInitialPosition(closePrice);
			return;
		}

		if ((_longPositions.Count > 0 || _shortPositions.Count > 0) && totalProfit > MinProfit)
		{
			CloseAllPositions(closePrice);
			ResetMartingale();
		}
	}

	private void EvaluateLongPositions(decimal closePrice)
	{
		var takeProfitDistance = GetTakeProfitDistance();
		var stopLossDistance = GetStopLossDistance();
		var snapshot = _longPositions.ToArray();

		foreach (var position in snapshot)
		{
			var priceGain = closePrice - position.EntryPrice;
			var profit = ConvertPriceToMoney(priceGain, position.Volume);

			if (profit > 0m && priceGain > takeProfitDistance)
				ExecuteOrder(Sides.Buy, _currentVolume, closePrice);

			if (StartDirection == Sides.Buy && stopLossDistance > 0m)
			{
				var lossDistance = position.EntryPrice - closePrice;
				if (lossDistance > stopLossDistance * (_multiplicationCount + 1) &&
					_multiplicationCount + 1 <= MaxMultiplications)
				{
					var newVolume = AdjustVolume(_currentVolume * LotMultiplier);
					if (newVolume > 0m)
					{
						_multiplicationCount++;
						_currentVolume = newVolume;
						ExecuteOrder(Sides.Sell, newVolume, closePrice);
					}
				}
			}
		}
	}

	private void EvaluateShortPositions(decimal closePrice)
	{
		var takeProfitDistance = GetTakeProfitDistance();
		var stopLossDistance = GetStopLossDistance();
		var snapshot = _shortPositions.ToArray();

		foreach (var position in snapshot)
		{
			var priceGain = position.EntryPrice - closePrice;
			var profit = ConvertPriceToMoney(priceGain, position.Volume);

			if (profit > 0m && priceGain > takeProfitDistance)
				ExecuteOrder(Sides.Sell, _currentVolume, closePrice);

			if (StartDirection == Sides.Sell && stopLossDistance > 0m)
			{
				var lossDistance = closePrice - position.EntryPrice;
				if (lossDistance > stopLossDistance * (_multiplicationCount + 1) &&
					_multiplicationCount + 1 <= MaxMultiplications)
				{
					var newVolume = AdjustVolume(_currentVolume * LotMultiplier);
					if (newVolume > 0m)
					{
						_multiplicationCount++;
						_currentVolume = newVolume;
						ExecuteOrder(Sides.Buy, newVolume, closePrice);
					}
				}
			}
		}
	}

	private void OpenInitialPosition(decimal price)
	{
		var volume = _currentVolume;
		if (volume <= 0m)
			return;

		var side = StartDirection == Sides.Sell ? Sides.Sell : Sides.Buy;
		ExecuteOrder(side, volume, price);
	}

	private void CloseAllPositions(decimal price)
	{
		var longVolume = GetTotalVolume(_longPositions);
		if (longVolume > 0m)
		{
			ExecuteOrder(Sides.Sell, longVolume, price);
		}

		var shortVolume = GetTotalVolume(_shortPositions);
		if (shortVolume > 0m)
		{
			ExecuteOrder(Sides.Buy, shortVolume, price);
		}
	}

	private void ExecuteOrder(Sides side, decimal volume, decimal price)
	{
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		UpdatePositions(side, volume, price);
	}

	private void UpdatePositions(Sides side, decimal volume, decimal price)
	{
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
		{
			var remaining = volume;
			var index = 0;
			while (remaining > 0m && index < _shortPositions.Count)
			{
				var position = _shortPositions[index];
				var qty = Math.Min(position.Volume, remaining);
				position.Volume -= qty;
				remaining -= qty;
				if (position.Volume <= 0m)
				{
					_shortPositions.RemoveAt(index);
					continue;
				}

				index++;
			}

			if (remaining > 0m)
			{
				_longPositions.Add(new PositionRecord
				{
					Volume = remaining,
					EntryPrice = price
				});
			}
		}
		else
		{
			var remaining = volume;
			var index = 0;
			while (remaining > 0m && index < _longPositions.Count)
			{
				var position = _longPositions[index];
				var qty = Math.Min(position.Volume, remaining);
				position.Volume -= qty;
				remaining -= qty;
				if (position.Volume <= 0m)
				{
					_longPositions.RemoveAt(index);
					continue;
				}

				index++;
			}

			if (remaining > 0m)
			{
				_shortPositions.Add(new PositionRecord
				{
					Volume = remaining,
					EntryPrice = price
				});
			}
		}
	}

	private decimal CalculateOpenProfit(decimal currentPrice)
	{
		var profit = 0m;

		foreach (var position in _longPositions)
		{
			var diff = currentPrice - position.EntryPrice;
			profit += ConvertPriceToMoney(diff, position.Volume);
		}

		foreach (var position in _shortPositions)
		{
			var diff = position.EntryPrice - currentPrice;
			profit += ConvertPriceToMoney(diff, position.Volume);
		}

		return profit;
	}

	private decimal ConvertPriceToMoney(decimal priceDifference, decimal volume)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return priceDifference * volume;

		var steps = priceDifference / priceStep;
		return steps * stepPrice * volume;
	}

	private decimal GetStopLossDistance()
	{
		var pip = GetPipSize();
		return StopLossPips * pip;
	}

	private decimal GetTakeProfitDistance()
	{
		var pip = GetPipSize();
		return TakeProfitPips * pip;
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

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			volume = Math.Floor(volume / step) * step;
			if (volume < step)
				return 0m;
		}

		return volume;
	}

	private static decimal GetTotalVolume(List<PositionRecord> positions)
	{
		var total = 0m;
		foreach (var position in positions)
			total += position.Volume;
		return total;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= StartHour && hour <= EndHour;
	}

	private void ResetMartingale()
	{
		_multiplicationCount = 0;
		_currentVolume = AdjustVolume(InitialVolume);
	}
}
