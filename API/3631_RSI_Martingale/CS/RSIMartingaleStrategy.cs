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
/// Port of the MetaTrader strategy RSI&Martingale1.5.
/// Uses RSI extremes to enter trades, closes on a 50 cross, and optionally applies a martingale recovery step.
/// Includes daily profit/loss control and hourly filters to avoid trading during specific sessions.
/// </summary>
public class RSIMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _barsForCondition;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _enableMartingaleGrowth;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<bool> _enableDailyTargets;
	private readonly StrategyParam<decimal> _dailyTargetPercent;
	private readonly StrategyParam<decimal> _dailyMaxLossPercent;
	private readonly StrategyParam<int> _dailyStartHour;
	private readonly StrategyParam<int> _dailyEndHour;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool>[] _hourFilters;

	private RelativeStrengthIndex _rsi = null!;
	private readonly List<decimal> _recentRsi = new();

	private decimal _pipSize;
	private decimal _lastClosePrice;

	private decimal _currentPositionVolume;
	private decimal _averagePrice;
	private Sides? _activeSide;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	private decimal? _lastClosedPnL;
	private decimal? _lastClosedVolume;
	private Sides? _lastClosedSide;

	private DateTime? _dailyBaselineDate;
	private decimal _dailyBaselineEquity;
	private bool _dailyTradingSuspended;

	/// <summary>
	/// Initializes a new instance of the <see cref="RSIMartingaleStrategy"/> class.
	/// </summary>
	public RSIMartingaleStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetDisplay("Initial Volume", "Base order volume", "Trading")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Number of periods for the RSI indicator", "Indicator")
			.SetCanOptimize(true);

		_barsForCondition = Param(nameof(BarsForCondition), 20)
			.SetDisplay("Bars For Extremes", "Number of finished candles used to detect RSI extremes", "Indicator")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 30m)
			.SetDisplay("Take Profit (pips)", "Fixed profit target distance; set to 0 to disable", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 15m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance; set to 0 to disable", "Risk")
			.SetCanOptimize(true);

		_enableMartingaleGrowth = Param(nameof(EnableMartingaleGrowth), true)
			.SetDisplay("Enable Martingale", "Increase volume after a losing trade", "Recovery");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
			.SetDisplay("Martingale Multiplier", "Multiplier applied to the previous losing volume", "Recovery")
			.SetCanOptimize(true);

		_enableDailyTargets = Param(nameof(EnableDailyTargets), false)
			.SetDisplay("Daily Targets", "Pause trading after reaching the configured daily gain or loss", "Risk");

		_dailyTargetPercent = Param(nameof(DailyTargetPercent), 1m)
			.SetDisplay("Daily Profit %", "Profit percentage that stops trading for the rest of the day", "Risk")
			.SetCanOptimize(true);

		_dailyMaxLossPercent = Param(nameof(DailyMaxLossPercent), 2m)
			.SetDisplay("Daily Loss %", "Loss percentage that stops trading for the rest of the day", "Risk")
			.SetCanOptimize(true);

		_dailyStartHour = Param(nameof(DailyStartHour), 0)
			.SetDisplay("Daily Control Start", "Hour when the daily profit/loss check becomes active", "Schedule");

		_dailyEndHour = Param(nameof(DailyEndHour), 22)
			.SetDisplay("Daily Control End", "Hour when the daily profit/loss check is no longer applied", "Schedule");

		_tradingStartHour = Param(nameof(TradingStartHour), 0)
			.SetDisplay("Trading Start", "First hour when new positions may be opened", "Schedule")
			.SetCanOptimize(true);

		_tradingEndHour = Param(nameof(TradingEndHour), 23)
			.SetDisplay("Trading End", "Last hour when new positions may be opened", "Schedule")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
			.SetDisplay("Candle Type", "Timeframe used for calculations", "Data");

		_hourFilters = new StrategyParam<bool>[24];
		for (var hour = 0; hour < 24; hour++)
		{
			var name = $"AvoidHour{hour:00}";
			_hourFilters[hour] = Param(name, false)
				.SetDisplay($"Avoid {hour:00}:00", "Skip trading during this hour", "Schedule");
		}
	}

	/// <summary>
	/// Order volume used for standard entries.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Period of the RSI indicator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles to evaluate when searching for RSI extremes.
	/// </summary>
	public int BarsForCondition
	{
		get => _barsForCondition.Value;
		set => _barsForCondition.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enables martingale growth after a losing trade.
	/// </summary>
	public bool EnableMartingaleGrowth
	{
		get => _enableMartingaleGrowth.Value;
		set => _enableMartingaleGrowth.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the previous losing volume.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Enables daily profit and loss boundaries.
	/// </summary>
	public bool EnableDailyTargets
	{
		get => _enableDailyTargets.Value;
		set => _enableDailyTargets.Value = value;
	}

	/// <summary>
	/// Daily profit percentage that halts trading.
	/// </summary>
	public decimal DailyTargetPercent
	{
		get => _dailyTargetPercent.Value;
		set => _dailyTargetPercent.Value = value;
	}

	/// <summary>
	/// Daily loss percentage that halts trading.
	/// </summary>
	public decimal DailyMaxLossPercent
	{
		get => _dailyMaxLossPercent.Value;
		set => _dailyMaxLossPercent.Value = value;
	}

	/// <summary>
	/// First hour when the daily profit/loss limits are evaluated.
	/// </summary>
	public int DailyStartHour
	{
		get => _dailyStartHour.Value;
		set => _dailyStartHour.Value = value;
	}

	/// <summary>
	/// Last hour when the daily profit/loss limits are evaluated.
	/// </summary>
	public int DailyEndHour
	{
		get => _dailyEndHour.Value;
		set => _dailyEndHour.Value = value;
	}

	/// <summary>
	/// First hour when new positions may be opened.
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Last hour when new positions may be opened.
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Avoid trading between 00:00 and 00:59.
	/// </summary>
	public bool AvoidHour00
	{
		get => _hourFilters[0].Value;
		set => _hourFilters[0].Value = value;
	}

	/// <summary>
	/// Avoid trading between 01:00 and 01:59.
	/// </summary>
	public bool AvoidHour01
	{
		get => _hourFilters[1].Value;
		set => _hourFilters[1].Value = value;
	}

	/// <summary>
	/// Avoid trading between 02:00 and 02:59.
	/// </summary>
	public bool AvoidHour02
	{
		get => _hourFilters[2].Value;
		set => _hourFilters[2].Value = value;
	}

	/// <summary>
	/// Avoid trading between 03:00 and 03:59.
	/// </summary>
	public bool AvoidHour03
	{
		get => _hourFilters[3].Value;
		set => _hourFilters[3].Value = value;
	}

	/// <summary>
	/// Avoid trading between 04:00 and 04:59.
	/// </summary>
	public bool AvoidHour04
	{
		get => _hourFilters[4].Value;
		set => _hourFilters[4].Value = value;
	}

	/// <summary>
	/// Avoid trading between 05:00 and 05:59.
	/// </summary>
	public bool AvoidHour05
	{
		get => _hourFilters[5].Value;
		set => _hourFilters[5].Value = value;
	}

	/// <summary>
	/// Avoid trading between 06:00 and 06:59.
	/// </summary>
	public bool AvoidHour06
	{
		get => _hourFilters[6].Value;
		set => _hourFilters[6].Value = value;
	}

	/// <summary>
	/// Avoid trading between 07:00 and 07:59.
	/// </summary>
	public bool AvoidHour07
	{
		get => _hourFilters[7].Value;
		set => _hourFilters[7].Value = value;
	}

	/// <summary>
	/// Avoid trading between 08:00 and 08:59.
	/// </summary>
	public bool AvoidHour08
	{
		get => _hourFilters[8].Value;
		set => _hourFilters[8].Value = value;
	}

	/// <summary>
	/// Avoid trading between 09:00 and 09:59.
	/// </summary>
	public bool AvoidHour09
	{
		get => _hourFilters[9].Value;
		set => _hourFilters[9].Value = value;
	}

	/// <summary>
	/// Avoid trading between 10:00 and 10:59.
	/// </summary>
	public bool AvoidHour10
	{
		get => _hourFilters[10].Value;
		set => _hourFilters[10].Value = value;
	}

	/// <summary>
	/// Avoid trading between 11:00 and 11:59.
	/// </summary>
	public bool AvoidHour11
	{
		get => _hourFilters[11].Value;
		set => _hourFilters[11].Value = value;
	}

	/// <summary>
	/// Avoid trading between 12:00 and 12:59.
	/// </summary>
	public bool AvoidHour12
	{
		get => _hourFilters[12].Value;
		set => _hourFilters[12].Value = value;
	}

	/// <summary>
	/// Avoid trading between 13:00 and 13:59.
	/// </summary>
	public bool AvoidHour13
	{
		get => _hourFilters[13].Value;
		set => _hourFilters[13].Value = value;
	}

	/// <summary>
	/// Avoid trading between 14:00 and 14:59.
	/// </summary>
	public bool AvoidHour14
	{
		get => _hourFilters[14].Value;
		set => _hourFilters[14].Value = value;
	}

	/// <summary>
	/// Avoid trading between 15:00 and 15:59.
	/// </summary>
	public bool AvoidHour15
	{
		get => _hourFilters[15].Value;
		set => _hourFilters[15].Value = value;
	}

	/// <summary>
	/// Avoid trading between 16:00 and 16:59.
	/// </summary>
	public bool AvoidHour16
	{
		get => _hourFilters[16].Value;
		set => _hourFilters[16].Value = value;
	}

	/// <summary>
	/// Avoid trading between 17:00 and 17:59.
	/// </summary>
	public bool AvoidHour17
	{
		get => _hourFilters[17].Value;
		set => _hourFilters[17].Value = value;
	}

	/// <summary>
	/// Avoid trading between 18:00 and 18:59.
	/// </summary>
	public bool AvoidHour18
	{
		get => _hourFilters[18].Value;
		set => _hourFilters[18].Value = value;
	}

	/// <summary>
	/// Avoid trading between 19:00 and 19:59.
	/// </summary>
	public bool AvoidHour19
	{
		get => _hourFilters[19].Value;
		set => _hourFilters[19].Value = value;
	}

	/// <summary>
	/// Avoid trading between 20:00 and 20:59.
	/// </summary>
	public bool AvoidHour20
	{
		get => _hourFilters[20].Value;
		set => _hourFilters[20].Value = value;
	}

	/// <summary>
	/// Avoid trading between 21:00 and 21:59.
	/// </summary>
	public bool AvoidHour21
	{
		get => _hourFilters[21].Value;
		set => _hourFilters[21].Value = value;
	}

	/// <summary>
	/// Avoid trading between 22:00 and 22:59.
	/// </summary>
	public bool AvoidHour22
	{
		get => _hourFilters[22].Value;
		set => _hourFilters[22].Value = value;
	}

	/// <summary>
	/// Avoid trading between 23:00 and 23:59.
	/// </summary>
	public bool AvoidHour23
	{
		get => _hourFilters[23].Value;
		set => _hourFilters[23].Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePipSize();

		_currentPositionVolume = 0m;
		_averagePrice = 0m;
		ResetTargets();

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, ProcessCandle).Start();

		StartProtection(useMarketOrders: true);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Trade is null || trade.Order is null)
			return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;
		var direction = trade.Order.Side;

		if (direction == Sides.Buy)
		{
			if (_currentPositionVolume >= 0m)
			{
				var previousVolume = _currentPositionVolume;
				_currentPositionVolume += volume;

				if (_currentPositionVolume > 0m)
				{
					if (previousVolume <= 0m)
						_averagePrice = price;
					else
						_averagePrice = ((previousVolume * _averagePrice) + volume * price) / _currentPositionVolume;

					_activeSide = Sides.Buy;
					UpdateTargets();
				}
			}
			else
			{
				var closingVolume = Math.Min(volume, Math.Abs(_currentPositionVolume));
				var profit = (_averagePrice - price) * closingVolume;
				_currentPositionVolume += volume;

				if (_currentPositionVolume >= 0m)
				{
					_lastClosedPnL = profit;
					_lastClosedVolume = closingVolume;
					_lastClosedSide = Sides.Sell;

					if (_currentPositionVolume > 0m)
					{
						_averagePrice = price;
						_activeSide = Sides.Buy;
						UpdateTargets();
					}
					else
					{
						_averagePrice = 0m;
						ResetTargets();
					}
				}
			}
		}
		else if (direction == Sides.Sell)
		{
			if (_currentPositionVolume <= 0m)
			{
				var previousVolume = Math.Abs(_currentPositionVolume);
				_currentPositionVolume -= volume;

				if (_currentPositionVolume < 0m)
				{
					if (previousVolume <= 0m)
						_averagePrice = price;
					else
					{
						var totalVolume = previousVolume + volume;
						_averagePrice = ((previousVolume * _averagePrice) + volume * price) / totalVolume;
						_currentPositionVolume = -totalVolume;
					}

					_activeSide = Sides.Sell;
					UpdateTargets();
				}
			}
			else
			{
				var closingVolume = Math.Min(volume, _currentPositionVolume);
				var profit = (price - _averagePrice) * closingVolume;
				_currentPositionVolume -= volume;

				if (_currentPositionVolume <= 0m)
				{
					_lastClosedPnL = profit;
					_lastClosedVolume = closingVolume;
					_lastClosedSide = Sides.Buy;

					if (_currentPositionVolume < 0m)
					{
						_averagePrice = price;
						_activeSide = Sides.Sell;
						UpdateTargets();
					}
					else
					{
						_averagePrice = 0m;
						ResetTargets();
					}
				}
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastClosePrice = candle.ClosePrice;

		UpdateDailyBaseline(candle.OpenTime, candle.ClosePrice);

		if (_rsi.IsFormed)
		{
			_recentRsi.Insert(0, rsiValue);
			while (_recentRsi.Count > BarsForCondition)
				_recentRsi.RemoveAt(_recentRsi.Count - 1);
		}

		if (EvaluateDailyTargets(candle))
			return;

		if (_dailyTradingSuspended)
			return;

		if (!_rsi.IsFormed || _recentRsi.Count < BarsForCondition)
			return;

		if (!IsTradingHourAllowed(candle.OpenTime))
			return;

		if (ShouldAvoidHour(candle.OpenTime.Hour))
			return;

		var currentRsi = rsiValue;

		HandleStopsAndExits(candle, currentRsi);

		if (Position != 0m)
			return;

		if (TryOpenMartingale())
			return;

		if (_lastClosedPnL is not null && _lastClosedPnL < 0m)
			return;

		var volume = AlignVolume(InitialVolume);
		if (volume <= 0m)
			return;

		if (IsCurrentMinimum() && currentRsi < 50m)
		{
			BuyMarket(volume);
			return;
		}

		if (IsCurrentMaximum() && currentRsi > 50m)
		{
			SellMarket(volume);
		}
	}

	private void HandleStopsAndExits(ICandleMessage candle, decimal currentRsi)
	{
		if (Position > 0m)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				ClosePosition();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				ClosePosition();
				return;
			}

			if (currentRsi > 50m)
				ClosePosition();
		}
		else if (Position < 0m)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				ClosePosition();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				ClosePosition();
				return;
			}

			if (currentRsi < 50m)
				ClosePosition();
		}
	}

	private bool TryOpenMartingale()
	{
		if (_lastClosedPnL is null || _lastClosedPnL >= 0m)
			return false;

		if (_lastClosedSide is null || _lastClosedVolume is null)
			return false;

		var volume = EnableMartingaleGrowth ? _lastClosedVolume.Value * MartingaleMultiplier : InitialVolume;
		volume = AlignVolume(volume);

		if (volume <= 0m)
			return false;

		if (_lastClosedSide == Sides.Buy)
		{
			SellMarket(volume);
		}
		else if (_lastClosedSide == Sides.Sell)
		{
			BuyMarket(volume);
		}
		else
		{
			return false;
		}

		_lastClosedPnL = 0m;
		_lastClosedVolume = null;
		_lastClosedSide = null;

		return true;
	}

	private bool EvaluateDailyTargets(ICandleMessage candle)
	{
		if (!EnableDailyTargets)
			return false;

		if (_dailyBaselineEquity <= 0m)
			return false;

		var hour = candle.OpenTime.Hour;
		if (hour < DailyStartHour || hour >= DailyEndHour)
			return false;

		var equity = CalculateEquity(candle.ClosePrice);
		if (_dailyBaselineEquity == 0m)
			return false;

		var profitPercent = ((equity - _dailyBaselineEquity) / _dailyBaselineEquity) * 100m;

		if (profitPercent >= DailyTargetPercent || profitPercent <= -DailyMaxLossPercent)
		{
			if (!_dailyTradingSuspended)
			{
				ClosePosition();
				_dailyTradingSuspended = true;
			}

			return true;
		}

		return false;
	}

	private void UpdateDailyBaseline(DateTimeOffset time, decimal closePrice)
	{
		var day = time.Date;
		if (_dailyBaselineDate == day)
			return;

		_dailyBaselineDate = day;
		_dailyBaselineEquity = CalculateEquity(closePrice);
		_dailyTradingSuspended = false;
	}

	private decimal CalculateEquity(decimal closePrice)
	{
		var baseValue = Portfolio?.BeginValue ?? 0m;
		var realized = PnL;
		var floating = 0m;

		if (Position != 0m && PositionPrice is decimal entryPrice)
			floating = Position * (closePrice - entryPrice);

		if (baseValue == 0m)
			return realized + floating;

		return baseValue + realized + floating;
	}

	private bool IsTradingHourAllowed(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= TradingStartHour && hour <= TradingEndHour;
	}

	private bool ShouldAvoidHour(int hour)
	{
		if (hour < 0 || hour > 23)
			return false;

		return _hourFilters[hour].Value;
	}

	private bool IsCurrentMinimum()
	{
		if (_recentRsi.Count == 0)
			return false;

		var current = _recentRsi[0];
		for (var i = 1; i < _recentRsi.Count; i++)
		{
			if (current > _recentRsi[i])
				return false;
		}

		return true;
	}

	private bool IsCurrentMaximum()
	{
		if (_recentRsi.Count == 0)
			return false;

		var current = _recentRsi[0];
		for (var i = 1; i < _recentRsi.Count; i++)
		{
			if (current < _recentRsi[i])
				return false;
		}

		return true;
	}

	private void InitializePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			_pipSize = 0m;
			return;
		}

		if (priceStep == 0.00001m || priceStep == 0.001m)
			_pipSize = priceStep * 10m;
		else
			_pipSize = priceStep;
	}

	private decimal PipToPrice(decimal pips)
	{
		if (_pipSize <= 0m)
			return 0m;

		return pips * _pipSize;
	}

	private void UpdateTargets()
	{
		if (_activeSide is null)
		{
			ResetTargets();
			return;
		}

		var stopDistance = PipToPrice(StopLossPips);
		var takeDistance = PipToPrice(TakeProfitPips);

		if (_activeSide == Sides.Buy)
		{
			_stopPrice = StopLossPips > 0m ? _averagePrice - stopDistance : null;
			_takePrice = TakeProfitPips > 0m ? _averagePrice + takeDistance : null;
		}
		else if (_activeSide == Sides.Sell)
		{
			_stopPrice = StopLossPips > 0m ? _averagePrice + stopDistance : null;
			_takePrice = TakeProfitPips > 0m ? _averagePrice - takeDistance : null;
		}
	}

	private void ResetTargets()
	{
		_stopPrice = null;
		_takePrice = null;
		_activeSide = null;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		var min = Security.VolumeMin ?? 0m;
		var max = Security.VolumeMax ?? decimal.MaxValue;

		if (step > 0m)
		{
			var ratio = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			if (ratio == 0m && volume > 0m)
				ratio = 1m;
			volume = ratio * step;
		}

		if (min > 0m && volume < min)
			volume = min;

		if (volume > max)
			volume = max;

		return volume;
	}
}

