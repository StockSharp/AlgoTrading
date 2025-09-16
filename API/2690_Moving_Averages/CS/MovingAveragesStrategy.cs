using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with risk-based position sizing and trade streak tracking.
/// </summary>
public class MovingAveragesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<int> _movingShift;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = new();
	private decimal[] _shiftBuffer = Array.Empty<decimal>();
	private int _shiftIndex;
	private int _shiftFillCount;
	private decimal _avgEntryPrice;
	private decimal _entryVolume;
	private Sides? _entrySide;
	private int _consecutiveLosses;

	/// <summary>
	/// Maximum risk per trade expressed as portfolio percentage.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Factor that reduces position size after consecutive losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Simple moving average period.
	/// </summary>
	public int MovingPeriod
	{
		get => _movingPeriod.Value;
		set => _movingPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed bars used to shift the moving average.
	/// </summary>
	public int MovingShift
	{
		get => _movingShift.Value;
		set => _movingShift.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAveragesStrategy"/>.
	/// </summary>
	public MovingAveragesStrategy()
	{
		// Configure risk management parameters.
		_maximumRisk = Param(nameof(MaximumRisk), 0.02m)
			.SetGreaterThanZero()
			.SetLessOrEqual(1m)
			.SetDisplay("Maximum Risk", "Fraction of equity risked per trade", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Decrease Factor", "Loss streak divisor for position sizing", "Risk");

		// Configure indicator settings.
		_movingPeriod = Param(nameof(MovingPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Moving Period", "Simple moving average lookback", "Indicator");

		_movingShift = Param(nameof(MovingShift), 6)
			.SetNotNegative()
			.SetDisplay("Moving Shift", "Bars to shift the moving average", "Indicator");

		// Configure candle source for the strategy.
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicator and buffers.
		_sma = new SimpleMovingAverage { Length = MovingPeriod };
		_shiftBuffer = new decimal[Math.Max(1, MovingShift + 1)];
		_shiftIndex = 0;
		_shiftFillCount = 0;
		_consecutiveLosses = 0;
		ResetEntryState();

		// Subscribe to candles and bind indicator values.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		// Add optional chart visuals.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		// Process only finished candles to match bar-based logic.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is ready to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Wait until the moving average has enough data.
		if (!_sma.IsFormed)
			return;

		// Update shifted buffer to emulate MetaTrader style MA shift.
		UpdateShiftBuffer(maValue);

		if (!IsShiftReady())
			return;

		var shiftedMa = GetShiftedValue();

		var crossDown = candle.OpenPrice > shiftedMa && candle.ClosePrice < shiftedMa;
		var crossUp = candle.OpenPrice < shiftedMa && candle.ClosePrice > shiftedMa;

		// Manage existing long position before searching for new entries.
		if (Position > 0)
		{
			if (crossDown)
			{
				CloseLongPosition(candle, shiftedMa);
			}

			return;
		}

		// Manage existing short position before searching for new entries.
		if (Position < 0)
		{
			if (crossUp)
			{
				CloseShortPosition(candle, shiftedMa);
			}

			return;
		}

		// No open position, evaluate entry opportunities.
		if (crossUp)
		{
			OpenLongPosition(candle, shiftedMa);
		}
		else if (crossDown)
		{
			OpenShortPosition(candle, shiftedMa);
		}
	}

	private void OpenLongPosition(ICandleMessage candle, decimal shiftedMa)
	{
		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		LogInfo($"Enter long on bullish cross. Price={candle.ClosePrice}, SMA={shiftedMa}, Volume={volume}");
	}

	private void OpenShortPosition(ICandleMessage candle, decimal shiftedMa)
	{
		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		LogInfo($"Enter short on bearish cross. Price={candle.ClosePrice}, SMA={shiftedMa}, Volume={volume}");
	}

	private void CloseLongPosition(ICandleMessage candle, decimal shiftedMa)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		LogInfo($"Exit long due to bearish cross. Price={candle.ClosePrice}, SMA={shiftedMa}, Volume={volume}");
	}

	private void CloseShortPosition(ICandleMessage candle, decimal shiftedMa)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		LogInfo($"Exit short due to bullish cross. Price={candle.ClosePrice}, SMA={shiftedMa}, Volume={volume}");
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		if (price <= 0m)
			return 0m;

		// Base position size uses portfolio value and risk percentage.
		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		var baseVolume = Volume > 0m ? Volume : 1m;

		if (portfolioValue > 0m && MaximumRisk > 0m)
			baseVolume = portfolioValue * MaximumRisk / price;

		// Apply loss streak reduction similar to the original MQL logic.
		if (DecreaseFactor > 0m && _consecutiveLosses > 0)
		{
			var reduction = baseVolume * _consecutiveLosses / DecreaseFactor;
			baseVolume -= reduction;
		}

		return baseVolume > 0m ? baseVolume : 0m;
	}

	private void UpdateShiftBuffer(decimal value)
	{
		_shiftBuffer[_shiftIndex] = value;
		if (_shiftFillCount < _shiftBuffer.Length)
			_shiftFillCount++;

		_shiftIndex++;
		if (_shiftIndex >= _shiftBuffer.Length)
			_shiftIndex = 0;
	}

	private bool IsShiftReady()
	{
		return _shiftFillCount > MovingShift;
	}

	private decimal GetShiftedValue()
	{
		if (_shiftBuffer.Length == 0)
			return 0m;

		var offset = Math.Min(MovingShift, _shiftFillCount - 1);
		var index = _shiftIndex - 1 - offset;

		while (index < 0)
			index += _shiftBuffer.Length;

		return _shiftBuffer[index];
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		var tradePrice = trade.Trade.Price;
		var tradeVolume = trade.Trade.Volume;

		// Track entries and exits to evaluate profit streaks.
		if (trade.Order.Side == Sides.Buy)
		{
			if (Position > 0)
			{
				RegisterEntry(tradePrice, tradeVolume, Sides.Buy);
			}
			else if (Position == 0 && _entrySide == Sides.Sell)
			{
				EvaluateClosedTrade(tradePrice);
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (Position < 0)
			{
				RegisterEntry(tradePrice, tradeVolume, Sides.Sell);
			}
			else if (Position == 0 && _entrySide == Sides.Buy)
			{
				EvaluateClosedTrade(tradePrice);
			}
		}
	}

	private void RegisterEntry(decimal price, decimal volume, Sides side)
	{
		if (volume <= 0m)
			return;

		var totalVolume = _entryVolume + volume;
		if (totalVolume <= 0m)
		{
			ResetEntryState();
			return;
		}

		_avgEntryPrice = _entryVolume > 0m
			? (_avgEntryPrice * _entryVolume + price * volume) / totalVolume
			: price;

		_entryVolume = totalVolume;
		_entrySide = side;
	}

	private void EvaluateClosedTrade(decimal exitPrice)
	{
		if (_entrySide == null || _entryVolume <= 0m)
		{
			ResetEntryState();
			return;
		}

		decimal profit = 0m;

		if (_entrySide == Sides.Buy)
			profit = exitPrice - _avgEntryPrice;
		else if (_entrySide == Sides.Sell)
			profit = _avgEntryPrice - exitPrice;

		if (profit > 0m)
		{
			_consecutiveLosses = 0;
		}
		else if (profit < 0m)
		{
			_consecutiveLosses++;
		}

		LogInfo($"Trade closed. Side={_entrySide}, Entry={_avgEntryPrice}, Exit={exitPrice}, Profit={profit}, LossStreak={_consecutiveLosses}");

		ResetEntryState();
	}

	private void ResetEntryState()
	{
		_avgEntryPrice = 0m;
		_entryVolume = 0m;
		_entrySide = null;
	}
}
