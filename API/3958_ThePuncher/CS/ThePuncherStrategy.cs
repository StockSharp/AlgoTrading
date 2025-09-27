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
/// Momentum-reversal strategy converted from the MetaTrader 4 expert advisor "The Puncher".
/// Trades when Stochastic and RSI simultaneously signal extreme oversold or overbought conditions
/// and manages positions with optional stop-loss, take-profit, break-even and trailing stop rules.
/// </summary>
public class ThePuncherStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticSignalPeriod;
	private readonly StrategyParam<int> _stochasticSmoothingPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _breakEvenPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _lastTrailingReference;
	private bool _breakEvenActivated;

	/// <summary>
	/// Trade volume used for new market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Lookback length of the Stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Smoothing period applied to %K before the signal line.
	/// </summary>
	public int StochasticSignalPeriod
	{
		get => _stochasticSignalPeriod.Value;
		set => _stochasticSignalPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period of the Stochastic signal line (%D).
	/// </summary>
	public int StochasticSmoothingPeriod
	{
		get => _stochasticSmoothingPeriod.Value;
		set => _stochasticSmoothingPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Shared oversold threshold for Stochastic and RSI.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Shared overbought threshold for Stochastic and RSI.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Set to zero to disable.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Set to zero to disable.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Set to zero to disable.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum favorable move before the trailing stop is tightened.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Profit in pips required to move the stop to break-even.
	/// </summary>
	public int BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ThePuncherStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Default volume for new entries", "Trading")
		.SetGreaterThanZero();

		_stochasticLength = Param(nameof(StochasticLength), 100)
		.SetDisplay("Stochastic Length", "Lookback period for %K", "Indicators")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(50, 150, 10);

		_stochasticSignalPeriod = Param(nameof(StochasticSignalPeriod), 3)
		.SetDisplay("Stochastic Signal", "Smoothing period for %K", "Indicators")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_stochasticSmoothingPeriod = Param(nameof(StochasticSmoothingPeriod), 3)
		.SetDisplay("Stochastic %D", "Smoothing period for %D", "Indicators")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Calculation period for RSI", "Indicators")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
		.SetDisplay("Oversold Level", "Shared oversold threshold", "Indicators")
		.SetRange(0m, 100m)
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
		.SetDisplay("Overbought Level", "Shared overbought threshold", "Indicators")
		.SetRange(0m, 100m)
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);

		_stopLossPips = Param(nameof(StopLossPips), 2000)
		.SetDisplay("Stop-Loss (pips)", "Protective stop distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(200, 3000, 200);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0)
		.SetDisplay("Take-Profit (pips)", "Profit target distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 3000, 200);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 2000, 100);

		_trailingStepPips = Param(nameof(TrailingStepPips), 1)
		.SetDisplay("Trailing Step (pips)", "Minimum move before trailing", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true)
		.SetOptimize(0, 500, 10);

		_breakEvenPips = Param(nameof(BreakEvenPips), 0)
		.SetDisplay("Break-Even (pips)", "Profit required to move stop to entry", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true)
		.SetOptimize(0, 1000, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for signals", "General");
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

		ResetTradeState();
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_pipSize = CalculatePipSize();

		var stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticSignalPeriod },
			D = { Length = StochasticSmoothingPeriod },
		};

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(stochastic, rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!stochasticValue.IsFinal || !rsiValue.IsFinal)
		return;

		var stochastic = (StochasticOscillatorValue)stochasticValue;
		if (stochastic.D is not decimal stochSignal)
		return;

		var rsi = rsiValue.ToDecimal();

		UpdateEntryPriceFromPosition();

		var buySignal = stochSignal < OversoldLevel && rsi < OversoldLevel;
		var sellSignal = stochSignal > OverboughtLevel && rsi > OverboughtLevel;

		if (HandleActivePosition(candle, buySignal, sellSignal))
		return;

		if (Position == 0)
		{
			if (buySignal)
			{
				EnterLong(candle.ClosePrice);
			}
			else if (sellSignal)
			{
				EnterShort(candle.ClosePrice);
			}
		}
	}

	private bool HandleActivePosition(ICandleMessage candle, bool buySignal, bool sellSignal)
	{
		if (Position > 0)
		{
			if (TryExitLongByProtection(candle))
			return true;

			ApplyLongRiskManagement(candle);

			if (sellSignal)
			{
				SellMarket(Position);
				ResetTradeState();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (TryExitShortByProtection(candle))
			return true;

			ApplyShortRiskManagement(candle);

			if (buySignal)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}

		return false;
	}

	private bool TryExitLongByProtection(ICandleMessage candle)
	{
		if (Position <= 0)
		return false;

		if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
		{
			SellMarket(Position);
			ResetTradeState();
			return true;
		}

		if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
		{
			SellMarket(Position);
			ResetTradeState();
			return true;
		}

		return false;
	}

	private bool TryExitShortByProtection(ICandleMessage candle)
	{
		if (Position >= 0)
		return false;

		if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetTradeState();
			return true;
		}

		if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetTradeState();
			return true;
		}

		return false;
	}

	private void ApplyLongRiskManagement(ICandleMessage candle)
	{
		var close = candle.ClosePrice;

		if (_pipSize > 0m && BreakEvenPips > 0 && !_breakEvenActivated && _entryPrice > 0m)
		{
			var breakEvenDistance = BreakEvenPips * _pipSize;
			if (close - _entryPrice >= breakEvenDistance)
			{
				var breakEvenPrice = _entryPrice;
				if (!_stopPrice.HasValue || _stopPrice.Value < breakEvenPrice)
				_stopPrice = breakEvenPrice;

				_breakEvenActivated = true;
			}
		}

		if (_pipSize > 0m && TrailingStopPips > 0)
		{
			var trailingDistance = TrailingStopPips * _pipSize;
			var trailingStep = TrailingStepPips * _pipSize;

			var reference = _lastTrailingReference ?? _entryPrice;
			var shouldUpdate = trailingStep <= 0m || close - reference >= trailingStep;

			if (shouldUpdate && close - _entryPrice > trailingDistance)
			{
				var newStop = close - trailingDistance;
				if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
				_stopPrice = newStop;

				_lastTrailingReference = close;
			}
		}
	}

	private void ApplyShortRiskManagement(ICandleMessage candle)
	{
		var close = candle.ClosePrice;

		if (_pipSize > 0m && BreakEvenPips > 0 && !_breakEvenActivated && _entryPrice > 0m)
		{
			var breakEvenDistance = BreakEvenPips * _pipSize;
			if (_entryPrice - close >= breakEvenDistance)
			{
				var breakEvenPrice = _entryPrice;
				if (!_stopPrice.HasValue || _stopPrice.Value > breakEvenPrice)
				_stopPrice = breakEvenPrice;

				_breakEvenActivated = true;
			}
		}

		if (_pipSize > 0m && TrailingStopPips > 0)
		{
			var trailingDistance = TrailingStopPips * _pipSize;
			var trailingStep = TrailingStepPips * _pipSize;

			var reference = _lastTrailingReference ?? _entryPrice;
			var shouldUpdate = trailingStep <= 0m || reference - close >= trailingStep;

			if (shouldUpdate && _entryPrice - close > trailingDistance)
			{
				var newStop = close + trailingDistance;
				if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
				_stopPrice = newStop;

				_lastTrailingReference = close;
			}
		}
	}

	private void EnterLong(decimal referencePrice)
	{
		BuyMarket();

		_entryPrice = referencePrice;
		InitializeProtectionLevels(isLong: true);
	}

	private void EnterShort(decimal referencePrice)
	{
		SellMarket();

		_entryPrice = referencePrice;
		InitializeProtectionLevels(isLong: false);
	}

	private void InitializeProtectionLevels(bool isLong)
	{
		_stopPrice = null;
		_takeProfitPrice = null;
		_lastTrailingReference = null;
		_breakEvenActivated = false;

		if (_pipSize <= 0m)
		return;

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		if (isLong)
		{
			if (stopDistance > 0m)
			_stopPrice = _entryPrice - stopDistance;

			if (takeDistance > 0m)
			_takeProfitPrice = _entryPrice + takeDistance;
		}
		else
		{
			if (stopDistance > 0m)
			_stopPrice = _entryPrice + stopDistance;

			if (takeDistance > 0m)
			_takeProfitPrice = _entryPrice - takeDistance;
		}
	}

	private void UpdateEntryPriceFromPosition()
	{
		if (Position == 0)
		return;

		if (PositionPrice != 0m)
			_entryPrice = PositionPrice;
	}

	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_lastTrailingReference = null;
		_breakEvenActivated = false;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
		return step;

		var decimals = Security?.Decimals ?? 0;
		if (decimals > 0)
		{
			var value = 1m;
			for (var i = 0; i < decimals; i++)
			value /= 10m;

			return value;
		}

		return 0.0001m;
	}
}

