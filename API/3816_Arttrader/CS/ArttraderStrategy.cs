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
/// Port of the MetaTrader expert advisor "Arttrader v1.5".
/// Trades hourly candles using EMA slope filters, intrabar volatility suppression, and smart exit timing.
/// </summary>
public class ArttraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _bigJumpPips;
	private readonly StrategyParam<decimal> _doubleJumpPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _emergencyLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _slopeSmallPips;
	private readonly StrategyParam<decimal> _slopeLargePips;
	private readonly StrategyParam<decimal> _minutesBegin;
	private readonly StrategyParam<decimal> _minutesEnd;
	private readonly StrategyParam<decimal> _slipBeginPips;
	private readonly StrategyParam<decimal> _slipEndPips;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _spreadAdjustPips;
	private readonly StrategyParam<decimal> _slippagePips;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;

	private decimal _pipSize;
	private decimal? _previousEma;
	private decimal? _entryReferencePrice;
	private decimal? _longEmergencyStop;
	private decimal? _shortEmergencyStop;
	private decimal? _longTakeProfit;
	private decimal? _shortTakeProfit;
	private decimal _previousVolume;
	private decimal? _open1;
	private decimal? _open2;
	private decimal? _open3;
	private decimal? _open4;
	private decimal? _open5;
	private TimeSpan _timeFrame;

	/// <summary>
	/// Initializes a new instance of the <see cref="ArttraderStrategy"/> class.
	/// </summary>
	public ArttraderStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Executed order size in lots or contracts.", "Trading");

		_emaPeriod = Param(nameof(EmaPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the open-price EMA filter.", "Filters");

		_bigJumpPips = Param(nameof(BigJumpPips), 30m)
			.SetNotNegative()
			.SetDisplay("Big Jump (pips)", "Maximum single-bar open gap before signals are blocked.", "Filters");

		_doubleJumpPips = Param(nameof(DoubleJumpPips), 55m)
			.SetNotNegative()
			.SetDisplay("Double Jump (pips)", "Maximum two-bar open gap before signals are blocked.", "Filters");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetNotNegative()
			.SetDisplay("Smart Stop (pips)", "Distance in pips used by the timed stop logic.", "Risk");

		_emergencyLossPips = Param(nameof(EmergencyLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Emergency Stop (pips)", "Hard protective stop submitted together with the entry.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 25m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Fixed profit target placed after entries.", "Risk");

		_slopeSmallPips = Param(nameof(SlopeSmallPips), 5m)
			.SetNotNegative()
			.SetDisplay("Slope Min (pips)", "Minimum EMA advance to enable entries.", "Filters");

		_slopeLargePips = Param(nameof(SlopeLargePips), 8m)
			.SetNotNegative()
			.SetDisplay("Slope Max (pips)", "Maximum EMA advance to avoid overextended moves.", "Filters");

		_minutesBegin = Param(nameof(MinutesBegin), 25m)
			.SetNotNegative()
			.SetDisplay("Entry Delay (min)", "Minutes that must elapse inside the bar before entries are allowed.", "Timing");

		_minutesEnd = Param(nameof(MinutesEnd), 25m)
			.SetNotNegative()
			.SetDisplay("Exit Delay (min)", "Minutes that must elapse inside the bar before timed exits trigger.", "Timing");

		_slipBeginPips = Param(nameof(SlipBeginPips), 0m)
			.SetNotNegative()
			.SetDisplay("Entry Slip (pips)", "Allowance between the close and the extreme for entries.", "Filters");

		_slipEndPips = Param(nameof(SlipEndPips), 0m)
			.SetNotNegative()
			.SetDisplay("Exit Slip (pips)", "Allowance between the close and the extreme for exits.", "Filters");

		_minVolume = Param(nameof(MinVolume), 0m)
			.SetNotNegative()
			.SetDisplay("Min Volume", "If the previous candle volume is not above this value the trade is closed.", "Filters");

		_spreadAdjustPips = Param(nameof(SpreadAdjustPips), 1m)
			.SetNotNegative()
			.SetDisplay("Spread Adjust (pips)", "Imaginary spread compensation applied to the synthetic entry price.", "Trading");

		_slippagePips = Param(nameof(SlippagePips), 3m)
			.SetNotNegative()
			.SetDisplay("Slippage (pips)", "Broker slippage used only for reference, kept for compatibility.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that drives the EMA and signal calculations.", "General");
	}

	/// <summary>
	/// Order volume used for market executions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Length of the EMA calculated on candle open prices.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Maximum open-to-open jump in pips allowed between consecutive candles.
	/// </summary>
	public decimal BigJumpPips
	{
		get => _bigJumpPips.Value;
		set => _bigJumpPips.Value = value;
	}

	/// <summary>
	/// Maximum two-bar open gap in pips before signals are suspended.
	/// </summary>
	public decimal DoubleJumpPips
	{
		get => _doubleJumpPips.Value;
		set => _doubleJumpPips.Value = value;
	}

	/// <summary>
	/// Loss threshold in pips evaluated together with the exit timing filters.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Hard protective stop distance placed immediately after entries.
	/// </summary>
	public decimal EmergencyLossPips
	{
		get => _emergencyLossPips.Value;
		set => _emergencyLossPips.Value = value;
	}

	/// <summary>
	/// Profit target distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Minimum EMA slope (in pips) required to enable trades.
	/// </summary>
	public decimal SlopeSmallPips
	{
		get => _slopeSmallPips.Value;
		set => _slopeSmallPips.Value = value;
	}

	/// <summary>
	/// Maximum EMA slope (in pips) accepted before trades are rejected.
	/// </summary>
	public decimal SlopeLargePips
	{
		get => _slopeLargePips.Value;
		set => _slopeLargePips.Value = value;
	}

	/// <summary>
	/// Minutes required inside the active candle before entries are allowed.
	/// </summary>
	public decimal MinutesBegin
	{
		get => _minutesBegin.Value;
		set => _minutesBegin.Value = value;
	}

	/// <summary>
	/// Minutes required inside the active candle before timed exits are evaluated.
	/// </summary>
	public decimal MinutesEnd
	{
		get => _minutesEnd.Value;
		set => _minutesEnd.Value = value;
	}

	/// <summary>
	/// Entry slip allowance in pips.
	/// </summary>
	public decimal SlipBeginPips
	{
		get => _slipBeginPips.Value;
		set => _slipBeginPips.Value = value;
	}

	/// <summary>
	/// Exit slip allowance in pips.
	/// </summary>
	public decimal SlipEndPips
	{
		get => _slipEndPips.Value;
		set => _slipEndPips.Value = value;
	}

	/// <summary>
	/// Minimum acceptable volume for the previous candle.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Synthetic spread adjustment applied to the stored entry price.
	/// </summary>
	public decimal SpreadAdjustPips
	{
		get => _spreadAdjustPips.Value;
		set => _spreadAdjustPips.Value = value;
	}

	/// <summary>
	/// Reference slippage in pips kept for documentation purposes.
	/// </summary>
	public decimal SlippagePips
	{
		get => _slippagePips.Value;
		set => _slippagePips.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();
		_timeFrame = GetTimeFrame(CandleType);
		_previousEma = null;
		_entryReferencePrice = null;
		_longEmergencyStop = null;
		_shortEmergencyStop = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
		_previousVolume = 0m;
		_open1 = null;
		_open2 = null;
		_open3 = null;
		_open4 = null;
		_open5 = null;

		_ema = new ExponentialMovingAverage
		{
			Length = Math.Max(1, EmaPeriod),
			CandlePrice = CandlePrice.Open,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previousEma = _previousEma;
		_previousEma = emaValue;

		var currentOpen = candle.OpenPrice;
		var currentClose = candle.ClosePrice;
		var currentHigh = candle.HighPrice;
		var currentLow = candle.LowPrice;
		var currentVolume = candle.TotalVolume ?? candle.Volume ?? 0m;

		if (!_ema.IsFormed || previousEma is null)
		{
			UpdateHistory(currentOpen);
			_previousVolume = currentVolume;
			return;
		}

		if (ManageProtectiveStops(candle))
		{
			UpdateHistory(currentOpen);
			_previousVolume = currentVolume;
			return;
		}

		var slopePips = _pipSize > 0m ? (emaValue - previousEma.Value) / _pipSize : 0m;
		var durationMinutes = GetDurationMinutes(candle);
		var entryWindowReached = durationMinutes >= MinutesBegin;
		var exitWindowReached = durationMinutes >= MinutesEnd;

		var entrySlip = SlipBeginPips * _pipSize;
		var exitSlip = SlipEndPips * _pipSize;

		var beginLong = false;
		var beginShort = false;

		if (entryWindowReached)
		{
			if (slopePips >= SlopeSmallPips && slopePips <= SlopeLargePips &&
				currentClose <= currentOpen && currentClose <= currentLow + entrySlip)
			{
				beginLong = true;
			}

			if (slopePips <= -SlopeSmallPips && slopePips >= -SlopeLargePips &&
				currentClose >= currentOpen && currentClose >= currentHigh - entrySlip)
			{
				beginShort = true;
			}
		}

		if (HasVolatilitySpike(currentOpen))
		{
			beginLong = false;
			beginShort = false;
		}

		var endLong = false;
		var endShort = false;

		if (Position > 0 && _entryReferencePrice is decimal longEntry)
		{
			var lossPips = _pipSize > 0m ? (currentClose - longEntry) / _pipSize : 0m;
			if (lossPips <= -StopLossPips && exitWindowReached &&
				currentClose >= currentOpen && currentClose >= currentHigh - exitSlip)
			{
				endLong = true;
			}
		}

		if (Position < 0 && _entryReferencePrice is decimal shortEntry)
		{
			var lossPips = _pipSize > 0m ? (shortEntry - currentClose) / _pipSize : 0m;
			if (lossPips <= -StopLossPips && exitWindowReached &&
				currentClose <= currentOpen && currentClose <= currentLow + exitSlip)
			{
				endShort = true;
			}
		}

		if (_previousVolume <= MinVolume)
		{
			if (Position > 0)
				endLong = true;
			else if (Position < 0)
				endShort = true;
		}

		if (endLong && Position > 0)
		{
			SellMarket(Position);
			ClearPositionState();
		}
		else if (endShort && Position < 0)
		{
			BuyMarket(-Position);
			ClearPositionState();
		}

		if (Position != 0)
		{
			beginLong = false;
			beginShort = false;
		}

		if (beginLong && OrderVolume > 0m)
		{
			BuyMarket(OrderVolume);

			var adjust = SpreadAdjustPips * _pipSize;
			var reference = currentOpen - adjust;
			_entryReferencePrice = reference;
			_longEmergencyStop = EmergencyLossPips > 0m ? reference - (EmergencyLossPips * _pipSize) : null;
			_longTakeProfit = TakeProfitPips > 0m ? reference + (TakeProfitPips * _pipSize) : null;
			_shortEmergencyStop = null;
			_shortTakeProfit = null;
		}
		else if (beginShort && OrderVolume > 0m)
		{
			SellMarket(OrderVolume);

			var adjust = SpreadAdjustPips * _pipSize;
			var reference = currentOpen + adjust;
			_entryReferencePrice = reference;
			_shortEmergencyStop = EmergencyLossPips > 0m ? reference + (EmergencyLossPips * _pipSize) : null;
			_shortTakeProfit = TakeProfitPips > 0m ? reference - (TakeProfitPips * _pipSize) : null;
			_longEmergencyStop = null;
			_longTakeProfit = null;
		}

		UpdateHistory(currentOpen);
		_previousVolume = currentVolume;
	}

	private bool ManageProtectiveStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longEmergencyStop is decimal hardStop && candle.LowPrice <= hardStop)
			{
				SellMarket(Position);
				ClearPositionState();
				return true;
			}

			if (_longTakeProfit is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ClearPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_shortEmergencyStop is decimal hardStop && candle.HighPrice >= hardStop)
			{
				BuyMarket(-Position);
				ClearPositionState();
				return true;
			}

			if (_shortTakeProfit is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ClearPositionState();
				return true;
			}
		}

		return false;
	}

	private bool HasVolatilitySpike(decimal currentOpen)
	{
		var bigJump = BigJumpPips * _pipSize;
		var doubleJump = DoubleJumpPips * _pipSize;

		if (bigJump > 0m)
		{
			if (_open1 is decimal o1 && Math.Abs(o1 - currentOpen) >= bigJump)
				return true;
			if (_open2 is decimal o2 && _open1 is decimal prev1 && Math.Abs(o2 - prev1) >= bigJump)
				return true;
			if (_open3 is decimal o3 && _open2 is decimal prev2 && Math.Abs(o3 - prev2) >= bigJump)
				return true;
			if (_open4 is decimal o4 && _open3 is decimal prev3 && Math.Abs(o4 - prev3) >= bigJump)
				return true;
			if (_open5 is decimal o5 && _open4 is decimal prev4 && Math.Abs(o5 - prev4) >= bigJump)
				return true;
		}

		if (doubleJump > 0m)
		{
			if (_open2 is decimal o2 && Math.Abs(o2 - currentOpen) >= doubleJump)
				return true;
			if (_open3 is decimal o3 && _open1 is decimal prev1 && Math.Abs(o3 - prev1) >= doubleJump)
				return true;
			if (_open4 is decimal o4 && _open2 is decimal prev2 && Math.Abs(o4 - prev2) >= doubleJump)
				return true;
			if (_open5 is decimal o5 && _open3 is decimal prev3 && Math.Abs(o5 - prev3) >= doubleJump)
				return true;
		}

		return false;
	}

	private void UpdateHistory(decimal currentOpen)
	{
		_open5 = _open4;
		_open4 = _open3;
		_open3 = _open2;
		_open2 = _open1;
		_open1 = currentOpen;
	}

	private void ClearPositionState()
	{
		_entryReferencePrice = null;
		_longEmergencyStop = null;
		_shortEmergencyStop = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		return step > 0m ? step : 0m;
	}

	private static TimeSpan GetTimeFrame(DataType candleType)
	{
		if (candleType.Arg is TimeSpan span)
			return span;

		return TimeSpan.Zero;
	}

	private decimal GetDurationMinutes(ICandleMessage candle)
	{
		var openTime = candle.OpenTime;
		var closeTime = candle.CloseTime ?? openTime + _timeFrame;
		var duration = closeTime - openTime;
		return (decimal)duration.TotalMinutes;
	}
}

