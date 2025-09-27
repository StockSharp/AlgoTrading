namespace StockSharp.Samples.Strategies;

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

public class SpearmanRankCorrelationHistogramTimeWeekPeriodStrategy : Strategy
{
	public enum SpearmanTradeMode
	{
		Mode1 = 0,
		Mode2 = 1,
		Mode3 = 2,
	}

	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<MarginMode> _marginMode;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _deviationPoints;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<SpearmanTradeMode> _tradeMode;
	private readonly StrategyParam<bool> _timeTrade;
	private readonly StrategyParam<DayOfWeek> _startDay;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _startSecond;
	private readonly StrategyParam<DayOfWeek> _endDay;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<int> _endSecond;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _maxRange;
	private readonly StrategyParam<bool> _direction;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<int> _signalBar;

	private readonly List<decimal> _closeHistory = new();
	private readonly List<int?> _colorHistory = new();

	private TimeSpan _timeFrame;
	private DateTimeOffset? _nextBuyTime;
	private DateTimeOffset? _nextSellTime;
	private decimal? _entryPrice;
	private DateTimeOffset? _entryTime;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	public MarginMode MarginMode
	{
		get => _marginMode.Value;
		set => _marginMode.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal DeviationPoints
	{
		get => _deviationPoints.Value;
		set => _deviationPoints.Value = value;
	}

	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	public SpearmanTradeMode TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	public bool TimeTrade
	{
		get => _timeTrade.Value;
		set => _timeTrade.Value = value;
	}

	public DayOfWeek StartDay
	{
		get => _startDay.Value;
		set => _startDay.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	public int StartSecond
	{
		get => _startSecond.Value;
		set => _startSecond.Value = value;
	}

	public DayOfWeek EndDay
	{
		get => _endDay.Value;
		set => _endDay.Value = value;
	}

	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	public int EndSecond
	{
		get => _endSecond.Value;
		set => _endSecond.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	public int MaxRange
	{
		get => _maxRange.Value;
		set => _maxRange.Value = value;
	}

	public bool Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public SpearmanRankCorrelationHistogramTimeWeekPeriodStrategy()
	{
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
		.SetDisplay("Money Management", "Fraction of capital or direct lot size when positive.", "Trading");

		_marginMode = Param(nameof(MarginMode), MarginMode.Lot)
		.SetDisplay("Margin Mode", "Interpretation of the money management parameter.", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetDisplay("Stop Loss", "Protective stop distance in price points.", "Risk")
		.SetNotNegative();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetDisplay("Take Profit", "Profit target distance in price points.", "Risk")
		.SetNotNegative();

		_deviationPoints = Param(nameof(DeviationPoints), 10m)
		.SetDisplay("Deviation", "Expected slippage in points (informational).", "Risk")
		.SetNotNegative();

		_buyOpen = Param(nameof(BuyOpen), true)
		.SetDisplay("Allow Long Entries", "Enable opening long positions.", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
		.SetDisplay("Allow Short Entries", "Enable opening short positions.", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
		.SetDisplay("Allow Long Exits", "Allow closing long positions on signals.", "Trading");

		_sellClose = Param(nameof(SellClose), true)
		.SetDisplay("Allow Short Exits", "Allow closing short positions on signals.", "Trading");

		_tradeMode = Param(nameof(TradeMode), SpearmanTradeMode.Mode1)
		.SetDisplay("Trade Mode", "Signal interpretation mode for the histogram colors.", "Trading");

		_timeTrade = Param(nameof(TimeTrade), true)
		.SetDisplay("Use Weekly Window", "Restrict trading to the configured weekly session.", "Schedule");

		_startDay = Param(nameof(StartDay), DayOfWeek.Tuesday)
		.SetDisplay("Start Day", "Day of week when trading window opens.", "Schedule");

		_startHour = Param(nameof(StartHour), 8)
		.SetDisplay("Start Hour", "Hour when trading window opens.", "Schedule");

		_startMinute = Param(nameof(StartMinute), 0)
		.SetDisplay("Start Minute", "Minute when trading window opens.", "Schedule");

		_startSecond = Param(nameof(StartSecond), 0)
		.SetDisplay("Start Second", "Second when trading window opens.", "Schedule");

		_endDay = Param(nameof(EndDay), DayOfWeek.Friday)
		.SetDisplay("End Day", "Day of week when trading window closes.", "Schedule");

		_endHour = Param(nameof(EndHour), 20)
		.SetDisplay("End Hour", "Hour when trading window closes.", "Schedule");

		_endMinute = Param(nameof(EndMinute), 59)
		.SetDisplay("End Minute", "Minute when trading window closes.", "Schedule");

		_endSecond = Param(nameof(EndSecond), 40)
		.SetDisplay("End Second", "Second when trading window closes.", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe for the strategy.", "Data");

		_rangeLength = Param(nameof(RangeLength), 14)
		.SetDisplay("Range Length", "Number of closes used to compute the Spearman correlation.", "Indicator")
		.SetGreaterThanZero();

		_maxRange = Param(nameof(MaxRange), 30)
		.SetDisplay("Max Range", "Maximum allowed Spearman range length (safety limit).", "Indicator")
		.SetGreaterThanZero();

		_direction = Param(nameof(Direction), true)
		.SetDisplay("Series Direction", "Reserved flag from the MQL indicator (kept for compatibility).", "Indicator");

		_highLevel = Param(nameof(HighLevel), 0.5m)
		.SetDisplay("High Level", "Upper threshold for the histogram colors.", "Indicator");

		_lowLevel = Param(nameof(LowLevel), -0.5m)
		.SetDisplay("Low Level", "Lower threshold for the histogram colors.", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of closed bars to look back for the color signals.", "Indicator")
		.SetNotNegative();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_closeHistory.Clear();
		_colorHistory.Clear();
		_nextBuyTime = null;
		_nextSellTime = null;
		_entryPrice = null;
		_entryTime = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_timeFrame = GetTimeFrame(CandleType);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		_closeHistory.Add(candle.ClosePrice);

		var spearman = CalculateSpearmanForOffset(0);
		if (spearman.HasValue)
		{
			var color = DetermineColor(spearman.Value);
			_colorHistory.Add(color);
		}
		else
		{
			_colorHistory.Add(null);
		}

		var signalIndex = _colorHistory.Count - 1 - SignalBar;
		var previousIndex = signalIndex - 1;
		if (signalIndex < 0 || previousIndex < 0)
		{
			return;
		}

		var currentColor = _colorHistory[signalIndex];
		var previousColor = _colorHistory[previousIndex];
		if (currentColor is null || previousColor is null)
		{
			return;
		}

		var candleTime = candle.CloseTime;
		var insideWindow = !TimeTrade || IsWithinTradeWindow(candleTime);

		if (TimeTrade && !insideWindow)
		{
			ForceClosePositions();
			return;
		}

		ManageStopsAndTargets(candle);

		var (buyOpenSignal, sellOpenSignal, buyCloseSignal, sellCloseSignal) = EvaluateSignals(currentColor.Value, previousColor.Value);

		if (buyCloseSignal && Position > 0m && BuyClose)
		{
			SellMarket(Position);
			ResetPositionState();
		}

		if (sellCloseSignal && Position < 0m && SellClose)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
		}

		if (buyOpenSignal && Position <= 0m && BuyOpen && CanEnterLong(candleTime))
		{
			TryOpenLong(candle.ClosePrice, candleTime);
		}

		if (sellOpenSignal && Position >= 0m && SellOpen && CanEnterShort(candleTime))
		{
			TryOpenShort(candle.ClosePrice, candleTime);
		}
	}

	private (bool buyOpen, bool sellOpen, bool buyClose, bool sellClose) EvaluateSignals(int currentColor, int previousColor)
	{
		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		switch (TradeMode)
		{
			case SpearmanTradeMode.Mode1:
			{
				if (currentColor > 2)
				{
					if (previousColor < 3)
					{
						buyOpen = true;
					}

					sellClose = true;
				}

				if (currentColor < 2)
				{
					if (previousColor > 1)
					{
						sellOpen = true;
					}

					buyClose = true;
				}

				break;
			}
			case SpearmanTradeMode.Mode2:
			{
				if (currentColor == 4)
				{
					if (previousColor < 4)
					{
						buyOpen = true;
					}
				}

				if (currentColor > 2)
				{
					sellClose = true;
				}

				if (currentColor == 0)
				{
					if (previousColor > 0)
					{
						sellOpen = true;
					}
				}

				if (currentColor < 2)
				{
					buyClose = true;
				}

				break;
			}
			case SpearmanTradeMode.Mode3:
			{
				if (currentColor == 4)
				{
					if (previousColor < 4)
					{
						buyOpen = true;
					}

					sellClose = true;
				}

				if (currentColor == 0)
				{
					if (previousColor > 0)
					{
						sellOpen = true;
					}

					buyClose = true;
				}

				break;
			}
		}

		return (buyOpen, sellOpen, buyClose, sellClose);
	}

	private void TryOpenLong(decimal price, DateTimeOffset candleTime)
	{
		var volume = CalculateOrderVolume(price);
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);

		_entryPrice = price;
		_entryTime = candleTime;

		var step = Security?.PriceStep ?? 1m;

		_longStop = StopLossPoints > 0m ? price - StopLossPoints * step : null;
		_longTake = TakeProfitPoints > 0m ? price + TakeProfitPoints * step : null;
		_shortStop = null;
		_shortTake = null;

		_nextBuyTime = candleTime + _timeFrame;
	}

	private void TryOpenShort(decimal price, DateTimeOffset candleTime)
	{
		var volume = CalculateOrderVolume(price);
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);

		_entryPrice = price;
		_entryTime = candleTime;

		var step = Security?.PriceStep ?? 1m;

		_shortStop = StopLossPoints > 0m ? price + StopLossPoints * step : null;
		_shortTake = TakeProfitPoints > 0m ? price - TakeProfitPoints * step : null;
		_longStop = null;
		_longTake = null;

		_nextSellTime = candleTime + _timeFrame;
	}

	private void ManageStopsAndTargets(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var volume = Position;

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(volume);
				ResetPositionState();
				return;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(volume);
				ResetPositionState();
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(volume);
				ResetPositionState();
				return;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(volume);
				ResetPositionState();
			}
		}
	}

	private void ForceClosePositions()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
			ResetPositionState();
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
		}
	}

	private bool CanEnterLong(DateTimeOffset time)
	{
		return !_nextBuyTime.HasValue || time >= _nextBuyTime.Value;
	}

	private bool CanEnterShort(DateTimeOffset time)
	{
		return !_nextSellTime.HasValue || time >= _nextSellTime.Value;
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		var mm = MoneyManagement;
		if (mm == 0m)
		{
			return NormalizeVolume(Volume);
		}

		if (mm < 0m)
		{
			return NormalizeVolume(Math.Abs(mm));
		}

		if (Portfolio == null || price <= 0m)
		{
			return NormalizeVolume(Volume);
		}

		var capital = Portfolio.CurrentValue ?? 0m;
		if (capital <= 0m)
		{
			return NormalizeVolume(Volume);
		}

		decimal volume;

		switch (MarginMode)
		{
			case MarginMode.FreeMargin:
			case MarginMode.Balance:
			{
				var amount = capital * mm;
				volume = price > 0m ? amount / price : 0m;
				break;
			}
			case MarginMode.LossFreeMargin:
			case MarginMode.LossBalance:
			{
				var step = Security?.PriceStep ?? 1m;
				var risk = StopLossPoints * step;
				if (risk <= 0m)
				{
					return NormalizeVolume(Volume);
				}

				var lossAmount = capital * mm;
				volume = lossAmount / risk;
				break;
			}
			case MarginMode.Lot:
			default:
			{
				volume = mm;
				break;
			}
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		{
			return volume;
		}

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		{
			volume = minVolume;
		}

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		{
			volume = maxVolume;
		}

		return volume;
	}

	private decimal? CalculateSpearmanForOffset(int offset)
	{
		var range = RangeLength;
		var maxRange = MaxRange;
		if (maxRange > 0 && range > maxRange)
		{
			return null;
		}

		var required = range + offset;
		if (_closeHistory.Count < required)
		{
			return null;
		}

		var startIndex = _closeHistory.Count - required;
		var values = new decimal[range];
		for (var i = 0; i < range; i++)
		{
			var sourceIndex = startIndex + range - 1 - i;
			values[i] = _closeHistory[sourceIndex];
		}

		var ranks = CalculateRanks(values);
		var n = range;
		if (n <= 1)
		{
			return null;
		}

		decimal sum = 0m;
		for (var i = 0; i < n; i++)
		{
			var diff = ranks[i] - (i + 1);
			sum += diff * diff;
		}

		var denominator = n * n * n - n;
		if (denominator == 0m)
		{
			return null;
		}

		var result = 1m - 6m * sum / denominator;
		return result;
	}

	private static decimal[] CalculateRanks(decimal[] values)
	{
		var n = values.Length;
		var pairs = new (decimal value, int index)[n];
		for (var i = 0; i < n; i++)
		{
			pairs[i] = (values[i], i);
		}

		Array.Sort(pairs, (a, b) =>
		{
			var cmp = a.value.CompareTo(b.value);
			return cmp != 0 ? cmp : a.index.CompareTo(b.index);
		});

		var ranked = new decimal[n];
		var position = 0;
		while (position < n)
		{
			var end = position + 1;
			while (end < n && pairs[end].value == pairs[position].value)
			{
				end++;
			}

			decimal rankSum = 0m;
			for (var i = position; i < end; i++)
			{
				rankSum += i + 1;
			}

			var averageRank = rankSum / (end - position);
			for (var i = position; i < end; i++)
			{
				ranked[i] = averageRank;
			}

			position = end;
		}

		var result = new decimal[n];
		for (var i = 0; i < n; i++)
		{
			var originalIndex = pairs[i].index;
			result[originalIndex] = ranked[i];
		}

		return result;
	}

	private int DetermineColor(decimal value)
	{
		if (value > 0m)
		{
			if (value > HighLevel)
			{
				return 4;
			}

			return 3;
		}

		if (value < 0m)
		{
			if (value < LowLevel)
			{
				return 0;
			}

			return 1;
		}

		return 2;
	}

	private static TimeSpan GetTimeFrame(DataType candleType)
	{
		if (candleType?.Arg is TimeSpan span)
		{
			return span;
		}

		return TimeSpan.Zero;
	}

	private bool IsWithinTradeWindow(DateTimeOffset time)
	{
		var day = time.DayOfWeek;
		var startDay = StartDay;
		var endDay = EndDay;

		if (day < startDay || day > endDay)
		{
			return false;
		}

		if (day > startDay && day < endDay)
		{
			return true;
		}

		if (day == startDay)
		{
			if (time.Hour < StartHour)
			{
				return false;
			}

			if (time.Hour > StartHour)
			{
				return true;
			}

			if (time.Minute < StartMinute)
			{
				return false;
			}

			if (time.Minute > StartMinute)
			{
				return true;
			}

			return time.Second >= StartSecond;
		}

		if (day == endDay)
		{
			if (time.Hour > EndHour)
			{
				return false;
			}

			if (time.Hour < EndHour)
			{
				return true;
			}

			if (time.Minute > EndMinute)
			{
				return false;
			}

			if (time.Minute < EndMinute)
			{
				return true;
			}

			return time.Second < EndSecond;
		}

		return false;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_entryTime = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}
}

