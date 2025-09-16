using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TRIX cross strategy based on the "TRIX ARROWS" expert advisor.
/// Opens a long position when the signal line crosses above TRIX and a short position on the opposite crossover.
/// Includes optional stop loss, take profit, break-even and trailing stop logic.
/// </summary>
public class EaTrixStrategy : Strategy
{
	private enum SignalDirection
	{
		Buy,
		Sell,
	}

	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<decimal> _breakEven;
	private readonly StrategyParam<bool> _tradeOnCloseBar;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _trixEma1 = null!;
	private ExponentialMovingAverage _trixEma2 = null!;
	private ExponentialMovingAverage _trixEma3 = null!;
	private ExponentialMovingAverage _signalEma1 = null!;
	private ExponentialMovingAverage _signalEma2 = null!;
	private ExponentialMovingAverage _signalEma3 = null!;

	private decimal? _prevThirdTrix;
	private decimal? _prevThirdSignal;
	private decimal? _prevTrix;
	private decimal? _prevSignal;

	private SignalDirection? _pendingSignal;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Stop loss distance in price units. Set to zero to disable.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units. Set to zero to disable.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units. Set to zero to disable.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Minimal step for trailing stop updates.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Break-even trigger distance. The stop is moved to the entry price when the distance is reached.
	/// </summary>
	public decimal BreakEven
	{
		get => _breakEven.Value;
		set => _breakEven.Value = value;
	}

	/// <summary>
	/// Trade using signals confirmed on the previous closed bar.
	/// When disabled the strategy reacts immediately on the bar that generated the crossover.
	/// </summary>
	public bool TradeOnCloseBar
	{
		get => _tradeOnCloseBar.Value;
		set => _tradeOnCloseBar.Value = value;
	}

	/// <summary>
	/// EMA length used to build the TRIX series.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// EMA length used to build the signal series.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EaTrixStrategy"/>.
	/// </summary>
	public EaTrixStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 150m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Take profit distance", "Risk")
			.SetCanOptimize(true);

		_trailingStop = Param(nameof(TrailingStop), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
			.SetCanOptimize(true);

		_trailingStep = Param(nameof(TrailingStep), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step", "Minimal trailing step", "Risk")
			.SetCanOptimize(true);

		_breakEven = Param(nameof(BreakEven), 2m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break Even", "Break-even trigger distance", "Risk")
			.SetCanOptimize(true);

		_tradeOnCloseBar = Param(nameof(TradeOnCloseBar), true)
			.SetDisplay("Trade On Close", "Confirm signals on closed bars", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("TRIX EMA", "TRIX EMA length", "Indicators")
			.SetCanOptimize(true);

		_signalPeriod = Param(nameof(SignalPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal EMA", "Signal EMA length", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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

		_prevThirdTrix = null;
		_prevThirdSignal = null;
		_prevTrix = null;
		_prevSignal = null;
		_pendingSignal = null;

		ClearPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_trixEma1 = new ExponentialMovingAverage { Length = EmaPeriod };
		_trixEma2 = new ExponentialMovingAverage { Length = EmaPeriod };
		_trixEma3 = new ExponentialMovingAverage { Length = EmaPeriod };

		_signalEma1 = new ExponentialMovingAverage { Length = SignalPeriod };
		_signalEma2 = new ExponentialMovingAverage { Length = SignalPeriod };
		_signalEma3 = new ExponentialMovingAverage { Length = SignalPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		HandlePendingSignal(candle);

		ManageActivePosition(candle);

		if (!TryCalculateIndicators(candle, out var trix, out var signal))
			return;

		if (_prevTrix is null || _prevSignal is null)
		{
			_prevTrix = trix;
			_prevSignal = signal;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevTrix = trix;
			_prevSignal = signal;
			return;
		}

		var crossUp = _prevSignal < _prevTrix && signal > trix;
		var crossDown = _prevSignal > _prevTrix && signal < trix;

		if (crossUp)
		{
			if (TradeOnCloseBar)
				_pendingSignal = SignalDirection.Buy;
			else
				ExecuteSignal(SignalDirection.Buy, candle, candle.ClosePrice);
		}
		else if (crossDown)
		{
			if (TradeOnCloseBar)
				_pendingSignal = SignalDirection.Sell;
			else
				ExecuteSignal(SignalDirection.Sell, candle, candle.ClosePrice);
		}

		_prevTrix = trix;
		_prevSignal = signal;
	}

	private void HandlePendingSignal(ICandleMessage candle)
	{
		if (_pendingSignal is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ExecuteSignal(_pendingSignal.Value, candle, candle.OpenPrice);
		_pendingSignal = null;
	}

	private void ExecuteSignal(SignalDirection direction, ICandleMessage candle, decimal fillPrice)
	{
		if (Volume <= 0m)
			return;

		var volume = Volume;

		switch (direction)
		{
			case SignalDirection.Buy:
				if (Position < 0m)
					volume += Math.Abs(Position);

				if (volume > 0m)
					BuyMarket(volume);

				_entryPrice = fillPrice;
				_stopPrice = StopLoss > 0m ? fillPrice - StopLoss : null;
				_takePrice = TakeProfit > 0m ? fillPrice + TakeProfit : null;
				break;

			case SignalDirection.Sell:
				if (Position > 0m)
					volume += Position;

				if (volume > 0m)
					SellMarket(volume);

				_entryPrice = fillPrice;
				_stopPrice = StopLoss > 0m ? fillPrice + StopLoss : null;
				_takePrice = TakeProfit > 0m ? fillPrice - TakeProfit : null;
				break;
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m && _entryPrice is decimal longEntry)
		{
			if (BreakEven > 0m && candle.HighPrice - longEntry >= BreakEven && (_stopPrice is null || _stopPrice < longEntry))
				_stopPrice = longEntry;

			if (TrailingStop > 0m)
			{
				var move = candle.HighPrice - longEntry;
				if (move >= TrailingStop)
				{
					var newStop = candle.HighPrice - TrailingStop;
					if (_stopPrice is null || newStop - _stopPrice >= TrailingStep)
						_stopPrice = newStop;
				}
			}

			if (_takePrice is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				ClearPositionState();
				return;
			}

			if (_stopPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Position);
				ClearPositionState();
			}
		}
		else if (Position < 0m && _entryPrice is decimal shortEntry)
		{
			if (BreakEven > 0m && shortEntry - candle.LowPrice >= BreakEven && (_stopPrice is null || _stopPrice > shortEntry))
				_stopPrice = shortEntry;

			if (TrailingStop > 0m)
			{
				var move = shortEntry - candle.LowPrice;
				if (move >= TrailingStop)
				{
					var newStop = candle.LowPrice + TrailingStop;
					if (_stopPrice is null || _stopPrice - newStop >= TrailingStep)
						_stopPrice = newStop;
				}
			}

			if (_takePrice is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				ClearPositionState();
				return;
			}

			if (_stopPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(Math.Abs(Position));
				ClearPositionState();
			}
		}
		else if (Position == 0m)
		{
			ClearPositionState();
		}
	}

	private bool TryCalculateIndicators(ICandleMessage candle, out decimal trix, out decimal signal)
	{
		trix = 0m;
		signal = 0m;

		var ema1Value = _trixEma1.Process(new DecimalIndicatorValue(_trixEma1, candle.ClosePrice, candle.OpenTime));
		if (ema1Value is not DecimalIndicatorValue { IsFinal: true, Value: var ema1 })
			return false;

		var ema2Value = _trixEma2.Process(new DecimalIndicatorValue(_trixEma2, ema1, candle.OpenTime));
		if (ema2Value is not DecimalIndicatorValue { IsFinal: true, Value: var ema2 })
			return false;

		var ema3Value = _trixEma3.Process(new DecimalIndicatorValue(_trixEma3, ema2, candle.OpenTime));
		if (ema3Value is not DecimalIndicatorValue { IsFinal: true, Value: var ema3 })
			return false;

		if (_prevThirdTrix is null)
		{
			_prevThirdTrix = ema3;
			return false;
		}

		trix = _prevThirdTrix != 0m ? (ema3 - _prevThirdTrix.Value) / _prevThirdTrix.Value : 0m;
		_prevThirdTrix = ema3;

		var signal1Value = _signalEma1.Process(new DecimalIndicatorValue(_signalEma1, candle.ClosePrice, candle.OpenTime));
		if (signal1Value is not DecimalIndicatorValue { IsFinal: true, Value: var signal1 })
			return false;

		var signal2Value = _signalEma2.Process(new DecimalIndicatorValue(_signalEma2, signal1, candle.OpenTime));
		if (signal2Value is not DecimalIndicatorValue { IsFinal: true, Value: var signal2 })
			return false;

		var signal3Value = _signalEma3.Process(new DecimalIndicatorValue(_signalEma3, signal2, candle.OpenTime));
		if (signal3Value is not DecimalIndicatorValue { IsFinal: true, Value: var signalBase })
			return false;

		if (_prevThirdSignal is null)
		{
			_prevThirdSignal = signalBase;
			return false;
		}

		signal = _prevThirdSignal != 0m ? (signalBase - _prevThirdSignal.Value) / _prevThirdSignal.Value : 0m;
		_prevThirdSignal = signalBase;

		return true;
	}

	private void ClearPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}
}
