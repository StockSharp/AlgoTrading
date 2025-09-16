
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// GreenTrade strategy converted from the MQL implementation.
/// Combines a smoothed moving average slope filter with RSI momentum confirmation.
/// </summary>
public class GreenTradeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _shiftBar;
	private readonly StrategyParam<int> _shiftBar1;
	private readonly StrategyParam<int> _shiftBar2;
	private readonly StrategyParam<int> _shiftBar3;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _smma;
	private RelativeStrengthIndex _rsi;

	private readonly List<decimal> _maHistory = new();
	private readonly List<decimal> _rsiHistory = new();

	private decimal _pipSize = 1m;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Period for the smoothed moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Shift (in bars) used for the most recent MA/RSI sample.
	/// </summary>
	public int ShiftBar
	{
		get => _shiftBar.Value;
		set => _shiftBar.Value = value;
	}

	/// <summary>
	/// Additional shift between the first and second MA comparison.
	/// </summary>
	public int ShiftBar1
	{
		get => _shiftBar1.Value;
		set => _shiftBar1.Value = value;
	}

	/// <summary>
	/// Additional shift between the second and third MA comparison.
	/// </summary>
	public int ShiftBar2
	{
		get => _shiftBar2.Value;
		set => _shiftBar2.Value = value;
	}

	/// <summary>
	/// Additional shift between the third and fourth MA comparison.
	/// </summary>
	public int ShiftBar3
	{
		get => _shiftBar3.Value;
		set => _shiftBar3.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold to confirm bullish entries.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// RSI threshold to confirm bearish entries.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// Trade volume for each new position add-on.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
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
	/// Minimum price improvement (in pips) before trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of position units (in TradeVolume steps) allowed.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Candle type used for backtesting/live trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public GreenTradeStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 67)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of the smoothed moving average", "Indicators");

		_shiftBar = Param(nameof(ShiftBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Shift #0", "Index of the most recent evaluated bar", "Signals");

		_shiftBar1 = Param(nameof(ShiftBar1), 1)
			.SetGreaterThanZero()
			.SetDisplay("Shift #1", "Offset from bar #0 to bar #1", "Signals");

		_shiftBar2 = Param(nameof(ShiftBar2), 2)
			.SetGreaterThanZero()
			.SetDisplay("Shift #2", "Offset from bar #1 to bar #2", "Signals");

		_shiftBar3 = Param(nameof(ShiftBar3), 3)
			.SetGreaterThanZero()
			.SetDisplay("Shift #3", "Offset from bar #2 to bar #3", "Signals");

		_rsiPeriod = Param(nameof(RsiPeriod), 57)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI indicator", "Indicators");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 60m)
			.SetDisplay("RSI Buy Level", "RSI threshold for bullish entries", "Signals");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 36m)
			.SetDisplay("RSI Sell Level", "RSI threshold for bearish entries", "Signals");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume used for each new order", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 300m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Initial stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 300m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Initial take-profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 12m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step", "Required progress before trailing adjusts", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 7)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of volume units allowed", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle subscription", "Data");
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

		_maHistory.Clear();
		_rsiHistory.Clear();
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_smma = new SmoothedMovingAverage { Length = MaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smma);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
		var maValue = _smma.Process(medianPrice, candle.OpenTime, true).ToDecimal();
		var rsiValue = _rsi.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		_maHistory.Add(maValue);
		_rsiHistory.Add(rsiValue);
		TrimHistory();

		if (!_smma.IsFormed || !_rsi.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shift0 = ShiftBar;
		var shift1 = shift0 + ShiftBar1;
		var shift2 = shift1 + ShiftBar2;
		var shift3 = shift2 + ShiftBar3;

		var ma0 = GetHistoryValue(_maHistory, shift0);
		var ma1 = GetHistoryValue(_maHistory, shift1);
		var ma2 = GetHistoryValue(_maHistory, shift2);
		var ma3 = GetHistoryValue(_maHistory, shift3);
		var rsiSample = GetHistoryValue(_rsiHistory, ShiftBar);

		if (ma0 is null || ma1 is null || ma2 is null || ma3 is null || rsiSample is null)
			return;

		var buySignal = ma0 > ma1 && ma1 > ma2 && ma2 > ma3 && rsiSample > RsiBuyLevel;
		var sellSignal = ma0 < ma1 && ma1 < ma2 && ma2 < ma3 && rsiSample < RsiSellLevel;

		if (buySignal && CanIncreasePosition(true))
			OpenPosition(true, candle);
		else if (sellSignal && CanIncreasePosition(false))
			OpenPosition(false, candle);

		UpdateTrailing(candle);
		ManageExits(candle);
	}

	private void OpenPosition(bool isLong, ICandleMessage candle)
	{
		var additionalVolume = TradeVolume;
		var currentPosition = Position;

		if (isLong && currentPosition < 0)
			additionalVolume += Math.Abs(currentPosition);
		else if (!isLong && currentPosition > 0)
			additionalVolume += currentPosition;

		if (additionalVolume <= 0)
			return;

		if (isLong)
			BuyMarket(additionalVolume);
		else
			SellMarket(additionalVolume);

		if (isLong)
		{
			if (currentPosition > 0)
			{
				var total = currentPosition + TradeVolume;
				_entryPrice = total > 0 ? ((currentPosition * _entryPrice) + (TradeVolume * candle.ClosePrice)) / total : candle.ClosePrice;
			}
			else
			{
				_entryPrice = candle.ClosePrice;
			}
		}
		else
		{
			if (currentPosition < 0)
			{
				var total = Math.Abs(currentPosition) + TradeVolume;
				_entryPrice = total > 0 ? ((Math.Abs(currentPosition) * _entryPrice) + (TradeVolume * candle.ClosePrice)) / total : candle.ClosePrice;
			}
			else
			{
				_entryPrice = candle.ClosePrice;
			}
		}

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		_stopPrice = stopDistance > 0 ? (isLong ? _entryPrice - stopDistance : _entryPrice + stopDistance) : null;
		_takePrice = takeDistance > 0 ? (isLong ? _entryPrice + takeDistance : _entryPrice - takeDistance) : null;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;

		if (Position > 0 && _entryPrice > 0)
		{
			var profit = candle.ClosePrice - _entryPrice;
			if (profit > trailingDistance + stepDistance)
			{
				var threshold = candle.ClosePrice - (trailingDistance + stepDistance);
				if (!_stopPrice.HasValue || _stopPrice.Value < threshold)
					_stopPrice = candle.ClosePrice - trailingDistance;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var profit = _entryPrice - candle.ClosePrice;
			if (profit > trailingDistance + stepDistance)
			{
				var threshold = candle.ClosePrice + trailingDistance + stepDistance;
				if (!_stopPrice.HasValue || _stopPrice.Value > threshold)
					_stopPrice = candle.ClosePrice + trailingDistance;
			}
		}
	}

	private void ManageExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(volume);
				ResetPositionState();
				return;
			}

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(volume);
				ResetPositionState();
				return;
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private bool CanIncreasePosition(bool isLong)
	{
		if (TradeVolume <= 0)
			return false;

		if (MaxPositions <= 0)
			return true;

		var maxVolume = MaxPositions * TradeVolume;
		var absolutePosition = Math.Abs(Position);

		if (isLong && Position < 0)
			return true;

		if (!isLong && Position > 0)
			return true;

		var tolerance = Security?.VolumeStep ?? 0.0000001m;
		return absolutePosition + TradeVolume <= maxVolume + tolerance;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step < 0.01m)
			step *= 10m;
		return step;
	}

	private static decimal? GetHistoryValue(List<decimal> values, int shift)
	{
		if (shift <= 0)
			return null;

		var index = values.Count - shift;
		if (index < 0)
			return null;

		return values[index];
	}

	private void TrimHistory()
	{
		var maxShift = ShiftBar + ShiftBar1 + ShiftBar2 + ShiftBar3;
		var maxCount = Math.Max(maxShift + 5, 10);

		if (_maHistory.Count > maxCount)
			_maHistory.RemoveRange(0, _maHistory.Count - maxCount);

		if (_rsiHistory.Count > maxCount)
			_rsiHistory.RemoveRange(0, _rsiHistory.Count - maxCount);
	}

	private void ResetPositionState()
	{
		if (Math.Abs(Position) < (Security?.VolumeStep ?? 0.0000001m))
		{
			_entryPrice = 0m;
			_stopPrice = null;
			_takePrice = null;
		}
	}
}
