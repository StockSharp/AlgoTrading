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

/// <summary>
/// Rectangle-based range strategy converted from the RectangleTest MetaTrader expert.
/// </summary>
public class RectangleTestStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _maxLosingTradesPerDay;
	private readonly StrategyParam<int> _rangeCandles;
	private readonly StrategyParam<decimal> _rectangleSizePercent;
	private readonly StrategyParam<bool> _useRiskMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<TimeSpan> _tradeStartTime;
	private readonly StrategyParam<TimeSpan> _tradeEndTime;
	private readonly StrategyParam<bool> _enableTimeClose;
	private readonly StrategyParam<TimeSpan> _timeClose;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;
	private SimpleMovingAverage _sma = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private DateTime _dailyLossDate;
	private int _dailyLossCount;

	private int _entryDirection;
	private decimal _entryPrice;
	private decimal _entryVolume;

	private int _pendingDirection;
	private decimal _pendingEntryPrice;

	private decimal? _pendingExitPrice;
	private decimal _previousPosition;

	public RectangleTestStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 45)
		.SetDisplay("Fast EMA Period", "Length of the fast EMA", "Indicators")
		.SetCanOptimize(true, 10, 100, 5);

		_smaPeriod = Param(nameof(SmaPeriod), 200)
		.SetDisplay("Slow SMA Period", "Length of the slow SMA", "Indicators")
		.SetCanOptimize(true, 50, 400, 10);

		_stopLossPoints = Param(nameof(StopLossPoints), 250)
		.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in price steps", "Risk")
		.SetCanOptimize(true, 50, 400, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 750)
		.SetDisplay("Take Profit (points)", "Take-profit distance expressed in price steps", "Risk")
		.SetCanOptimize(true, 100, 1000, 10);

		_maxLosingTradesPerDay = Param(nameof(MaxLosingTradesPerDay), 1)
		.SetDisplay("Max Losing Trades", "Maximum number of losing trades per day", "Risk");

		_rangeCandles = Param(nameof(RangeCandles), 10)
		.SetDisplay("Rectangle Candles", "Number of candles used to form the rectangle", "Logic")
		.SetCanOptimize(true, 5, 30, 1);

		_rectangleSizePercent = Param(nameof(RectangleSizePercent), 0.5m)
		.SetDisplay("Rectangle Size (%)", "Maximum rectangle height in percent", "Logic")
		.SetCanOptimize(true, 0.2m, 2m, 0.1m);

		_useRiskMoneyManagement = Param(nameof(UseRiskMoneyManagement), true)
		.SetDisplay("Use Risk MM", "Switch between risk-based and fixed volume", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetDisplay("Risk Percent", "Risk per trade when risk-based money management is enabled", "Risk")
		.SetCanOptimize(true, 0.5m, 5m, 0.5m);

		_fixedVolume = Param(nameof(FixedVolume), 1m)
		.SetDisplay("Fixed Volume", "Fixed order volume when risk money management is disabled", "Risk")
		.SetCanOptimize(true, 0.1m, 5m, 0.1m);

		_tradeStartTime = Param(nameof(TradeStartTime), TimeSpan.FromHours(3))
		.SetDisplay("Trade Start", "Daily time when trading becomes active", "Timing");

		_tradeEndTime = Param(nameof(TradeEndTime), new TimeSpan(22, 50, 0))
		.SetDisplay("Trade End", "Daily time when trading stops generating new entries", "Timing");

		_enableTimeClose = Param(nameof(EnableTimeClose), false)
		.SetDisplay("Close By Time", "Enable end-of-day liquidation", "Timing");

		_timeClose = Param(nameof(TimeClose), new TimeSpan(23, 0, 0))
		.SetDisplay("Liquidation Time", "Time of day when all positions are closed", "Timing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle source", "General");
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int MaxLosingTradesPerDay
	{
		get => _maxLosingTradesPerDay.Value;
		set => _maxLosingTradesPerDay.Value = value;
	}

	public int RangeCandles
	{
		get => _rangeCandles.Value;
		set => _rangeCandles.Value = value;
	}

	public decimal RectangleSizePercent
	{
		get => _rectangleSizePercent.Value;
		set => _rectangleSizePercent.Value = value;
	}

	public bool UseRiskMoneyManagement
	{
		get => _useRiskMoneyManagement.Value;
		set => _useRiskMoneyManagement.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	public TimeSpan TradeStartTime
	{
		get => _tradeStartTime.Value;
		set => _tradeStartTime.Value = value;
	}

	public TimeSpan TradeEndTime
	{
		get => _tradeEndTime.Value;
		set => _tradeEndTime.Value = value;
	}

	public bool EnableTimeClose
	{
		get => _enableTimeClose.Value;
		set => _enableTimeClose.Value = value;
	}

	public TimeSpan TimeClose
	{
		get => _timeClose.Value;
		set => _timeClose.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_dailyLossDate = default;
		_dailyLossCount = 0;

		_entryDirection = 0;
		_entryPrice = 0m;
		_entryVolume = 0m;

		_pendingDirection = 0;
		_pendingEntryPrice = 0m;

		_pendingExitPrice = null;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_sma = new SimpleMovingAverage { Length = SmaPeriod };
		_highest = new Highest { Length = RangeCandles, CandlePrice = CandlePrice.High };
		_lowest = new Lowest { Length = RangeCandles, CandlePrice = CandlePrice.Low };

		Volume = FixedVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ema, _sma, _highest, _lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal smaValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ResetDailyLossCounter(candle.CloseTime);

		if (EnableTimeClose && Position != 0m && IsLiquidationTime(candle.CloseTime))
		{
			PrepareExit(candle.ClosePrice);
			ClosePosition();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_ema.IsFormed || !_sma.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		return;

		if (MaxLosingTradesPerDay > 0 && _dailyLossCount >= MaxLosingTradesPerDay)
		return;

		if (!IsWithinTradingTime(candle.CloseTime))
		return;

		if (highestValue <= 0m)
		return;

		var rangePercent = (highestValue - lowestValue) / highestValue * 100m;
		var rectangleValid = rangePercent < RectangleSizePercent;
		if (!rectangleValid)
		return;

		var emaInsideRectangle = emaValue > lowestValue && emaValue < highestValue;
		var smaInsideRectangle = smaValue > lowestValue && smaValue < highestValue;
		var priceInsideRectangle = candle.ClosePrice > lowestValue && candle.ClosePrice < highestValue;

		if (!emaInsideRectangle || !smaInsideRectangle || !priceInsideRectangle)
		return;

		if (emaValue > smaValue && candle.ClosePrice > emaValue && Position >= 0m)
		{
			if (Position > 0m)
			PrepareExit(candle.ClosePrice);

			var volume = CalculateTradeVolume();
			if (volume <= 0m)
			return;

			var resultingVolume = volume + Math.Max(Position, 0m);
			SellMarket(resultingVolume);
			RegisterEntry(-1, candle.ClosePrice, resultingVolume, candle.CloseTime);
		}
		else if (emaValue < smaValue && candle.ClosePrice < emaValue && Position <= 0m)
		{
			if (Position < 0m)
			PrepareExit(candle.ClosePrice);

			var volume = CalculateTradeVolume();
			if (volume <= 0m)
			return;

			var resultingVolume = volume + Math.Abs(Position);
			BuyMarket(resultingVolume);
			RegisterEntry(1, candle.ClosePrice, resultingVolume, candle.CloseTime);
		}
	}

	private void PrepareExit(decimal price)
	{
		_pendingExitPrice = price;
	}

	private void RegisterEntry(int direction, decimal price, decimal volume, DateTimeOffset time)
	{
		if (direction == 0 || volume <= 0m)
		return;

		ResetDailyLossCounter(time);

		_pendingDirection = direction;
		_pendingEntryPrice = price;
		_pendingExitPrice = null;
	}

	private decimal CalculateTradeVolume()
	{
		if (!UseRiskMoneyManagement)
		return FixedVolume;

		if (StopLossPoints <= 0)
		return FixedVolume;

		if (Portfolio == null || Security == null)
		return FixedVolume;

		var step = Security.PriceStep;
		var stepPrice = Security.StepPrice;
		if (step == null || stepPrice == null)
		return FixedVolume;

		var stopDistance = StopLossPoints * step.Value;
		if (stopDistance <= 0m)
		return FixedVolume;

		var portfolioValue = Portfolio.CurrentValue;
		if (portfolioValue <= 0m)
		return FixedVolume;

		var riskAmount = portfolioValue * RiskPercent / 100m;
		if (riskAmount <= 0m)
		return FixedVolume;

		var moneyPerUnit = stepPrice.Value / step.Value;
		if (moneyPerUnit <= 0m)
		return FixedVolume;

		var volume = riskAmount / (stopDistance * moneyPerUnit);
		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security == null)
		return volume;

		var minVolume = Security.MinVolume ?? 0m;
		var maxVolume = Security.MaxVolume ?? decimal.MaxValue;
		var stepVolume = Security.VolumeStep ?? 0m;

		if (stepVolume > 0m)
		volume = Math.Floor(volume / stepVolume) * stepVolume;

		if (volume < minVolume)
		volume = 0m;
		else if (volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private bool IsWithinTradingTime(DateTimeOffset time)
	{
		var localTime = time.LocalDateTime.TimeOfDay;
		var start = TradeStartTime;
		var end = TradeEndTime;

		if (start <= end)
		return localTime >= start && localTime <= end;

		return localTime >= start || localTime <= end;
	}

	private bool IsLiquidationTime(DateTimeOffset time)
	{
		var localTime = time.LocalDateTime.TimeOfDay;
		return localTime >= TimeClose;
	}

	private void ResetDailyLossCounter(DateTimeOffset time)
	{
		var day = time.Date;
		if (day == _dailyLossDate)
		return;

		_dailyLossDate = day;
		_dailyLossCount = 0;
	}

	private void ApplyProtection(int direction, decimal price, decimal volume)
	{
		if (StopLossPoints <= 0 && TakeProfitPoints <= 0)
		return;

		if (volume <= 0m)
		return;

		var resultingPosition = direction > 0 ? volume : -volume;

		if (TakeProfitPoints > 0)
		SetTakeProfit(TakeProfitPoints, price, resultingPosition);

		if (StopLossPoints > 0)
		SetStopLoss(StopLossPoints, price, resultingPosition);
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (_previousPosition == 0m && Position != 0m && _pendingDirection != 0)
		{
			_entryDirection = _pendingDirection;
			_entryPrice = _pendingEntryPrice;
			_entryVolume = Math.Abs(Position);

			ApplyProtection(_entryDirection, _entryPrice, _entryVolume);

			_pendingDirection = 0;
			_pendingEntryPrice = 0m;
		}

		if (_previousPosition != 0m && Position == 0m && _entryDirection != 0)
		{
			if (_pendingExitPrice.HasValue)
			{
				var exitPrice = _pendingExitPrice.Value;
				var pnl = _entryDirection > 0 ? exitPrice - _entryPrice : _entryPrice - exitPrice;
				if (pnl < 0m)
				_dailyLossCount++;
			}

			_entryDirection = 0;
			_entryPrice = 0m;
			_entryVolume = 0m;
			_pendingExitPrice = null;
		}

		if (Position == 0m)
		{
			_pendingDirection = 0;
			_pendingEntryPrice = 0m;
		}

		_previousPosition = Position;
	}
}

