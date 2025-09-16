using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Randomized Avalanche martingale strategy converted from the original MQL version.
/// </summary>
public class AvalancheAvStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _maxDrawdownPercent;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<int> _decisionInterval;
	private readonly StrategyParam<DataType> _candleType;

	private Random _random = new(Environment.TickCount);

	private decimal _currentVolume;
	private decimal _initialBalance;
	private decimal _previousBalanceMax;
	private decimal _lastRealizedPnL;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _lastTradeVolume;
	private decimal _previousPosition;
	private int _counter;

	/// <summary>
	/// Starting order volume.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Maximum allowed floating loss as a percentage of account balance.
	/// </summary>
	public decimal MaxDrawdownPercent
	{
		get => _maxDrawdownPercent.Value;
		set => _maxDrawdownPercent.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied after losing trades.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Number of finished candles between trade decisions.
	/// </summary>
	public int DecisionInterval
	{
		get => _decisionInterval.Value;
		set => _decisionInterval.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AvalancheAvStrategy"/>.
	/// </summary>
	public AvalancheAvStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Starting order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 15)
			.SetRange(0, int.MaxValue)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 30)
			.SetRange(0, int.MaxValue)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

		_maxDrawdownPercent = Param(nameof(MaxDrawdownPercent), 75m)
			.SetRange(0m, 100m)
			.SetDisplay("Max Drawdown %", "Maximum allowed floating loss", "Risk");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.6m)
			.SetRange(1m, 10m)
			.SetDisplay("Martingale Multiplier", "Volume multiplier after losses", "Trading");

		_decisionInterval = Param(nameof(DecisionInterval), 9)
			.SetRange(1, int.MaxValue)
			.SetDisplay("Decision Interval", "Finished candles between decisions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_random = new Random(Environment.TickCount);
		_currentVolume = InitialVolume;
		_initialBalance = 0m;
		_previousBalanceMax = 0m;
		_lastRealizedPnL = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_lastTradeVolume = 0m;
		_previousPosition = 0m;
		_counter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentVolume = InitialVolume;
		_initialBalance = (Portfolio?.BeginValue ?? Portfolio?.CurrentValue) ?? 0m;
		_previousBalanceMax = _initialBalance;
		_lastRealizedPnL = PnL;
		_counter = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage active positions before evaluating new trades.
		if (HandlePositionRisk(candle.ClosePrice))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var interval = Math.Max(DecisionInterval, 1);
		_counter++;
		if (_counter < interval)
			return;
		_counter = 0;

		if (Position != 0)
			return;

		var shouldBuy = _random.Next(0, 32768) > 16384;
		var entryPrice = candle.ClosePrice;

		if (shouldBuy)
		{
			EnterLong(entryPrice);
		}
		else
		{
			EnterShort(entryPrice);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (_previousPosition == 0m && Position != 0m)
		{
			_lastTradeVolume = Math.Abs(Position);
		}
		else if (_previousPosition != 0m && Position == 0m)
		{
			var tradePnL = PnL - _lastRealizedPnL;
			_lastRealizedPnL = PnL;
			var closedVolume = Math.Abs(_previousPosition);
			UpdateVolumeAfterTrade(tradePnL, closedVolume);
			ResetTradeLevels();
		}

		_previousPosition = Position;
	}

	private void EnterLong(decimal price)
	{
		var volume = PrepareVolume();
		if (volume == 0m)
			return;

		var pip = GetPipSize();
		var stopOffset = StopLossPips > 0 ? StopLossPips * pip : 0m;
		var takeOffset = TakeProfitPips > 0 ? TakeProfitPips * pip : 0m;

		_entryPrice = price;
		_stopPrice = stopOffset > 0m ? price - stopOffset : 0m;
		_takeProfitPrice = takeOffset > 0m ? price + takeOffset : 0m;
		_lastTradeVolume = volume;

		// Enter long position at market.
		BuyMarket(volume);
	}

	private void EnterShort(decimal price)
	{
		var volume = PrepareVolume();
		if (volume == 0m)
			return;

		var pip = GetPipSize();
		var stopOffset = StopLossPips > 0 ? StopLossPips * pip : 0m;
		var takeOffset = TakeProfitPips > 0 ? TakeProfitPips * pip : 0m;

		_entryPrice = price;
		_stopPrice = stopOffset > 0m ? price + stopOffset : 0m;
		_takeProfitPrice = takeOffset > 0m ? price - takeOffset : 0m;
		_lastTradeVolume = volume;

		// Enter short position at market.
		SellMarket(volume);
	}

	private bool HandlePositionRisk(decimal price)
	{
		if (Position == 0)
			return false;

		if (CheckDrawdown(price))
			return true;

		return CheckTargets(price);
	}

	private bool CheckDrawdown(decimal currentPrice)
	{
		if (MaxDrawdownPercent <= 0m || _entryPrice == 0m)
			return false;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
			return false;

		var diff = currentPrice - _entryPrice;
		var steps = diff / priceStep;
		var pnl = steps * stepPrice * Position;
		if (pnl >= 0m)
			return false;

		var balance = _initialBalance + PnL;
		if (balance <= 0m)
			return false;

		var lossPercent = (-pnl * 100m) / balance;
		if (lossPercent <= MaxDrawdownPercent)
			return false;

		// Close the position when floating loss exceeds the threshold.
		ClosePosition();
		return true;
	}

	private bool CheckTargets(decimal currentPrice)
	{
		if (Position > 0)
		{
			if (_stopPrice > 0m && currentPrice <= _stopPrice)
			{
				ClosePosition();
				return true;
			}

			if (_takeProfitPrice > 0m && currentPrice >= _takeProfitPrice)
			{
				ClosePosition();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice > 0m && currentPrice >= _stopPrice)
			{
				ClosePosition();
				return true;
			}

			if (_takeProfitPrice > 0m && currentPrice <= _takeProfitPrice)
			{
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private void ClosePosition()
	{
		var volume = Math.Abs(Position);
		if (volume == 0m)
			return;

		if (Position > 0)
		{
			// Exit long position.
			SellMarket(volume);
		}
		else
		{
			// Exit short position.
			BuyMarket(volume);
		}
	}

	private decimal PrepareVolume()
	{
		var normalized = NormalizeVolume(_currentVolume);
		if (normalized == 0m)
		{
			_currentVolume = InitialVolume;
			return 0m;
		}

		_currentVolume = normalized;
		return normalized;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var minVolume = Security?.MinVolume ?? 0m;
		var maxVolume = Security?.MaxVolume ?? 0m;
		var step = Security?.VolumeStep ?? 0m;

		var adjusted = volume;
		if (step > 0m)
		{
			var steps = Math.Floor(adjusted / step);
			adjusted = steps * step;
		}

		if (minVolume > 0m && adjusted < minVolume)
			return 0m;

		if (maxVolume > 0m && adjusted > maxVolume)
			adjusted = maxVolume;

		return adjusted;
	}

	private decimal GetPipSize()
	{
		return Security?.PriceStep ?? 0m;
	}

	private void UpdateVolumeAfterTrade(decimal tradePnL, decimal closedVolume)
	{
		var baseVolume = closedVolume > 0m ? closedVolume : _currentVolume;

		if (tradePnL < 0m)
		{
			ApplyMartingale(baseVolume);
			return;
		}

		var balance = _initialBalance + PnL;
		if (balance >= _previousBalanceMax)
		{
			_previousBalanceMax = balance;
			_currentVolume = InitialVolume;
		}
		else
		{
			ApplyMartingale(baseVolume);
		}
	}

	private void ApplyMartingale(decimal baseVolume)
	{
		var nextVolume = NormalizeVolume(baseVolume * MartingaleMultiplier);
		if (nextVolume == 0m)
		{
			_currentVolume = InitialVolume;
			return;
		}

		_currentVolume = nextVolume;
	}

	private void ResetTradeLevels()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}
}
