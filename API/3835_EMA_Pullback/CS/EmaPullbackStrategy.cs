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
/// EMA pullback strategy translated from the original MetaTrader expert advisor.
/// Waits for a crossover between fast and slow exponential moving averages calculated on median prices.
/// After the crossover it enters on a retracement towards the previous candle and applies fixed take-profit and stop-loss levels.
/// </summary>
public class EmaPullbackStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _moveBackPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _previousHigh;
	private decimal? _previousLow;

	private decimal? _longTakePrice;
	private decimal? _longStopPrice;
	private decimal? _shortTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _entryPrice;

	private SignalDirection _pendingDirection = SignalDirection.None;
	private decimal _pointValue;

	private enum SignalDirection
	{
		None,
		Long,
		Short
	}

	/// <summary>
	/// Trade volume used for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Distance from the previous candle extremum required for pullback entries, expressed in points.
	/// </summary>
	public decimal MoveBackPoints
	{
		get => _moveBackPoints.Value;
		set => _moveBackPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
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
	/// Initializes a new instance of the <see cref="EmaPullbackStrategy"/> class.
	/// </summary>
	public EmaPullbackStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Volume for each market order", "General");

		_fastLength = Param(nameof(FastLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Length of the fast EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 15, 1);

		_slowLength = Param(nameof(SlowLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Length of the slow EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(8, 25, 1);

		_moveBackPoints = Param(nameof(MoveBackPoints), 3m)
		.SetNotNegative()
		.SetDisplay("Pullback Distance", "Retracement distance in points from the prior candle", "Trading Rules")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 1m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 5m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 20m, 1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 40m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "General");
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

		_previousFast = null;
		_previousSlow = null;
		_previousHigh = null;
		_previousLow = null;
		_longTakePrice = null;
		_longStopPrice = null;
		_shortTakePrice = null;
		_shortStopPrice = null;
		_entryPrice = null;
		_pendingDirection = SignalDirection.None;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Align the strategy volume with the parameter value.
		Volume = TradeVolume;

		_pointValue = Security?.PriceStep ?? 0m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };

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

		// Median price matches PRICE_MEDIAN used in the MQL version.
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;

		var fastValue = _fastEma.Process(medianPrice, candle.OpenTime, true).ToDecimal();
		var slowValue = _slowEma.Process(medianPrice, candle.OpenTime, true).ToDecimal();

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
		{
			_previousFast = fastValue;
			_previousSlow = slowValue;
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		var prevFast = _previousFast;
		var prevSlow = _previousSlow;

		_previousFast = fastValue;
		_previousSlow = slowValue;

		if (prevFast is null || prevSlow is null)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		// Evaluate existing position for exits before considering new entries.
		ProcessPositionManagement(candle);

		if (Position == 0)
		{
			// Detect EMA crossovers to arm the pullback logic.
			var crossUp = prevFast < prevSlow && fastValue > slowValue;
			var crossDown = prevFast > prevSlow && fastValue < slowValue;

			if (crossUp)
			{
				_pendingDirection = SignalDirection.Long;
				LogInfo($"Fast EMA crossed above slow EMA at {candle.OpenTime}. Awaiting long pullback entry.");
			}
			else if (crossDown)
			{
				_pendingDirection = SignalDirection.Short;
				LogInfo($"Fast EMA crossed below slow EMA at {candle.OpenTime}. Awaiting short pullback entry.");
			}

			TryExecuteEntry(candle, fastValue, slowValue);
		}
		else
		{
			// While a position is open we do not wait for new entries.
			_pendingDirection = SignalDirection.None;
		}

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private void TryExecuteEntry(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (_pendingDirection == SignalDirection.None)
			return;

		var separationThreshold = 2m * _pointValue;
		var moveBackOffset = GetPriceOffset(MoveBackPoints);
		var tradeVolume = Volume > 0m ? Volume : 1m;

		if (_pendingDirection == SignalDirection.Long)
		{
			var emaGap = fastValue - slowValue;
			var triggerPrice = (_previousHigh ?? candle.HighPrice) - moveBackOffset;

			// Require the fast EMA to be sufficiently above the slow EMA and price to pull back.
			if (emaGap > separationThreshold && candle.ClosePrice <= triggerPrice)
			{
				BuyMarket(tradeVolume);
				_entryPrice = candle.ClosePrice;
				SetLongTargets();
				_pendingDirection = SignalDirection.None;
				LogInfo($"Opened long position at {candle.ClosePrice} on pullback after EMA crossover.");
			}
		}
		else if (_pendingDirection == SignalDirection.Short)
		{
			var emaGap = slowValue - fastValue;
			var triggerPrice = (_previousLow ?? candle.LowPrice) + moveBackOffset;

			// Require the slow EMA to stay above the fast EMA and price to retest the previous low.
			if (emaGap > separationThreshold && candle.ClosePrice >= triggerPrice)
			{
				SellMarket(tradeVolume);
				_entryPrice = candle.ClosePrice;
				SetShortTargets();
				_pendingDirection = SignalDirection.None;
				LogInfo($"Opened short position at {candle.ClosePrice} on pullback after EMA crossover.");
			}
		}
	}

	private void ProcessPositionManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var volume = Position;

			if (_longTakePrice is decimal takePrice && candle.HighPrice >= takePrice)
			{
				SellMarket(volume);
				LogInfo($"Long take-profit reached at {takePrice}.");
				ResetPositionState();
				return;
			}

			if (_longStopPrice is decimal stopPrice && candle.LowPrice <= stopPrice)
			{
				SellMarket(volume);
				LogInfo($"Long stop-loss triggered at {stopPrice}.");
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			if (_shortTakePrice is decimal takePrice && candle.LowPrice <= takePrice)
			{
				BuyMarket(volume);
				LogInfo($"Short take-profit reached at {takePrice}.");
				ResetPositionState();
				return;
			}

			if (_shortStopPrice is decimal stopPrice && candle.HighPrice >= stopPrice)
			{
				BuyMarket(volume);
				LogInfo($"Short stop-loss triggered at {stopPrice}.");
				ResetPositionState();
			}
		}
	}

	private void SetLongTargets()
	{
		var takeOffset = GetPriceOffset(TakeProfitPoints);
		var stopOffset = GetPriceOffset(StopLossPoints);

		_longTakePrice = takeOffset > 0m && _entryPrice.HasValue ? _entryPrice.Value + takeOffset : null;
		_longStopPrice = stopOffset > 0m && _entryPrice.HasValue ? _entryPrice.Value - stopOffset : null;

		_shortTakePrice = null;
		_shortStopPrice = null;
	}

	private void SetShortTargets()
	{
		var takeOffset = GetPriceOffset(TakeProfitPoints);
		var stopOffset = GetPriceOffset(StopLossPoints);

		_shortTakePrice = takeOffset > 0m && _entryPrice.HasValue ? _entryPrice.Value - takeOffset : null;
		_shortStopPrice = stopOffset > 0m && _entryPrice.HasValue ? _entryPrice.Value + stopOffset : null;

		_longTakePrice = null;
		_longStopPrice = null;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_longTakePrice = null;
		_longStopPrice = null;
		_shortTakePrice = null;
		_shortStopPrice = null;
	}

	private decimal GetPriceOffset(decimal points)
	{
		if (points <= 0m)
			return 0m;

		return points * _pointValue;
	}
}

