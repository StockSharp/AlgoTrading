using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD crossover strategy converted from the "MACD EA" MetaTrader expert advisor.
/// Implements partial profit taking, breakeven logic, and optional money management scaling.
/// </summary>
public class MacdEaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _partialProfitPips;
	private readonly StrategyParam<int> _breakevenPips;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _macdDiffs = new();

	private decimal? _entryPrice;
	private decimal _currentPositionVolume;
	private int _entryDirection;
	private bool _partialTaken;
	private bool _breakevenActive;
	private decimal _tradePnl;
	private int _consecutiveLosses;

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal moving average period.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
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
	/// Profit target for closing half of the position in pips.
	/// </summary>
	public int PartialProfitPips
	{
		get => _partialProfitPips.Value;
		set => _partialProfitPips.Value = value;
	}

	/// <summary>
	/// Breakeven activation distance in pips.
	/// </summary>
	public int BreakevenPips
	{
		get => _breakevenPips.Value;
		set => _breakevenPips.Value = value;
	}

	/// <summary>
	/// Enables money management scaling when true.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the base volume when money management is enabled.
	/// </summary>
	public decimal RiskMultiplier
	{
		get => _riskMultiplier.Value;
		set => _riskMultiplier.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
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
	/// Initializes a new instance of <see cref="MacdEaStrategy"/>.
	/// </summary>
	public MacdEaStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 120, 5);

		_slowPeriod = Param(nameof(SlowPeriod), 69)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 5);

		_signalPeriod = Param(nameof(SignalPeriod), 90)
			.SetGreaterThanZero()
			.SetDisplay("Signal MA", "Signal moving average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 5);

		_stopLossPips = Param(nameof(StopLossPips), 80)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 500)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 800, 20);

		_partialProfitPips = Param(nameof(PartialProfitPips), 70)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Partial Profit", "Pips to close half the position", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 10);

		_breakevenPips = Param(nameof(BreakevenPips), 0)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Breakeven", "Distance to activate breakeven", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 10);

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Use MM", "Enable money management scaling", "Money Management");

		_riskMultiplier = Param(nameof(RiskMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Multiplier", "Multiplier applied to base volume", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Default order size", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_macdDiffs.Clear();
		_entryPrice = null;
		_currentPositionVolume = 0m;
		_entryDirection = 0;
		_partialTaken = false;
		_breakevenActive = false;
		_tradePnl = 0m;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastPeriod,
			LongPeriod = SlowPeriod,
			SignalPeriod = SignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal signalLine, decimal histogram)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var diff = macdLine - signalLine;
		_macdDiffs.Add(diff);
		if (_macdDiffs.Count > 50)
		_macdDiffs.RemoveRange(0, _macdDiffs.Count - 50);

		if (_macdDiffs.Count < 5)
		return;

		var diffTwo = _macdDiffs[^3];
		var diffFour = _macdDiffs[^5];

		var bullish = diffTwo > 0m && diffFour < 0m;
		var bearish = diffTwo < 0m && diffFour > 0m;

		var pip = GetPipSize();

		if (Position > 0m)
		{
		if (HandleLongPosition(candle, bearish, pip))
		return;
		}
		else if (Position < 0m)
		{
		if (HandleShortPosition(candle, bullish, pip))
		return;
		}

		if (Position != 0m)
		return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return;

		if (bullish)
		{
		BuyMarket(volume);
		InitializeTradeState(candle.ClosePrice, volume, 1);
		}
		else if (bearish)
		{
		SellMarket(volume);
		InitializeTradeState(candle.ClosePrice, volume, -1);
		}
	}

	private bool HandleLongPosition(ICandleMessage candle, bool bearishSignal, decimal pip)
	{
		if (_entryPrice is not decimal entry)
		return false;

		var remainingVolume = _currentPositionVolume > 0m ? _currentPositionVolume : Math.Abs(Position);
		remainingVolume = NormalizeVolume(remainingVolume);
		if (remainingVolume <= 0m)
		return false;

		var stop = StopLossPips > 0 ? entry - StopLossPips * pip : (decimal?)null;
		var take = TakeProfitPips > 0 ? entry + TakeProfitPips * pip : (decimal?)null;
		var partial = PartialProfitPips > 0 ? entry + PartialProfitPips * pip : (decimal?)null;
		var breakeven = BreakevenPips > 0 ? entry + BreakevenPips * pip : (decimal?)null;

		if (stop is decimal stopPrice && candle.LowPrice <= stopPrice)
		{
		CloseLong(remainingVolume, stopPrice);
		return true;
		}

		if (take is decimal takePrice && candle.HighPrice >= takePrice)
		{
		CloseLong(remainingVolume, takePrice);
		return true;
		}

		if (!_partialTaken && partial is decimal partialPrice && candle.HighPrice >= partialPrice)
		{
		var halfVolume = NormalizeVolume(remainingVolume / 2m);
		if (halfVolume > 0m)
		{
		SellMarket(halfVolume);
		RegisterPnl(partialPrice, halfVolume);
		_currentPositionVolume = Math.Max(0m, _currentPositionVolume - halfVolume);
		_partialTaken = true;
		return true;
		}
		}

		if (breakeven is decimal breakevenPrice && !_breakevenActive && candle.HighPrice >= breakevenPrice)
		_breakevenActive = true;

		if (_breakevenActive && candle.LowPrice <= entry)
		{
		CloseLong(remainingVolume, entry);
		return true;
		}

		if (bearishSignal)
		{
		CloseLong(remainingVolume, candle.ClosePrice);
		return true;
		}

		return false;
	}

	private bool HandleShortPosition(ICandleMessage candle, bool bullishSignal, decimal pip)
	{
		if (_entryPrice is not decimal entry)
		return false;

		var remainingVolume = _currentPositionVolume > 0m ? _currentPositionVolume : Math.Abs(Position);
		remainingVolume = NormalizeVolume(remainingVolume);
		if (remainingVolume <= 0m)
		return false;

		var stop = StopLossPips > 0 ? entry + StopLossPips * pip : (decimal?)null;
		var take = TakeProfitPips > 0 ? entry - TakeProfitPips * pip : (decimal?)null;
		var partial = PartialProfitPips > 0 ? entry - PartialProfitPips * pip : (decimal?)null;
		var breakeven = BreakevenPips > 0 ? entry - BreakevenPips * pip : (decimal?)null;

		if (stop is decimal stopPrice && candle.HighPrice >= stopPrice)
		{
		CloseShort(remainingVolume, stopPrice);
		return true;
		}

		if (take is decimal takePrice && candle.LowPrice <= takePrice)
		{
		CloseShort(remainingVolume, takePrice);
		return true;
		}

		if (!_partialTaken && partial is decimal partialPrice && candle.LowPrice <= partialPrice)
		{
		var halfVolume = NormalizeVolume(remainingVolume / 2m);
		if (halfVolume > 0m)
		{
		BuyMarket(halfVolume);
		RegisterPnl(partialPrice, halfVolume);
		_currentPositionVolume = Math.Max(0m, _currentPositionVolume - halfVolume);
		_partialTaken = true;
		return true;
		}
		}

		if (breakeven is decimal breakevenPrice && !_breakevenActive && candle.LowPrice <= breakevenPrice)
		_breakevenActive = true;

		if (_breakevenActive && candle.HighPrice >= entry)
		{
		CloseShort(remainingVolume, entry);
		return true;
		}

		if (bullishSignal)
		{
		CloseShort(remainingVolume, candle.ClosePrice);
		return true;
		}

		return false;
	}

	private void CloseLong(decimal volume, decimal exitPrice)
	{
		volume = NormalizeVolume(volume);
		if (volume <= 0m)
		return;

		SellMarket(volume);
		RegisterPnl(exitPrice, volume);
		_currentPositionVolume = Math.Max(0m, _currentPositionVolume - volume);
		FinalizeTradeIfClosed();
	}

	private void CloseShort(decimal volume, decimal exitPrice)
	{
		volume = NormalizeVolume(volume);
		if (volume <= 0m)
		return;

		BuyMarket(volume);
		RegisterPnl(exitPrice, volume);
		_currentPositionVolume = Math.Max(0m, _currentPositionVolume - volume);
		FinalizeTradeIfClosed();
	}

	private void InitializeTradeState(decimal entryPrice, decimal volume, int direction)
	{
		_entryPrice = entryPrice;
		_currentPositionVolume = NormalizeVolume(Math.Abs(volume));
		_entryDirection = direction;
		_partialTaken = false;
		_breakevenActive = false;
		_tradePnl = 0m;
	}

	private decimal CalculateOrderVolume()
	{
		var volume = BaseVolume;

		if (UseMoneyManagement)
		{
		var multiplier = _consecutiveLosses switch
		{
		0 => 1m,
		1 => 2m,
		2 => 3m,
		3 => 4m,
		4 => 5m,
		5 => 6m,
		_ => 7m,
		};

		volume *= multiplier * RiskMultiplier;
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var sec = Security;
		if (sec == null)
		return volume;

		var step = sec.VolumeStep ?? 1m;
		if (step <= 0m)
		step = 1m;

		var steps = Math.Floor(volume / step);
		volume = steps * step;

		var min = sec.VolumeMin ?? step;
		if (volume < min)
		return 0m;

		var max = sec.VolumeMax;
		if (max != null && volume > max.Value)
		volume = max.Value;

		return volume;
	}

	private decimal GetPipSize()
	{
		var sec = Security;
		var step = sec?.PriceStep ?? 1m;
		if (step <= 0m)
		return 1m;

		var tmp = step;
		var decimals = 0;

		while (decimals < 10 && decimal.Truncate(tmp) != tmp)
		{
		tmp *= 10m;
		decimals++;
		}

		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}

	private void RegisterPnl(decimal exitPrice, decimal volume)
	{
		if (_entryPrice is not decimal entry || _entryDirection == 0)
		return;

		var pnl = (exitPrice - entry) * volume * _entryDirection;
		_tradePnl += pnl;
	}

	private void FinalizeTradeIfClosed()
	{
		if (_currentPositionVolume > 0m)
		return;

		if (_tradePnl > 0m)
		_consecutiveLosses = 0;
		else if (_tradePnl < 0m)
		_consecutiveLosses++;
		else
		_consecutiveLosses = 0;

		ResetTradeState();
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_currentPositionVolume = 0m;
		_entryDirection = 0;
		_partialTaken = false;
		_breakevenActive = false;
		_tradePnl = 0m;
	}
}
