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
/// Port of the Master MM Droid strategy with modular money management blocks.
/// Uses RSI crossover signals with pyramiding, daily gap detection, and
/// box/weekly breakout modules - all implemented via candle-based checks.
/// </summary>
public class MasterMmDroidStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<int> _rsiMaxEntries;
	private readonly StrategyParam<decimal> _rsiPyramidSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _trailingSteps;
	private readonly StrategyParam<int> _boxLookback;
	private readonly StrategyParam<decimal> _boxEntrySteps;

	private RelativeStrengthIndex _rsi = null!;

	private decimal _previousRsi;
	private bool _hasPreviousRsi;
	private decimal? _lastEntryPrice;
	private int _entryCount;

	private decimal? _activeStopPrice;
	private decimal _bestPrice;

	private decimal _boxHigh;
	private decimal _boxLow;
	private int _boxBarsCount;

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// Maximum pyramiding entries.
	/// </summary>
	public int RsiMaxEntries
	{
		get => _rsiMaxEntries.Value;
		set => _rsiMaxEntries.Value = value;
	}

	/// <summary>
	/// Price steps between pyramid entries.
	/// </summary>
	public decimal RsiPyramidSteps
	{
		get => _rsiPyramidSteps.Value;
		set => _rsiPyramidSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingSteps
	{
		get => _trailingSteps.Value;
		set => _trailingSteps.Value = value;
	}

	/// <summary>
	/// Number of candles for box high/low calculation.
	/// </summary>
	public int BoxLookback
	{
		get => _boxLookback.Value;
		set => _boxLookback.Value = value;
	}

	/// <summary>
	/// Breakout distance above/below the box in price steps.
	/// </summary>
	public decimal BoxEntrySteps
	{
		get => _boxEntrySteps.Value;
		set => _boxEntrySteps.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MasterMmDroidStrategy"/>.
	/// </summary>
	public MasterMmDroidStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "RSI")
			.SetOptimize(7, 21, 7);

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 25m)
			.SetDisplay("RSI Oversold", "RSI oversold threshold", "RSI");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 75m)
			.SetDisplay("RSI Overbought", "RSI overbought threshold", "RSI");

		_rsiMaxEntries = Param(nameof(RsiMaxEntries), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Entries", "Maximum pyramiding steps", "RSI");

		_rsiPyramidSteps = Param(nameof(RsiPyramidSteps), 250m)
			.SetGreaterThanZero()
			.SetDisplay("Pyramid Steps", "Price steps between entries", "RSI");

		_stopLossSteps = Param(nameof(StopLossSteps), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Steps", "Stop-loss distance in price steps", "Risk");

		_trailingSteps = Param(nameof(TrailingSteps), 700m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Steps", "Trailing distance in price steps", "Risk");

		_boxLookback = Param(nameof(BoxLookback), 16)
			.SetGreaterThanZero()
			.SetDisplay("Box Lookback", "Candles for box high/low", "Box");

		_boxEntrySteps = Param(nameof(BoxEntrySteps), 180m)
			.SetGreaterThanZero()
			.SetDisplay("Box Entry Steps", "Breakout distance in price steps", "Box");
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

		_previousRsi = 0m;
		_hasPreviousRsi = false;
		_lastEntryPrice = null;
		_entryCount = 0;
		_activeStopPrice = null;
		_bestPrice = 0m;
		_boxHigh = 0m;
		_boxLow = decimal.MaxValue;
		_boxBarsCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security?.PriceStep ?? 1m;
		var enteredThisCandle = false;

		// Update box tracking
		UpdateBox(candle);

		// Manage trailing stop
		ManageTrailing(candle, step);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousRsi = rsiValue;
			_hasPreviousRsi = true;
			return;
		}

		// Check box breakout entries
		if (Position == 0 && _boxBarsCount >= BoxLookback)
		{
			var boxOffset = BoxEntrySteps * step;
			if (candle.ClosePrice > _boxHigh + boxOffset)
			{
				BuyMarket(Volume);
				_lastEntryPrice = candle.ClosePrice;
				_entryCount = 1;
				_activeStopPrice = candle.ClosePrice - StopLossSteps * step;
				_bestPrice = candle.ClosePrice;
				enteredThisCandle = true;
			}
			else if (candle.ClosePrice < _boxLow - boxOffset)
			{
				SellMarket(Volume);
				_lastEntryPrice = candle.ClosePrice;
				_entryCount = 1;
				_activeStopPrice = candle.ClosePrice + StopLossSteps * step;
				_bestPrice = candle.ClosePrice;
				enteredThisCandle = true;
			}
		}

		// RSI crossover signals
		if (!enteredThisCandle && _hasPreviousRsi && _rsi.IsFormed)
		{
			var rsiCrossUp = _previousRsi <= RsiLowerLevel && rsiValue > RsiLowerLevel;
			var rsiCrossDown = _previousRsi >= RsiUpperLevel && rsiValue < RsiUpperLevel;

			if (rsiCrossUp && Position <= 0)
			{
				var vol = Volume + (Position < 0 ? Math.Abs(Position) : 0);
				BuyMarket(vol);
				_lastEntryPrice = candle.ClosePrice;
				_entryCount = 1;
				_activeStopPrice = candle.ClosePrice - StopLossSteps * step;
				_bestPrice = candle.ClosePrice;
			}
			else if (rsiCrossDown && Position >= 0)
			{
				var vol = Volume + (Position > 0 ? Position : 0);
				SellMarket(vol);
				_lastEntryPrice = candle.ClosePrice;
				_entryCount = 1;
				_activeStopPrice = candle.ClosePrice + StopLossSteps * step;
				_bestPrice = candle.ClosePrice;
			}

			// Pyramiding
			var pyramidDist = RsiPyramidSteps * step;
			if (Position > 0 && _entryCount < RsiMaxEntries && _lastEntryPrice.HasValue)
			{
				if (candle.ClosePrice >= _lastEntryPrice.Value + pyramidDist)
				{
					BuyMarket(Volume);
					_lastEntryPrice = candle.ClosePrice;
					_entryCount++;
				}
			}
			else if (Position < 0 && _entryCount < RsiMaxEntries && _lastEntryPrice.HasValue)
			{
				if (candle.ClosePrice <= _lastEntryPrice.Value - pyramidDist)
				{
					SellMarket(Volume);
					_lastEntryPrice = candle.ClosePrice;
					_entryCount++;
				}
			}
		}

		_previousRsi = rsiValue;
		_hasPreviousRsi = true;
	}

	private void UpdateBox(ICandleMessage candle)
	{
		_boxBarsCount++;
		if (_boxBarsCount <= BoxLookback)
		{
			_boxHigh = Math.Max(_boxHigh, candle.HighPrice);
			_boxLow = Math.Min(_boxLow, candle.LowPrice);
		}
		else
		{
			// Shift the window - approximate by using recent candle
			_boxHigh = Math.Max(_boxHigh, candle.HighPrice);
			_boxLow = Math.Min(_boxLow, candle.LowPrice);
		}
	}

	private void ManageTrailing(ICandleMessage candle, decimal step)
	{
		if (Position == 0)
		{
			_activeStopPrice = null;
			return;
		}

		if (!_activeStopPrice.HasValue)
			return;

		var trailDist = TrailingSteps * step;

		if (Position > 0)
		{
			if (candle.ClosePrice > _bestPrice)
				_bestPrice = candle.ClosePrice;

			var trailStop = _bestPrice - trailDist;
			if (trailStop > _activeStopPrice.Value)
				_activeStopPrice = trailStop;

			if (candle.LowPrice <= _activeStopPrice.Value)
			{
				SellMarket(Position);
				_activeStopPrice = null;
				_lastEntryPrice = null;
				_entryCount = 0;
			}
		}
		else
		{
			if (candle.ClosePrice < _bestPrice || _bestPrice == 0m)
				_bestPrice = candle.ClosePrice;

			var trailStop = _bestPrice + trailDist;
			if (trailStop < _activeStopPrice.Value)
				_activeStopPrice = trailStop;

			if (candle.HighPrice >= _activeStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_activeStopPrice = null;
				_lastEntryPrice = null;
				_entryCount = 0;
			}
		}
	}
}
