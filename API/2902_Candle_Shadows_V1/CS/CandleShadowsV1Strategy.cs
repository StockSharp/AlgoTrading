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
/// Candle shadow reversal strategy with session filter and trailing management.
/// </summary>
public class CandleShadowsV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _pipValue;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _positionLivesBars;
	private readonly StrategyParam<int> _closeProfitsOnBar;
	private readonly StrategyParam<int> _openWithinMinutes;
	private readonly StrategyParam<decimal> _candleSizeMinPips;
	private readonly StrategyParam<decimal> _oppositeShadowMaxPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _lossReductionFactor;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;

	private TimeSpan _timeFrame;
	private DateTimeOffset? _lastEntryCandle;
	private DateTimeOffset? _entryCandleTime;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal? _trailingStopPrice;
	private int _currentDirection;
	private bool _lastExitWasLoss;

	/// <summary>
	/// Value of one pip.
	/// </summary>
	public decimal PipValue
	{
		get => _pipValue.Value;
		set => _pipValue.Value = value;
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
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of bars a position can remain open.
	/// </summary>
	public int PositionLivesBars
	{
		get => _positionLivesBars.Value;
		set => _positionLivesBars.Value = value;
	}

	/// <summary>
	/// Close profitable trades after this many bars.
	/// </summary>
	public int CloseProfitsOnBar
	{
		get => _closeProfitsOnBar.Value;
		set => _closeProfitsOnBar.Value = value;
	}

	/// <summary>
	/// Minutes from candle open when new trades are allowed.
	/// </summary>
	public int OpenWithinMinutes
	{
		get => _openWithinMinutes.Value;
		set => _openWithinMinutes.Value = value;
	}

	/// <summary>
	/// Minimum distance between open price and shadow in pips.
	/// </summary>
	public decimal CandleSizeMinPips
	{
		get => _candleSizeMinPips.Value;
		set => _candleSizeMinPips.Value = value;
	}

	/// <summary>
	/// Maximum length of the opposite shadow in pips.
	/// </summary>
	public decimal OppositeShadowMaxPips
	{
		get => _oppositeShadowMaxPips.Value;
		set => _oppositeShadowMaxPips.Value = value;
	}

	/// <summary>
	/// Session start hour (exchange time).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour (exchange time).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Factor used to reduce volume after a losing trade.
	/// </summary>
	public decimal LossReductionFactor
	{
		get => _lossReductionFactor.Value;
		set => _lossReductionFactor.Value = value;
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
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleShadowsV1Strategy"/> class.
	/// </summary>
	public CandleShadowsV1Strategy()
	{
		_pipValue = Param(nameof(PipValue), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Value", "Value of one pip", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Pips", "Stop loss distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit Pips", "Take profit distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop Pips", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step Pips", "Minimum step for trailing", "Risk");

		_positionLivesBars = Param(nameof(PositionLivesBars), 4)
			.SetNotNegative()
			.SetDisplay("Position Lives Bars", "Maximum bars to keep position", "Risk");

		_closeProfitsOnBar = Param(nameof(CloseProfitsOnBar), 2)
			.SetNotNegative()
			.SetDisplay("Close Profits Bars", "Close profitable trades after bars", "Risk");

		_openWithinMinutes = Param(nameof(OpenWithinMinutes), 7)
			.SetGreaterThanZero()
			.SetDisplay("Open Within Minutes", "Allow entries within N minutes", "General");

		_candleSizeMinPips = Param(nameof(CandleSizeMinPips), 15m)
			.SetNotNegative()
			.SetDisplay("Candle Size Min", "Minimum shadow length", "Price Action");

		_oppositeShadowMaxPips = Param(nameof(OppositeShadowMaxPips), 1m)
			.SetNotNegative()
			.SetDisplay("Opposite Shadow Max", "Maximum opposite shadow", "Price Action");

		_startHour = Param(nameof(StartHour), 6)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Session start hour", "Session");

		_endHour = Param(nameof(EndHour), 18)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Session end hour", "Session");

		_lossReductionFactor = Param(nameof(LossReductionFactor), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Loss Reduction", "Volume reduction after loss", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Default order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_lastEntryCandle = null;
		_lastExitWasLoss = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		_timeFrame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageOpenPosition(candle);

		if (!CanEnterOnCandle(candle))
			return;

		var pip = ResolvePipValue();
		var minBody = CandleSizeMinPips * pip;
		var maxOppositeShadow = OppositeShadowMaxPips * pip;

		var upperShadow = Math.Max(0m, candle.HighPrice - candle.OpenPrice);
		var lowerShadow = Math.Max(0m, candle.OpenPrice - candle.LowPrice);

		var longSetup = upperShadow <= maxOppositeShadow && lowerShadow >= minBody;
		var shortSetup = lowerShadow <= maxOppositeShadow && upperShadow >= minBody;

		if (longSetup)
		{
			var volume = GetEntryVolume();
			if (volume <= 0m)
				return;

			BuyMarket(volume);
			OnEntered(candle, true);
		}
		else if (shortSetup)
		{
			var volume = GetEntryVolume();
			if (volume <= 0m)
				return;

			SellMarket(volume);
			OnEntered(candle, false);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0 || _currentDirection == 0)
			return;

		if (_entryCandleTime.HasValue && _timeFrame.TotalSeconds > 0)
		{
			var delta = candle.OpenTime - _entryCandleTime.Value;
			var barsSinceEntry = (int)(delta.TotalSeconds / _timeFrame.TotalSeconds);

			if (PositionLivesBars > 0 && barsSinceEntry > PositionLivesBars)
			{
				ClosePosition(candle.ClosePrice);
				return;
			}

			if (CloseProfitsOnBar > 0 && barsSinceEntry > CloseProfitsOnBar)
			{
				var profit = _currentDirection > 0 ? candle.ClosePrice - _entryPrice : _entryPrice - candle.ClosePrice;
				if (profit > 0m)
				{
					ClosePosition(candle.ClosePrice);
					return;
				}
			}
		}

		var pip = ResolvePipValue();
		var stopLossDistance = StopLossPips * pip;
		var takeProfitDistance = TakeProfitPips * pip;
		var trailingDistance = TrailingStopPips * pip;
		var trailingStep = TrailingStepPips * pip;

		if (_currentDirection > 0)
		{
			if (stopLossDistance > 0m && candle.LowPrice <= _entryPrice - stopLossDistance)
			{
				ClosePosition(_entryPrice - stopLossDistance);
				return;
			}

			if (takeProfitDistance > 0m && candle.HighPrice >= _entryPrice + takeProfitDistance)
			{
				ClosePosition(_entryPrice + takeProfitDistance);
				return;
			}

			if (UpdateTrailingForLong(candle, trailingDistance, trailingStep))
				return;
		}
		else
		{
			if (stopLossDistance > 0m && candle.HighPrice >= _entryPrice + stopLossDistance)
			{
				ClosePosition(_entryPrice + stopLossDistance);
				return;
			}

			if (takeProfitDistance > 0m && candle.LowPrice <= _entryPrice - takeProfitDistance)
			{
				ClosePosition(_entryPrice - takeProfitDistance);
				return;
			}

			if (UpdateTrailingForShort(candle, trailingDistance, trailingStep))
				return;
		}
	}

	private bool CanEnterOnCandle(ICandleMessage candle)
	{
		if (Position != 0 || _currentDirection != 0)
			return false;

		if (!IsWithinSession(candle))
			return false;

		if (_lastEntryCandle.HasValue && _lastEntryCandle.Value == candle.OpenTime)
			return false;

		if (OpenWithinMinutes > 0)
		{
			var limit = candle.OpenTime.AddMinutes(OpenWithinMinutes);
			if (limit <= candle.CloseTime)
				return false;
		}

		return true;
	}

	private decimal ResolvePipValue()
	{
		if (PipValue > 0m)
			return PipValue;

		var step = Security?.Step ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private decimal GetEntryVolume()
	{
		var volume = BaseVolume;
		if (volume <= 0m)
			volume = 1m;

		if (_lastExitWasLoss && LossReductionFactor > 0m)
			volume /= LossReductionFactor;

		return volume;
	}

	private void OnEntered(ICandleMessage candle, bool isLong)
	{
		_entryCandleTime = candle.OpenTime;
		_entryPrice = candle.ClosePrice;
		_highestPrice = Math.Max(candle.ClosePrice, candle.HighPrice);
		_lowestPrice = Math.Min(candle.ClosePrice, candle.LowPrice);
		_trailingStopPrice = null;
		_currentDirection = isLong ? 1 : -1;
		_lastEntryCandle = candle.OpenTime;
		_lastExitWasLoss = false;
	}

	private void ClosePosition(decimal exitPrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			ResetTradeState();
			return;
		}

		if (Position > 0)
			SellMarket(volume);
		else
			BuyMarket(volume);

		if (_currentDirection != 0)
		{
			var profit = _currentDirection > 0 ? exitPrice - _entryPrice : _entryPrice - exitPrice;
			_lastExitWasLoss = profit <= 0m;
		}

		ResetTradeState();
	}

	private bool UpdateTrailingForLong(ICandleMessage candle, decimal trailingDistance, decimal trailingStep)
	{
		if (trailingDistance <= 0m)
			return false;

		if (candle.HighPrice > _highestPrice)
			_highestPrice = candle.HighPrice;

		var profitFromEntry = _highestPrice - _entryPrice;
		if (profitFromEntry >= trailingDistance)
		{
			var candidate = _highestPrice - trailingDistance;
			var minStep = trailingStep > 0m ? trailingStep : 0m;

			if (!_trailingStopPrice.HasValue || candidate - _trailingStopPrice.Value >= minStep)
				_trailingStopPrice = candidate;
		}

		if (_trailingStopPrice.HasValue && candle.LowPrice <= _trailingStopPrice.Value)
		{
			ClosePosition(_trailingStopPrice.Value);
			return true;
		}

		return false;
	}

	private bool UpdateTrailingForShort(ICandleMessage candle, decimal trailingDistance, decimal trailingStep)
	{
		if (trailingDistance <= 0m)
			return false;

		if (candle.LowPrice < _lowestPrice)
			_lowestPrice = candle.LowPrice;

		var profitFromEntry = _entryPrice - _lowestPrice;
		if (profitFromEntry >= trailingDistance)
		{
			var candidate = _lowestPrice + trailingDistance;
			var minStep = trailingStep > 0m ? trailingStep : 0m;

			if (!_trailingStopPrice.HasValue || _trailingStopPrice.Value - candidate >= minStep)
				_trailingStopPrice = candidate;
		}

		if (_trailingStopPrice.HasValue && candle.HighPrice >= _trailingStopPrice.Value)
		{
			ClosePosition(_trailingStopPrice.Value);
			return true;
		}

		return false;
	}

	private bool IsWithinSession(ICandleMessage candle)
	{
		var start = TimeSpan.FromHours(StartHour);
		var end = TimeSpan.FromHours(EndHour);
		var time = candle.OpenTime.TimeOfDay;

		if (end > start)
			return time >= start && time < end;

		return time >= start || time < end;
	}

	private void ResetTradeState()
	{
		_entryCandleTime = null;
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_trailingStopPrice = null;
		_currentDirection = 0;
	}
}