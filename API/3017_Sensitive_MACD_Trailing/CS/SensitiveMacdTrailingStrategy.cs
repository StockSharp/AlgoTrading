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
/// MACD crossover system with trailing stop management converted from the MQL Sensitive expert advisor.
/// </summary>
public class SensitiveMacdTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _macdOpenLevel;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _entryPrice;
	private decimal? _stopLoss;
	private decimal? _takeProfit;
	private decimal _pipSize;
	private decimal _pointValue;

	/// <summary>
	/// Fast EMA period for the MACD line.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD line.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line EMA period.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Minimal MACD magnitude (in price points) required to take a trade.
	/// </summary>
	public decimal MacdOpenLevel
	{
		get => _macdOpenLevel.Value;
		set => _macdOpenLevel.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance expressed in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SensitiveMacdTrailingStrategy"/>.
	/// </summary>
	public SensitiveMacdTrailingStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length for MACD", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length for MACD", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Signal EMA", "Signal EMA length", "MACD");

		_macdOpenLevel = Param(nameof(MacdOpenLevel), 3m)
		.SetGreaterThanZero()
		.SetDisplay("MACD Open Level", "Required MACD magnitude in points", "MACD");

		_stopLossPips = Param(nameof(StopLossPips), 35)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 75)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Step", "Minimal movement before trailing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Source candles", "General");

		Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevMacd = null;
		_prevSignal = null;
		_entryPrice = null;
		_stopLoss = null;
		_takeProfit = null;
		_pipSize = 0m;
		_pointValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
		{
			LogError("Trailing step must be greater than zero when the trailing stop is enabled.");
			Stop();
			return;
		}

		// Calculate price increments used for pip and MACD thresholds.
		_pointValue = CalculatePointValue();
		_pipSize = CalculatePipSize();

		// Create MACD indicator mirroring the original MQL settings.
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		// Subscribe to candle data and bind MACD processing.
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, ProcessCandle).Start();

		// Draw candles, indicator and trades for visual analysis.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Only react to completed candles to avoid intrabar noise.
		if (candle.State != CandleStates.Finished)
		return;

		// Manage existing positions (take profit, stop loss and trailing logic).
		ApplyTrailing(candle);

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
		return;

		var threshold = (MacdOpenLevel <= 0m ? 0m : MacdOpenLevel) * (_pointValue > 0m ? _pointValue : 0.00001m);

		var previousMacd = _prevMacd;
		var previousSignal = _prevSignal;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}

		// Detect MACD crossings in the negative or positive territory respectively.
		var crossUp = previousMacd.HasValue && previousSignal.HasValue && previousMacd.Value < previousSignal.Value && macd > signal;
		var crossDown = previousMacd.HasValue && previousSignal.HasValue && previousMacd.Value > previousSignal.Value && macd < signal;

		if (crossUp && macd < 0m && Math.Abs(macd) > threshold && Position <= 0)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (crossDown && macd > 0m && Math.Abs(macd) > threshold && Position >= 0)
		{
			EnterShort(candle.ClosePrice);
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}

	private void ApplyTrailing(ICandleMessage candle)
	{
		var trailingStop = TrailingStopPips > 0 && _pipSize > 0m ? TrailingStopPips * _pipSize : 0m;
		var trailingStep = TrailingStepPips > 0 && _pipSize > 0m ? TrailingStepPips * _pipSize : 0m;

		if (Position > 0)
		{
			// Close the long position on take profit.
			if (_takeProfit.HasValue && candle.HighPrice >= _takeProfit.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			// Update trailing stop when price advances enough.
			if (trailingStop > 0m && _entryPrice.HasValue)
			{
				var distance = candle.ClosePrice - _entryPrice.Value;
				if (distance > trailingStop + trailingStep)
				{
					var minStop = candle.ClosePrice - (trailingStop + trailingStep);
					var candidate = candle.ClosePrice - trailingStop;
					if (!_stopLoss.HasValue || _stopLoss.Value < minStop)
					_stopLoss = candidate;
				}
			}

			// Exit the long position if the stop loss level is breached.
			if (_stopLoss.HasValue && candle.LowPrice <= _stopLoss.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}
		}
		else if (Position < 0)
		{
			// Close the short position on take profit.
			if (_takeProfit.HasValue && candle.LowPrice <= _takeProfit.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			// Update trailing stop for the short position.
			if (trailingStop > 0m && _entryPrice.HasValue)
			{
				var distance = _entryPrice.Value - candle.ClosePrice;
				if (distance > trailingStop + trailingStep)
				{
					var maxStop = candle.ClosePrice + (trailingStop + trailingStep);
					var candidate = candle.ClosePrice + trailingStop;
					if (!_stopLoss.HasValue || _stopLoss.Value > maxStop)
					_stopLoss = candidate;
				}
			}

			// Exit the short position when the stop loss is touched.
			if (_stopLoss.HasValue && candle.HighPrice >= _stopLoss.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void EnterLong(decimal price)
	{
		var volume = Volume;
		if (volume <= 0m)
		return;

		var quantity = volume;
		if (Position < 0)
		quantity += Math.Abs(Position);

		// Reverse any existing short position and open a new long.
		BuyMarket(quantity);

		_entryPrice = price;
		_takeProfit = TakeProfitPips > 0 && _pipSize > 0m ? price + TakeProfitPips * _pipSize : null;
		_stopLoss = StopLossPips > 0 && _pipSize > 0m ? price - StopLossPips * _pipSize : null;
	}

	private void EnterShort(decimal price)
	{
		var volume = Volume;
		if (volume <= 0m)
		return;

		var quantity = volume;
		if (Position > 0)
		quantity += Position;

		// Reverse any existing long position and open a new short.
		SellMarket(quantity);

		_entryPrice = price;
		_takeProfit = TakeProfitPips > 0 && _pipSize > 0m ? price - TakeProfitPips * _pipSize : null;
		_stopLoss = StopLossPips > 0 && _pipSize > 0m ? price + StopLossPips * _pipSize : null;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLoss = null;
		_takeProfit = null;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		var decimals = security.Decimals ?? 0;

		if (step == 0m)
		step = decimals > 0 ? (decimal)Math.Pow(10, -decimals) : 0.0001m;

		if (decimals == 3 || decimals == 5)
		step *= 10m;

		return step;
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		var decimals = security.Decimals ?? 0;

		if (step == 0m)
		step = decimals > 0 ? (decimal)Math.Pow(10, -decimals) : 0.0001m;

		return step;
	}
}

