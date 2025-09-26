using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Synchronized hour breakout strategy converted from the JK_sinkhro MQL expert advisor.
/// Enters positions during specific hours when recent candles show directional dominance.
/// Implements adaptive exit logic with dual take-profit levels, stop-loss, and trailing stop control.
/// </summary>
public class SynchronizedHourBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _secondaryTakeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _analysisPeriod;
	private readonly StrategyParam<int> _hourOffset;
	private readonly StrategyParam<decimal> _maxActiveOrders;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<bool> _useRiskBasedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<int> _directionWindow = new();

	private int _bullishCount;
	private int _bearishCount;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _priceStep;
	private decimal _volumeStep;
	private decimal _minVolume;

	/// <summary>
	/// Primary take-profit target in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Secondary (faster) take-profit target in price steps.
	/// </summary>
	public decimal SecondaryTakeProfitPoints
	{
		get => _secondaryTakeProfitPoints.Value;
		set => _secondaryTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Number of recent candles to analyse.
	/// </summary>
	public int AnalysisPeriod
	{
		get => _analysisPeriod.Value;
		set => _analysisPeriod.Value = value;
	}

	/// <summary>
	/// Hour offset applied to the original 19:00 and 22:00 trading windows.
	/// </summary>
	public int HourOffset
	{
		get => _hourOffset.Value;
		set => _hourOffset.Value = value;
	}

	/// <summary>
	/// Maximum allowed count of active orders across the strategy.
	/// </summary>
	public decimal MaxActiveOrders
	{
		get => _maxActiveOrders.Value;
		set => _maxActiveOrders.Value = value;
	}

	/// <summary>
	/// Fixed trade volume when risk based position sizing is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Enables risk based position sizing using the portfolio cash value.
	/// </summary>
	public bool UseRiskBasedVolume
	{
		get => _useRiskBasedVolume.Value;
		set => _useRiskBasedVolume.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio cash to risk per trade when risk sizing is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedHourBreakoutStrategy"/> class.
	/// </summary>
	public SynchronizedHourBreakoutStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pts)", "Primary take-profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 200m, 10m);

		_secondaryTakeProfitPoints = Param(nameof(SecondaryTakeProfitPoints), 36m)
			.SetGreaterThanZero()
			.SetDisplay("Secondary TP (pts)", "Early take-profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 80m, 4m);

		_stopLossPoints = Param(nameof(StopLossPoints), 82m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pts)", "Stop-loss distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(40m, 160m, 10m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
			.SetDisplay("Trailing Stop (pts)", "Trailing stop distance", "Risk");

		_analysisPeriod = Param(nameof(AnalysisPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Analysis Period", "Number of candles used for direction count", "Signal");

		_hourOffset = Param(nameof(HourOffset), 2)
			.SetDisplay("Hour Offset", "Offset applied to 19:00 and 22:00 trading windows", "Signal");

		_maxActiveOrders = Param(nameof(MaxActiveOrders), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Max Active Orders", "Maximum simultaneously active orders", "Trading");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Default order volume", "Risk");

		_useRiskBasedVolume = Param(nameof(UseRiskBasedVolume), false)
			.SetDisplay("Use Risk Volume", "Enable risk based position sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Portfolio risk percentage per trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		_directionWindow.Clear();
		_bullishCount = 0;
		_bearishCount = 0;
		ResetPositionTracking();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		_volumeStep = Security?.VolumeStep ?? 0.01m;
		_minVolume = Security?.MinVolume ?? _volumeStep;
	
		if (_volumeStep <= 0m)
			_volumeStep = 0.01m;
		if (_minVolume <= 0m)
			_minVolume = _volumeStep;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManagePosition(candle);

		if (_directionWindow.Count >= AnalysisPeriod)
			TryEnter(candle);

		UpdateDirectionWindow(candle);
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var close = candle.ClosePrice;
			var stopPrice = _entryPrice - StopLossPoints * _priceStep;
			var primaryTarget = _entryPrice + TakeProfitPoints * _priceStep;
			var secondaryTarget = _entryPrice + SecondaryTakeProfitPoints * _priceStep;
			var trailingDistance = TrailingStopPoints * _priceStep;

			if (close > _highestPrice)
				_highestPrice = close;

			if (close <= stopPrice)
			{
				SellMarket(Position);
				ResetPositionTracking();
				return;
			}

			if (close >= secondaryTarget)
			{
				SellMarket(Position);
				ResetPositionTracking();
				return;
			}

			if (close >= primaryTarget)
			{
				SellMarket(Position);
				ResetPositionTracking();
				return;
			}

			if (TrailingStopPoints > 0m && _highestPrice - _entryPrice > trailingDistance)
			{
				var trailingLevel = _highestPrice - trailingDistance;
				if (close <= trailingLevel)
				{
					SellMarket(Position);
					ResetPositionTracking();
				}
			}
		}
		else if (Position < 0)
		{
			var close = candle.ClosePrice;
			var stopPrice = _entryPrice + StopLossPoints * _priceStep;
			var primaryTarget = _entryPrice - TakeProfitPoints * _priceStep;
			var secondaryTarget = _entryPrice - SecondaryTakeProfitPoints * _priceStep;
			var trailingDistance = TrailingStopPoints * _priceStep;

			if (_lowestPrice == 0m || close < _lowestPrice)
				_lowestPrice = close;

			if (close >= stopPrice)
			{
				BuyMarket(-Position);
				ResetPositionTracking();
				return;
			}

			if (close <= secondaryTarget)
			{
				BuyMarket(-Position);
				ResetPositionTracking();
				return;
			}

			if (close <= primaryTarget)
			{
				BuyMarket(-Position);
				ResetPositionTracking();
				return;
			}

			if (TrailingStopPoints > 0m && _entryPrice - _lowestPrice > trailingDistance)
			{
				var trailingLevel = _lowestPrice + trailingDistance;
				if (close >= trailingLevel)
				{
					BuyMarket(-Position);
					ResetPositionTracking();
				}
			}
		}
		else if (_entryPrice != 0m)
		{
			ResetPositionTracking();
		}
	}

	private void TryEnter(ICandleMessage candle)
	{
		if (Position != 0)
			return;

		var activeOrders = Orders.Count(o => o.State == OrderStates.Active || o.State == OrderStates.Pending);
		if (activeOrders >= MaxActiveOrders)
			return;

		var minute = candle.OpenTime.Minute;
		if (minute >= 5)
			return;

		var hour = candle.OpenTime.Hour;
		var buyHour = (22 + HourOffset) % 24;
		var sellHour = (19 + HourOffset) % 24;

		var volume = CalculateVolume();
		if (volume <= 0m)
			return;

		if (_bullishCount > _bearishCount && hour == buyHour)
		{
			BuyMarket(volume);
			return;
		}

		if (_bearishCount > _bullishCount && hour == sellHour)
		{
			SellMarket(volume);
		}
	}

	private void UpdateDirectionWindow(ICandleMessage candle)
	{
		var direction = 0;

		if (candle.ClosePrice > candle.OpenPrice)
			direction = 1;
		else if (candle.ClosePrice < candle.OpenPrice)
			direction = -1;

		_directionWindow.Enqueue(direction);

		if (direction > 0)
			_bullishCount++;
		else if (direction < 0)
			_bearishCount++;

		while (_directionWindow.Count > AnalysisPeriod)
		{
			var removed = _directionWindow.Dequeue();
			if (removed > 0)
				_bullishCount--;
			else if (removed < 0)
				_bearishCount--;
		}
	}

	private decimal CalculateVolume()
	{
		if (!UseRiskBasedVolume)
			return FixedVolume;

		var cash = Portfolio?.Cash ?? 0m;
		if (cash <= 0m)
			return FixedVolume;

		var stopDistance = StopLossPoints * _priceStep;
		if (stopDistance <= 0m)
			return FixedVolume;

		var riskAmount = cash * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return FixedVolume;

		var rawVolume = Math.Max(riskAmount / stopDistance, _minVolume);
		var steps = Math.Max(1m, Math.Round(rawVolume / _volumeStep, MidpointRounding.AwayFromZero));
		return steps * _volumeStep;
	}

	private void ResetPositionTracking()
	{
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Direction == null)
			return;

		if (Position > 0 && trade.Order.Side == Sides.Buy)
		{
			_entryPrice = trade.Trade.Price;
			_highestPrice = trade.Trade.Price;
			_lowestPrice = 0m;
		}
		else if (Position < 0 && trade.Order.Side == Sides.Sell)
		{
			_entryPrice = trade.Trade.Price;
			_lowestPrice = trade.Trade.Price;
			_highestPrice = 0m;
		}
		else if (Position == 0)
		{
			ResetPositionTracking();
		}
	}
}
