using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MT5 "Fractals at Close prices" expert advisor.
/// Detects bullish and bearish fractal sequences built on close prices and trades trend reversals.
/// Includes configurable trading hours and manual risk management with trailing stops.
/// </summary>
public class FractalsAtClosePricesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closeWindow = new(5);

	private decimal? _lastUpperFractal;
	private decimal? _previousUpperFractal;
	private decimal? _lastLowerFractal;
	private decimal? _previousLowerFractal;

	private decimal _pipValue;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;

	private decimal? _entryPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Trading volume used for every market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Hour when the strategy can start opening positions.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour when the strategy stops opening positions.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Stop-loss size expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit size expressed in pips.
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
	/// Minimum price improvement required before moving the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="FractalsAtClosePricesStrategy"/> parameters.
	/// </summary>
	public FractalsAtClosePricesStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume used for entries", "General")
		.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 10)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Hour when trading can start (0-23)", "Trading Hours");

		_endHour = Param(nameof(EndHour), 22)
		.SetRange(0, 23)
		.SetDisplay("End Hour", "Hour when trading stops (0-23)", "Trading Hours");

		_stopLossPips = Param(nameof(StopLossPips), 30)
		.SetRange(0, 1000)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk Management")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetRange(0, 1000)
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk Management")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
		.SetRange(0, 1000)
		.SetDisplay("Trailing Stop (pips)", "Base distance for the trailing stop", "Risk Management")
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetRange(0, 1000)
		.SetDisplay("Trailing Step (pips)", "Additional move required before trailing", "Risk Management")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles processed by the strategy", "General");
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

		_closeWindow.Clear();
		_lastUpperFractal = null;
		_previousUpperFractal = null;
		_lastLowerFractal = null;
		_previousLowerFractal = null;

		_pipValue = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingStopDistance = 0m;
		_trailingStepDistance = 0m;

		_entryPrice = null;
		ResetRiskLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;

		_pipValue = priceStep;
		if (decimals == 3 || decimals == 5)
		{
			// MT5 version multiplies point value by 10 when the symbol uses 3 or 5 decimals.
			_pipValue *= 10m;
		}

		_stopLossDistance = StopLossPips == 0 ? 0m : StopLossPips * _pipValue;
		_takeProfitDistance = TakeProfitPips == 0 ? 0m : TakeProfitPips * _pipValue;
		_trailingStopDistance = TrailingStopPips == 0 ? 0m : TrailingStopPips * _pipValue;
		_trailingStepDistance = TrailingStepPips == 0 ? 0m : TrailingStepPips * _pipValue;

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
		{
			return;
		}

		UpdateFractals(candle);

		if (!IsWithinTradingHours(candle.OpenTime))
		{
			CloseAllPositions();
			return;
		}

		ApplyRiskManagement(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		ExecuteEntries(candle);
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		// Maintain a rolling window of the five most recent closes.
		_closeWindow.Enqueue(candle.ClosePrice);
		if (_closeWindow.Count > 5)
		{
			_closeWindow.Dequeue();
		}

		if (_closeWindow.Count < 5)
		{
			return;
		}

		var window = _closeWindow.ToArray();
		var center = window[2];

		var isUpper = center > window[0]
		&& center > window[1]
		&& center >= window[3]
		&& center >= window[4];

		if (isUpper)
		{
			_previousUpperFractal = _lastUpperFractal;
			_lastUpperFractal = center;
		}

		var isLower = center < window[0]
		&& center < window[1]
		&& center <= window[3]
		&& center <= window[4];

		if (isLower)
		{
			_previousLowerFractal = _lastLowerFractal;
			_lastLowerFractal = center;
		}
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;

		if (StartHour == EndHour)
		{
			// Trade the entire day when start and end hours are equal.
			return true;
		}

		if (StartHour < EndHour)
		{
			return hour >= StartHour && hour < EndHour;
		}

		return hour >= StartHour || hour < EndHour;
	}

	private void ApplyRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				// Close the long position if the stop-loss level is breached.
				SellMarket(Position);
				ResetRiskLevels();
				return;
			}

			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				// Close the long position when the take-profit level is hit.
				SellMarket(Position);
				ResetRiskLevels();
				return;
			}

			UpdateLongTrailingStop(candle);
		}
		else if (Position < 0)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				// Cover the short position if the stop-loss level is breached.
				BuyMarket(Math.Abs(Position));
				ResetRiskLevels();
				return;
			}

			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				// Cover the short position when the take-profit level is hit.
				BuyMarket(Math.Abs(Position));
				ResetRiskLevels();
				return;
			}

			UpdateShortTrailingStop(candle);
		}
	}

	private void UpdateLongTrailingStop(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m || _entryPrice is not decimal entry)
		{
			return;
		}

		var profitDistance = candle.ClosePrice - entry;
		if (profitDistance <= _trailingStopDistance + _trailingStepDistance)
		{
			return;
		}

		var targetStop = candle.ClosePrice - _trailingStopDistance;
		if (_longStop is decimal currentStop && currentStop >= candle.ClosePrice - (_trailingStopDistance + _trailingStepDistance))
		{
			// Skip updates until price improved by the trailing step.
			return;
		}

		_longStop = targetStop;
	}

	private void UpdateShortTrailingStop(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m || _entryPrice is not decimal entry)
		{
			return;
		}

		var profitDistance = entry - candle.ClosePrice;
		if (profitDistance <= _trailingStopDistance + _trailingStepDistance)
		{
			return;
		}

		var targetStop = candle.ClosePrice + _trailingStopDistance;
		if (_shortStop is decimal currentStop && currentStop <= candle.ClosePrice + (_trailingStopDistance + _trailingStepDistance))
		{
			// Skip updates until price improved by the trailing step.
			return;
		}

		_shortStop = targetStop;
	}

	private void ExecuteEntries(ICandleMessage candle)
	{
		var bullishTrend = _lastLowerFractal is decimal lastLow
		&& _previousLowerFractal is decimal prevLow
		&& prevLow < lastLow;

		if (bullishTrend)
		{
			CloseShortPosition();

			if (Position <= 0 && OrderVolume > 0m)
			{
				BuyMarket(OrderVolume);
				_entryPrice = candle.ClosePrice;
				_longStop = _stopLossDistance > 0m ? candle.ClosePrice - _stopLossDistance : null;
				_longTake = _takeProfitDistance > 0m ? candle.ClosePrice + _takeProfitDistance : null;
				_shortStop = null;
				_shortTake = null;
			}
		}

		var bearishTrend = _lastUpperFractal is decimal lastUp
		&& _previousUpperFractal is decimal prevUp
		&& prevUp > lastUp;

		if (bearishTrend)
		{
			CloseLongPosition();

			if (Position >= 0 && OrderVolume > 0m)
			{
				SellMarket(OrderVolume);
				_entryPrice = candle.ClosePrice;
				_shortStop = _stopLossDistance > 0m ? candle.ClosePrice + _stopLossDistance : null;
				_shortTake = _takeProfitDistance > 0m ? candle.ClosePrice - _takeProfitDistance : null;
				_longStop = null;
				_longTake = null;
			}
		}
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		ResetRiskLevels();
	}

	private void CloseLongPosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
			ResetRiskLevels();
		}
	}

	private void CloseShortPosition()
	{
		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskLevels();
		}
	}

	private void ResetRiskLevels()
	{
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_entryPrice = null;
	}
}
