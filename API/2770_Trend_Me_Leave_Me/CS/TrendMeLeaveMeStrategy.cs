using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Me Leave Me strategy converted from the original MQL5 version.
/// Waits for calm markets, trades with Parabolic SAR direction and flips after profitable exits.
/// </summary>
public class TrendMeLeaveMeStrategy : Strategy
{
	private enum TradeDirection
	{
		None,
		Buy,
		Sell
	}

	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _breakevenPips;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxQuietLevel;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx = null!;
	private ParabolicSar _sar = null!;

	private TradeDirection _nextDirection = TradeDirection.Buy;
	private bool _breakevenActivated;
	private decimal _pipSize;
	private int _positionDirection;
	private bool _exitOrderPending;

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
	/// Breakeven trigger distance expressed in pips.
	/// </summary>
	public int BreakevenPips
	{
		get => _breakevenPips.Value;
		set => _breakevenPips.Value = value;
	}

	/// <summary>
	/// ADX averaging period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set
		{
			_adxPeriod.Value = value;
			if (_adx != null)
				_adx.Length = value;
		}
	}

	/// <summary>
	/// ADX level that defines when the market is calm enough to enter.
	/// </summary>
	public decimal AdxQuietLevel
	{
		get => _adxQuietLevel.Value;
		set => _adxQuietLevel.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set
		{
			_sarStep.Value = value;
			if (_sar != null)
				_sar.AccelerationStep = value;
		}
	}

	/// <summary>
	/// Maximum Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set
		{
			_sarMax.Value = value;
			if (_sar != null)
				_sar.AccelerationMax = value;
		}
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
	/// Initializes a new instance of the <see cref="TrendMeLeaveMeStrategy"/> class.
	/// </summary>
	public TrendMeLeaveMeStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 180)
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk");

		_breakevenPips = Param(nameof(BreakevenPips), 5)
			.SetDisplay("Breakeven (pips)", "Distance before moving stop to entry", "Risk");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Smoothing period for ADX", "Indicators");

		_adxQuietLevel = Param(nameof(AdxQuietLevel), 20m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Quiet Level", "Maximum ADX value to allow entries", "Indicators");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration step for Parabolic SAR", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum acceleration for Parabolic SAR", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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

		_nextDirection = TradeDirection.Buy;
		_breakevenActivated = false;
		_pipSize = 0m;
		_positionDirection = 0;
		_exitOrderPending = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Pre-calculate pip size respecting fractional pricing conventions.
		_pipSize = CalculatePipSize();

		// Prepare indicators used for filtering and timing.
		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		_sar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};

		// Subscribe to candle stream and bind indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, _sar, ProcessCandle)
			.Start();

		// Draw everything on a chart if UI is attached.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sar);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue sarValue)
	{
		// Process only completed candles to stay close to bar-close logic from the EA.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Wait for both indicators to provide final values.
		if (!adxValue.IsFinal || !sarValue.IsFinal)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		// Make sure we do not send new commands until exit orders are filled.
		if (_exitOrderPending)
		{
			if (Position == 0)
			{
				_exitOrderPending = false;
				_positionDirection = 0;
				_breakevenActivated = false;
			}
			else
			{
				return;
			}
		}

		if (Position != 0)
		{
			var currentDirection = Position > 0 ? 1 : -1;
			if (_positionDirection != currentDirection)
			{
				_positionDirection = currentDirection;
				_breakevenActivated = false;
			}

			// Manage protective logic for the active trade.
			ManageOpenPosition(candle);
			if (_exitOrderPending || Position != 0)
				return;
		}
		else
		{
			_positionDirection = 0;
			_breakevenActivated = false;
		}

		var adxData = (AverageDirectionalIndexValue)adxValue;
		if (adxData.MovingAverage is not decimal adx)
			return;

		var sar = sarValue.ToDecimal();
		var close = candle.ClosePrice;
		var quietMarket = adx < AdxQuietLevel;

		// Follow original cmd logic: buy after losses or initialization, sell after profits.
		if ((_nextDirection == TradeDirection.Buy || _nextDirection == TradeDirection.None) && quietMarket && close > sar)
		{
			_breakevenActivated = false;
			BuyMarket(Volume + Math.Abs(Position));
			_positionDirection = 1;
		}
		else if (_nextDirection == TradeDirection.Sell && quietMarket && close < sar)
		{
			_breakevenActivated = false;
			SellMarket(Volume + Math.Abs(Position));
			_positionDirection = -1;
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		var direction = _positionDirection;
		var pip = _pipSize <= 0m ? 1m : _pipSize;

		if (direction > 0)
		{
			var stopPrice = StopLossPips > 0 ? entryPrice - StopLossPips * pip : decimal.MinValue;
			var takePrice = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * pip : decimal.MaxValue;

			// Activate the breakeven flag once price moves far enough in favor.
			if (!_breakevenActivated && BreakevenPips > 0)
			{
				var trigger = entryPrice + BreakevenPips * pip;
				if (candle.HighPrice >= trigger)
					_breakevenActivated = true;
			}

			var stopTriggered = (StopLossPips > 0 && candle.LowPrice <= stopPrice) || (_breakevenActivated && candle.LowPrice <= entryPrice);
			var takeTriggered = TakeProfitPips > 0 && candle.HighPrice >= takePrice;

			// Exit long positions on either stop or target, mirroring the EA logic.
			if (stopTriggered || takeTriggered)
			{
				SellMarket(Position);
				_exitOrderPending = true;
				UpdateNextDirection(takeTriggered && !stopTriggered, direction);
			}
		}
		else if (direction < 0)
		{
			var stopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * pip : decimal.MaxValue;
			var takePrice = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * pip : decimal.MinValue;

			// Activate the breakeven flag once the short trade gains enough.
			if (!_breakevenActivated && BreakevenPips > 0)
			{
				var trigger = entryPrice - BreakevenPips * pip;
				if (candle.LowPrice <= trigger)
					_breakevenActivated = true;
			}

			var stopTriggered = (StopLossPips > 0 && candle.HighPrice >= stopPrice) || (_breakevenActivated && candle.HighPrice >= entryPrice);
			var takeTriggered = TakeProfitPips > 0 && candle.LowPrice <= takePrice;

			// Exit short trades and adjust the direction scheduler.
			if (stopTriggered || takeTriggered)
			{
				BuyMarket(Math.Abs(Position));
				_exitOrderPending = true;
				UpdateNextDirection(takeTriggered && !stopTriggered, direction);
			}
		}
	}

	private void UpdateNextDirection(bool wasProfit, int direction)
	{
		if (direction > 0)
			_nextDirection = wasProfit ? TradeDirection.Sell : TradeDirection.Buy;
		else if (direction < 0)
			_nextDirection = wasProfit ? TradeDirection.Buy : TradeDirection.Sell;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 1m;

		var step = security.PriceStep ?? 1m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		var scale = (bits[3] >> 16) & 0x7F;
		return scale;
	}
}
