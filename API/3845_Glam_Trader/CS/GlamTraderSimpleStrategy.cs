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
/// Simplified Glam Trader strategy combining multi-timeframe confirmation from EMA, Laguerre filter and Awesome Oscillator.
/// </summary>
public class GlamTraderSimpleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _laguerreCandleType;
	private readonly StrategyParam<DataType> _awesomeCandleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _laguerreGamma;
	private readonly StrategyParam<decimal> _longStopLossPoints;
	private readonly StrategyParam<decimal> _shortStopLossPoints;
	private readonly StrategyParam<decimal> _longTakeProfitPoints;
	private readonly StrategyParam<decimal> _shortTakeProfitPoints;
	private readonly StrategyParam<decimal> _longTrailingPoints;
	private readonly StrategyParam<decimal> _shortTrailingPoints;

	private decimal? _laguerreValue;
	private decimal? _laguerreClose;
	private decimal? _awesomeValue;
	private decimal? _awesomeClose;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingLevel;
	private decimal? _shortTrailingLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GlamTraderSimpleStrategy"/> class.
	/// </summary>
	public GlamTraderSimpleStrategy()
	{
		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Timeframe", "Candles used for EMA confirmation", "General");

		_laguerreCandleType = Param(nameof(LaguerreCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Laguerre Timeframe", "Candles processed by the Laguerre filter", "General");

		_awesomeCandleType = Param(nameof(AwesomeCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Awesome Timeframe", "Candles processed by the Awesome Oscillator", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size used for entries", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_emaPeriod = Param(nameof(EmaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the EMA used on the primary timeframe", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_laguerreGamma = Param(nameof(LaguerreGamma), 0.7m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Laguerre Gamma", "Smoothing factor applied inside the Laguerre filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.4m, 0.9m, 0.05m);

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Long Stop Loss (points)", "Protective distance for long positions", "Risk");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Short Stop Loss (points)", "Protective distance for short positions", "Risk");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Long Take Profit (points)", "Target distance for long positions", "Risk");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Short Take Profit (points)", "Target distance for short positions", "Risk");

		_longTrailingPoints = Param(nameof(LongTrailingPoints), 15m)
			.SetNotNegative()
			.SetDisplay("Long Trailing (points)", "Trailing stop distance for long positions", "Risk");

		_shortTrailingPoints = Param(nameof(ShortTrailingPoints), 15m)
			.SetNotNegative()
			.SetDisplay("Short Trailing (points)", "Trailing stop distance for short positions", "Risk");
	}

	/// <summary>
	/// Primary timeframe used for EMA analysis.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe feeding the Laguerre filter.
	/// </summary>
	public DataType LaguerreCandleType
	{
		get => _laguerreCandleType.Value;
		set => _laguerreCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe feeding the Awesome Oscillator.
	/// </summary>
	public DataType AwesomeCandleType
	{
		get => _awesomeCandleType.Value;
		set => _awesomeCandleType.Value = value;
	}

	/// <summary>
	/// Trade size submitted on new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// EMA period on the primary timeframe.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Laguerre filter gamma factor.
	/// </summary>
	public decimal LaguerreGamma
	{
		get => _laguerreGamma.Value;
		set => _laguerreGamma.Value = value;
	}

	/// <summary>
	/// Stop loss distance for long positions in instrument points.
	/// </summary>
	public decimal LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short positions in instrument points.
	/// </summary>
	public decimal ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance for long positions in instrument points.
	/// </summary>
	public decimal LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance for short positions in instrument points.
	/// </summary>
	public decimal ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for long positions in instrument points.
	/// </summary>
	public decimal LongTrailingPoints
	{
		get => _longTrailingPoints.Value;
		set => _longTrailingPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for short positions in instrument points.
	/// </summary>
	public decimal ShortTrailingPoints
	{
		get => _shortTrailingPoints.Value;
		set => _shortTrailingPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, PrimaryCandleType);

		if (!Equals(LaguerreCandleType, PrimaryCandleType))
			yield return (Security, LaguerreCandleType);

		if (!Equals(AwesomeCandleType, PrimaryCandleType) && !Equals(AwesomeCandleType, LaguerreCandleType))
			yield return (Security, AwesomeCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_laguerreValue = null;
		_laguerreClose = null;
		_awesomeValue = null;
		_awesomeClose = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingLevel = null;
		_shortTrailingLevel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var laguerre = new LaguerreFilter { Gamma = LaguerreGamma };
		var awesome = new AwesomeOscillator();

		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		primarySubscription
			.Bind(ema, ProcessPrimaryCandle)
			.Start();

		var laguerreSubscription = SubscribeCandles(LaguerreCandleType);
		laguerreSubscription
			.Bind(laguerre, ProcessLaguerreCandle)
			.Start();

		var awesomeSubscription = SubscribeCandles(AwesomeCandleType);
		awesomeSubscription
			.Bind(awesome, ProcessAwesomeCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, laguerre);
			DrawIndicator(area, awesome);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLaguerreCandle(ICandleMessage candle, decimal laguerreValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest Laguerre reading and closing price from the secondary timeframe.
		_laguerreValue = laguerreValue;
		_laguerreClose = candle.ClosePrice;
	}

	private void ProcessAwesomeCandle(ICandleMessage candle, decimal awesomeValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Keep the latest Awesome Oscillator reading and closing price from the hourly timeframe.
		_awesomeValue = awesomeValue;
		_awesomeClose = candle.ClosePrice;
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (ManageLongPosition(candle))
				return;
		}
		else if (Position < 0)
		{
			if (ManageShortPosition(candle))
				return;
		}

		if (Position != 0)
			return;

		if (_laguerreValue is not decimal laguerreValue ||
			_laguerreClose is not decimal laguerreClose ||
			_awesomeValue is not decimal awesomeValue ||
			_awesomeClose is not decimal awesomeClose)
		{
			return;
		}

		var currentClose = candle.ClosePrice;
		var volume = TradeVolume;

		// Buy when all three indicators sit above their respective prices.
		if (emaValue > currentClose &&
			laguerreValue > laguerreClose &&
			awesomeValue > awesomeClose)
		{
			BuyMarket(volume);
			LogInfo("All bullish filters aligned. Entering long position.");
		}
		// Sell when all indicators drop below their prices.
		else if (emaValue < currentClose &&
			laguerreValue < laguerreClose &&
			awesomeValue < awesomeClose)
		{
			SellMarket(volume);
			LogInfo("All bearish filters aligned. Entering short position.");
		}
	}

	private bool ManageLongPosition(ICandleMessage candle)
	{
		if (_longEntryPrice is null)
			return false;

		var step = Security?.PriceStep ?? 1m;
		var stopDistance = LongStopLossPoints * step;
		var takeDistance = LongTakeProfitPoints * step;
		var trailingDistance = LongTrailingPoints * step;

		// Check fixed take profit first.
		if (LongTakeProfitPoints > 0m && candle.HighPrice >= _longEntryPrice.Value + takeDistance)
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
			LogInfo("Take profit reached for long position.");
			return true;
		}

		// Check fixed stop loss.
		if (LongStopLossPoints > 0m && candle.LowPrice <= _longEntryPrice.Value - stopDistance)
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
			LogInfo("Stop loss triggered for long position.");
			return true;
		}

		if (LongTrailingPoints > 0m)
		{
			var activationLevel = _longEntryPrice.Value + trailingDistance;

			if (candle.ClosePrice >= activationLevel)
			{
				var newTrail = candle.ClosePrice - trailingDistance;
				if (_longTrailingLevel is null || newTrail > _longTrailingLevel)
				{
					_longTrailingLevel = newTrail;
					LogInfo($"Adjusting long trailing stop to {newTrail:F5}.");
				}
			}

			if (_longTrailingLevel is decimal trail && candle.LowPrice <= trail)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				LogInfo("Trailing stop hit for long position.");
				return true;
			}
		}

		return false;
	}

	private bool ManageShortPosition(ICandleMessage candle)
	{
		if (_shortEntryPrice is null)
			return false;

		var step = Security?.PriceStep ?? 1m;
		var stopDistance = ShortStopLossPoints * step;
		var takeDistance = ShortTakeProfitPoints * step;
		var trailingDistance = ShortTrailingPoints * step;

		// Check fixed take profit.
		if (ShortTakeProfitPoints > 0m && candle.LowPrice <= _shortEntryPrice.Value - takeDistance)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			LogInfo("Take profit reached for short position.");
			return true;
		}

		// Check fixed stop loss.
		if (ShortStopLossPoints > 0m && candle.HighPrice >= _shortEntryPrice.Value + stopDistance)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			LogInfo("Stop loss triggered for short position.");
			return true;
		}

		if (ShortTrailingPoints > 0m)
		{
			var activationLevel = _shortEntryPrice.Value - trailingDistance;

			if (candle.ClosePrice <= activationLevel)
			{
				var newTrail = candle.ClosePrice + trailingDistance;
				if (_shortTrailingLevel is null || newTrail < _shortTrailingLevel)
				{
					_shortTrailingLevel = newTrail;
					LogInfo($"Adjusting short trailing stop to {newTrail:F5}.");
				}
			}

			if (_shortTrailingLevel is decimal trail && candle.HighPrice >= trail)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				LogInfo("Trailing stop hit for short position.");
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			if (Position > 0)
			{
				_longEntryPrice = trade.Trade.Price;
				_longTrailingLevel = null;
			}
			else if (Position <= 0)
			{
				ResetLongState();
			}

			if (Position >= 0)
			{
				ResetShortState();
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (Position < 0)
			{
				_shortEntryPrice = trade.Trade.Price;
				_shortTrailingLevel = null;
			}
			else if (Position >= 0)
			{
				ResetShortState();
			}

			if (Position <= 0)
			{
				ResetLongState();
			}
		}
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longTrailingLevel = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortTrailingLevel = null;
	}
}

