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
/// Strategy that replicates the BullsBearsEyes expert advisor behaviour.
/// The algorithm combines Bulls Power and Bears Power into a smoothed ratio.
/// When the ratio collapses to zero the expert buys, and when it jumps to one it sells.
/// Stop loss, take profit and trailing logic follow the original MQL implementation.
/// </summary>
public class BullsBearsEyesEaStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private BullPower _bulls;
	private BearPower _bears;

	private decimal _l0;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal? _previousRatio;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal _pipSize;

	/// <summary>
	/// Averaging period for Bulls Power and Bears Power.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Smoothing factor used by the four stage filter (0-1).
	/// </summary>
	public decimal Gamma
	{
		get => _gamma.Value;
		set => _gamma.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enables the trading session filter.
	/// </summary>
	public bool UseTimeControl
	{
		get => _useTimeControl.Value;
		set => _useTimeControl.Value = value;
	}

	/// <summary>
	/// First hour (inclusive) when trading is allowed.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last hour (exclusive) when trading is allowed.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
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
	/// Initializes a new instance of <see cref="BullsBearsEyesEaStrategy"/>.
	/// </summary>
	public BullsBearsEyesEaStrategy()
	{
		_period = Param(nameof(Period), 13)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Indicator averaging period", "Indicator")
			.SetCanOptimize(true);

		_gamma = Param(nameof(Gamma), 0.6m)
			.SetRange(0.1m, 1m)
			.SetDisplay("Gamma", "Smoothing factor (0-1)", "Indicator")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 150m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 25m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimal advance before trailing", "Risk")
			.SetCanOptimize(true);

		_useTimeControl = Param(nameof(UseTimeControl), true)
			.SetDisplay("Use Time Control", "Enable trading hours filter", "Session");

		_startHour = Param(nameof(StartHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Trading session start hour", "Session");

		_endHour = Param(nameof(EndHour), 16)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Trading session end hour (exclusive)", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
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

		_bulls = default;
		_bears = default;
		_l0 = _l1 = _l2 = _l3 = 0m;
		_previousRatio = null;
		_longStop = _longTake = _shortStop = _shortTake = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (StartHour < 0 || StartHour > 23)
			throw new InvalidOperationException("StartHour must be between 0 and 23.");

		if (EndHour < 0 || EndHour > 23)
			throw new InvalidOperationException("EndHour must be between 0 and 23.");

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("TrailingStepPips must be positive when trailing stop is used.");

		_bulls = new BullPower { Length = Period };
		_bears = new BearPower { Length = Period };
		_pipSize = GetPipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bulls, _bears, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bulls);
			DrawIndicator(area, _bears);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bullsValue, decimal bearsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (HandleRisk(candle))
			return;

		var (shouldBuy, shouldSell) = CalculateSignals(bullsValue, bearsValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsTradingTime(candle.OpenTime))
			return;

		if (shouldBuy)
			EnterLong();
		else if (shouldSell)
			EnterShort();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position > 0)
		{
			if (delta > 0)
				SetupLongRisk();

			_shortStop = null;
			_shortTake = null;
		}
		else if (Position < 0)
		{
			if (delta < 0)
				SetupShortRisk();

			_longStop = null;
			_longTake = null;
		}
		else
		{
			_longStop = _longTake = _shortStop = _shortTake = null;
		}
	}

	private (bool buy, bool sell) CalculateSignals(decimal bullsValue, decimal bearsValue)
	{
		var ratio = CalculateRatio(bullsValue, bearsValue);
		var previous = _previousRatio;
		_previousRatio = ratio;

		if (previous is not decimal prev)
			return (false, false);

		var shouldBuy = prev == 0m;
		var shouldSell = prev == 1m;

		return (shouldBuy, shouldSell);
	}

	private decimal? CalculateRatio(decimal bullsValue, decimal bearsValue)
	{
		var sum = bullsValue + bearsValue;
		var gamma = Gamma;

		var prevL0 = _l0;
		var prevL1 = _l1;
		var prevL2 = _l2;
		var prevL3 = _l3;

		// Recreate the four-stage IIR filter from the indicator.
		_l0 = ((1m - gamma) * sum) + (gamma * prevL0);
		_l1 = (-gamma * _l0) + prevL0 + (gamma * prevL1);
		_l2 = (-gamma * _l1) + prevL1 + (gamma * prevL2);
		_l3 = (-gamma * _l2) + prevL2 + (gamma * prevL3);

		var cu = 0m;
		var cd = 0m;

		if (_l0 >= _l1)
			cu = _l0 - _l1;
		else
			cd = _l1 - _l0;

		if (_l1 >= _l2)
			cu += _l1 - _l2;
		else
			cd += _l2 - _l1;

		if (_l2 >= _l3)
			cu += _l2 - _l3;
		else
			cd += _l3 - _l2;

		var denom = cu + cd;
		if (denom == 0m)
			return null;

		if (cu == 0m && cd > 0m)
			return 0m;

		if (cd == 0m && cu > 0m)
			return 1m;

		return cu / denom;
	}

	private void EnterLong()
	{
		if (Volume <= 0m)
			return;

		if (Position < 0)
			BuyMarket(-Position);

		if (Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			if (volume > 0m)
				BuyMarket(volume);
		}
	}

	private void EnterShort()
	{
		if (Volume <= 0m)
			return;

		if (Position > 0)
			SellMarket(Position);

		if (Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private bool HandleRisk(ICandleMessage candle)
	{
		var position = Position;

		if (position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(position);
				_longStop = _longTake = null;
				return true;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(position);
				_longStop = _longTake = null;
				return true;
			}

			UpdateLongTrailing(candle);
		}
		else if (position < 0)
		{
			var absPosition = -position;

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(absPosition);
				_shortStop = _shortTake = null;
				return true;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(absPosition);
				_shortStop = _shortTake = null;
				return true;
			}

			UpdateShortTrailing(candle);
		}

		return false;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m)
			return;

		var pip = EnsurePipSize();
		if (pip <= 0m)
			return;

		var trailingDistance = TrailingStopPips * pip;
		var trailingStep = TrailingStepPips * pip;
		var entry = PositionPrice;

		if (entry <= 0m)
			return;

		var advance = candle.ClosePrice - entry;
		if (advance <= trailingDistance + trailingStep)
			return;

		var newStop = candle.ClosePrice - trailingDistance;
		if (!_longStop.HasValue || _longStop.Value < newStop)
			_longStop = newStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m)
			return;

		var pip = EnsurePipSize();
		if (pip <= 0m)
			return;

		var trailingDistance = TrailingStopPips * pip;
		var trailingStep = TrailingStepPips * pip;
		var entry = PositionPrice;

		if (entry <= 0m)
			return;

		var advance = entry - candle.ClosePrice;
		if (advance <= trailingDistance + trailingStep)
			return;

		var newStop = candle.ClosePrice + trailingDistance;
		if (!_shortStop.HasValue || _shortStop.Value > newStop)
			_shortStop = newStop;
	}

	private void SetupLongRisk()
	{
		var pip = EnsurePipSize();
		if (pip <= 0m)
		{
			_longStop = _longTake = null;
			return;
		}

		var entry = PositionPrice;
		if (entry <= 0m)
		{
			_longStop = _longTake = null;
			return;
		}

		_longStop = StopLossPips > 0m ? entry - (StopLossPips * pip) : null;
		_longTake = TakeProfitPips > 0m ? entry + (TakeProfitPips * pip) : null;
	}

	private void SetupShortRisk()
	{
		var pip = EnsurePipSize();
		if (pip <= 0m)
		{
			_shortStop = _shortTake = null;
			return;
		}

		var entry = PositionPrice;
		if (entry <= 0m)
		{
			_shortStop = _shortTake = null;
			return;
		}

		_shortStop = StopLossPips > 0m ? entry + (StopLossPips * pip) : null;
		_shortTake = TakeProfitPips > 0m ? entry - (TakeProfitPips * pip) : null;
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		if (!UseTimeControl)
			return true;

		var hour = time.Hour;
		if (StartHour <= EndHour)
			return hour >= StartHour && hour < EndHour;

		return hour >= StartHour || hour < EndHour;
	}

	private decimal EnsurePipSize()
	{
		if (_pipSize <= 0m)
			_pipSize = GetPipSize();

		return _pipSize;
	}

	private decimal GetPipSize()
	{
		var step = Security?.Step ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}

