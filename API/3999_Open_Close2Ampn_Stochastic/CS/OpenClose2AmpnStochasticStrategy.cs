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
/// Port of the MetaTrader strategy open_close2ampnstochastic_strategy.
/// Replicates the price-action filters combined with a Stochastic crossover and includes the original money management rules.
/// </summary>
public class OpenClose2AmpnStochasticStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticKLength;
	private readonly StrategyParam<int> _stochasticDLength;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic = null!;

	private decimal? _previousOpen;
	private decimal? _previousClose;

	private decimal _averageEntryPrice;
	private decimal _entryVolume;
	private int _entryDirection;
	private int _lossStreak;

	/// <summary>
	/// Base position size used when portfolio data is unavailable.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Fraction of account value used for risk sizing and the drawdown guard.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Scaling factor that reduces position size after consecutive losing trades.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Minimum tradable volume that mirrors the original 0.1 lot floor.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator look-back period.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// %K smoothing period of the Stochastic oscillator.
	/// </summary>
	public int StochasticKLength
	{
		get => _stochasticKLength.Value;
		set => _stochasticKLength.Value = value;
	}

	/// <summary>
	/// %D smoothing period of the Stochastic oscillator.
	/// </summary>
	public int StochasticDLength
	{
		get => _stochasticDLength.Value;
		set => _stochasticDLength.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public OpenClose2AmpnStochasticStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management");

		_maximumRisk = Param(nameof(MaximumRisk), 0.3m)
		.SetNotNegative()
		.SetDisplay("Maximum Risk", "Fraction of equity used for sizing and the drawdown guard", "Money Management");

		_decreaseFactor = Param(nameof(DecreaseFactor), 100m)
		.SetNotNegative()
		.SetDisplay("Decrease Factor", "Divisor applied after losing trades to shrink the next position", "Money Management");

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Minimum Volume", "Lowest volume allowed after money management adjustments", "Money Management");

		_stochasticLength = Param(nameof(StochasticLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Length", "Number of periods used by the Stochastic oscillator", "Indicators");

		_stochasticKLength = Param(nameof(StochasticKLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "Smoothing applied to the %K line", "Indicators");

		_stochasticDLength = Param(nameof(StochasticDLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "Smoothing applied to the %D signal line", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time-frame used for processing", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousOpen = null;
		_previousClose = null;
		_averageEntryPrice = 0m;
		_entryVolume = 0m;
		_entryDirection = 0;
		_lossStreak = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Build the Stochastic oscillator that mirrors the original (9,3,3) setup.
		_stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticKLength },
			D = { Length = StochasticDLength },
		};

		// Subscribe to candle data and bind the indicator values.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_stochastic, ProcessCandle)
		.Start();

		// Draw indicator data if a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}

		// Enable built-in protection helpers (stop orders, etc.).
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		// Process signals only once per finished candle.
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFinal)
			return;

		var stochastic = (StochasticOscillatorValue)stochasticValue;
		if (stochastic.K is not decimal main || stochastic.D is not decimal signal)
			return;

		// Evaluate the emergency drawdown guard before new signals.
		if (Position != 0m && ApplyRiskGuard(candle.ClosePrice))
		{
			UpdatePreviousPrices(candle);
			return;
		}

		var previousOpen = _previousOpen;
		var previousClose = _previousClose;

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (previousOpen is decimal prevOpen && previousClose is decimal prevClose)
		{
			if (Position == 0m && canTrade)
			{
				var longSignal = main > signal && candle.OpenPrice < prevOpen && candle.ClosePrice < prevClose;
				var shortSignal = main < signal && candle.OpenPrice > prevOpen && candle.ClosePrice > prevClose;

				if (longSignal)
				{
					var volume = CalculateTradeVolume(candle.ClosePrice);
					if (volume > 0m)
					{
						BuyMarket(volume);
						LogInfo($"Enter long: main={main:F2}, signal={signal:F2}, open={candle.OpenPrice}, close={candle.ClosePrice}, volume={volume}");
					}
				}
				else if (shortSignal)
				{
					var volume = CalculateTradeVolume(candle.ClosePrice);
					if (volume > 0m)
					{
						SellMarket(volume);
						LogInfo($"Enter short: main={main:F2}, signal={signal:F2}, open={candle.OpenPrice}, close={candle.ClosePrice}, volume={volume}");
					}
				}
			}
			else if (Position > 0m)
			{
				var exitLong = main < signal && candle.OpenPrice > prevOpen && candle.ClosePrice > prevClose;
				if (exitLong)
				{
					ClosePosition(candle.ClosePrice);
					LogInfo($"Exit long: main={main:F2}, signal={signal:F2}");
				}
			}
			else if (Position < 0m)
			{
				var exitShort = main > signal && candle.OpenPrice < prevOpen && candle.ClosePrice < prevClose;
				if (exitShort)
				{
					ClosePosition(candle.ClosePrice);
					LogInfo($"Exit short: main={main:F2}, signal={signal:F2}");
				}
			}
		}

		UpdatePreviousPrices(candle);
	}

	private bool ApplyRiskGuard(decimal closePrice)
	{
		if (MaximumRisk <= 0m)
			return false;

		var floatingPnL = CalculateFloatingPnL(closePrice);
		if (floatingPnL >= 0m)
			return false;

		var marginBase = GetMarginBase();
		if (marginBase <= 0m)
			return false;

		var limit = marginBase * MaximumRisk;
		if (Math.Abs(floatingPnL) < limit)
			return false;

		LogInfo($"Risk guard triggered: floatingPnL={floatingPnL}, limit={limit}. Closing position.");
		ClosePosition(closePrice);
		return true;
	}

	private decimal CalculateTradeVolume(decimal price)
	{
		var volume = BaseVolume;

		// Derive the lot size from account value similar to the original EA.
		var accountValue = Portfolio?.CurrentValue;
		if (accountValue is decimal value && value > 0m && price > 0m && MaximumRisk > 0m)
		{
			var riskVolume = Math.Round(value * MaximumRisk / 1000m, 2, MidpointRounding.AwayFromZero);
			if (riskVolume > 0m)
				volume = riskVolume;
		}

		// Apply loss streak reduction once at least two losses occurred, matching the MT4 script.
		if (DecreaseFactor > 0m && _lossStreak > 1)
		{
			var reduction = volume * _lossStreak / DecreaseFactor;
			volume -= reduction;
		}

		if (volume < MinimumVolume)
			volume = MinimumVolume;

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps <= 0m)
				steps = 1m;
			volume = steps * step;
		}

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security.MaxVolume;
		if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private void ClosePosition(decimal closePrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			ResetEntryState();
			return;
		}

		if (Position > 0m)
			SellMarket(volume);
		else
			BuyMarket(volume);

		// Estimate profit using the stored average entry price.
		if (_entryDirection != 0 && _averageEntryPrice > 0m)
		{
			var profit = _entryDirection > 0 ? closePrice - _averageEntryPrice : _averageEntryPrice - closePrice;
			if (profit < 0m)
				_lossStreak++;
			else if (profit > 0m)
				_lossStreak = 0;
		}

		ResetEntryState();
	}

	private void UpdatePreviousPrices(ICandleMessage candle)
	{
		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
	}

	private decimal CalculateFloatingPnL(decimal price)
	{
		if (Position == 0m)
			return 0m;

		var entryPrice = PositionPrice != 0m ? PositionPrice : _averageEntryPrice;
		if (entryPrice == 0m)
			return 0m;

		var priceMove = price - entryPrice;
		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep > 0m && stepPrice > 0m)
		{
			var steps = priceMove / priceStep;
			return steps * stepPrice * Position;
		}

		return priceMove * Position;
	}

	private decimal GetMarginBase()
	{
		if (Portfolio == null)
			return 0m;

		if (Portfolio.BlockedValue is decimal blocked && blocked > 0m)
			return blocked;

		if (Portfolio.CurrentValue is decimal value && value > 0m)
			return value;

		return 0m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null || trade.Trade == null)
			return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		if (trade.Order.Side == Sides.Buy)
		{
			if (Position > 0m)
			{
				RegisterEntry(price, volume, 1);
			}
			else if (Position == 0m && _entryDirection == -1)
			{
				EvaluateClosedTrade(price);
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (Position < 0m)
			{
				RegisterEntry(price, volume, -1);
			}
			else if (Position == 0m && _entryDirection == 1)
			{
				EvaluateClosedTrade(price);
			}
		}
	}

	private void RegisterEntry(decimal price, decimal volume, int direction)
	{
		if (volume <= 0m)
			return;

		if (_entryDirection != direction)
		{
			_entryDirection = direction;
			_averageEntryPrice = price;
			_entryVolume = volume;
			return;
		}

		var totalVolume = _entryVolume + volume;
		if (totalVolume <= 0m)
		{
			ResetEntryState();
			return;
		}

		_averageEntryPrice = (_averageEntryPrice * _entryVolume + price * volume) / totalVolume;
		_entryVolume = totalVolume;
	}

	private void EvaluateClosedTrade(decimal exitPrice)
	{
		if (_entryDirection == 0 || _averageEntryPrice <= 0m)
		{
			ResetEntryState();
			return;
		}

		var profit = _entryDirection > 0 ? exitPrice - _averageEntryPrice : _averageEntryPrice - exitPrice;
		if (profit < 0m)
		{
			_lossStreak++;
		}
		else if (profit > 0m)
		{
			_lossStreak = 0;
		}

		ResetEntryState();
	}

	private void ResetEntryState()
	{
		_averageEntryPrice = 0m;
		_entryVolume = 0m;
		_entryDirection = 0;
	}
}

