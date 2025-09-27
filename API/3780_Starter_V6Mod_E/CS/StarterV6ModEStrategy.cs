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
/// Conversion of the MetaTrader 4 expert advisor "Starter_v6mod_e" using the high-level StockSharp API.
/// Combines Laguerre oscillator extremes, dual EMA momentum and a CCI filter with EMA-angle gating.
/// Friday evening safety rules block new positions and close existing ones ahead of the weekend.
/// </summary>
public class StarterV6ModEStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _angleEmaPeriod;
	private readonly StrategyParam<int> _angleStartShift;
	private readonly StrategyParam<int> _angleEndShift;
	private readonly StrategyParam<decimal> _angleThreshold;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciThreshold;
	private readonly StrategyParam<decimal> _laguerreGamma;
	private readonly StrategyParam<decimal> _laguerreOversold;
	private readonly StrategyParam<decimal> _laguerreOverbought;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fridayBlockHour;
	private readonly StrategyParam<int> _fridayExitHour;

	private ExponentialMovingAverage _slowEma = null!;
	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _angleEma = null!;
	private CommodityChannelIndex _cci = null!;

	private decimal? _previousSlowEma;
	private decimal? _previousFastEma;
	private decimal? _previousLaguerre;
	private bool _laguerreInitialized;

	private readonly List<decimal> _angleBuffer = [];

	private decimal _lagL0;
	private decimal _lagL1;
	private decimal _lagL2;
	private decimal _lagL3;

	private decimal? _entryPrice;
	private decimal? _highestPrice;
	private decimal? _lowestPrice;

	private decimal _pipSize;


	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Period for the slow EMA trend filter.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the fast EMA momentum filter.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the EMA used in the angle detector.
	/// </summary>
	public int AngleEmaPeriod
	{
		get => _angleEmaPeriod.Value;
		set => _angleEmaPeriod.Value = value;
	}

	/// <summary>
	/// Starting shift (in bars) for the EMA-angle calculation.
	/// </summary>
	public int AngleStartShift
	{
		get => _angleStartShift.Value;
		set => _angleStartShift.Value = value;
	}

	/// <summary>
	/// Ending shift (in bars) for the EMA-angle calculation.
	/// </summary>
	public int AngleEndShift
	{
		get => _angleEndShift.Value;
		set => _angleEndShift.Value = value;
	}

	/// <summary>
	/// Threshold in angle units required to confirm a trend bias.
	/// </summary>
	public decimal AngleThreshold
	{
		get => _angleThreshold.Value;
		set => _angleThreshold.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Absolute CCI threshold for momentum confirmation.
	/// </summary>
	public decimal CciThreshold
	{
		get => _cciThreshold.Value;
		set => _cciThreshold.Value = value;
	}

	/// <summary>
	/// Gamma parameter of the Laguerre oscillator.
	/// </summary>
	public decimal LaguerreGamma
	{
		get => _laguerreGamma.Value;
		set => _laguerreGamma.Value = value;
	}

	/// <summary>
	/// Oversold level of the Laguerre oscillator (0-1 scale).
	/// </summary>
	public decimal LaguerreOversold
	{
		get => _laguerreOversold.Value;
		set => _laguerreOversold.Value = value;
	}

	/// <summary>
	/// Overbought level of the Laguerre oscillator (0-1 scale).
	/// </summary>
	public decimal LaguerreOverbought
	{
		get => _laguerreOverbought.Value;
		set => _laguerreOverbought.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Hour after which Friday trading is blocked.
	/// </summary>
	public int FridayBlockHour
	{
		get => _fridayBlockHour.Value;
		set => _fridayBlockHour.Value = value;
	}

	/// <summary>
	/// Hour when all Friday positions are liquidated.
	/// </summary>
	public int FridayExitHour
	{
		get => _fridayExitHour.Value;
		set => _fridayExitHour.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StarterV6ModEStrategy"/>.
	/// </summary>
	public StarterV6ModEStrategy()
	{

		_stopLossPips = Param(nameof(StopLossPips), 35)
		.SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10)
		.SetDisplay("Take Profit", "Target distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 5);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
		.SetDisplay("Trailing Stop", "Trailing distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 80, 5);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 120)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA period (PRICE_MEDIAN)", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(60, 200, 10);

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 40)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA period (PRICE_MEDIAN)", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 10);

		_angleEmaPeriod = Param(nameof(AngleEmaPeriod), 34)
		.SetGreaterThanZero()
		.SetDisplay("Angle EMA", "EMA period for angle detector", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 80, 5);

		_angleStartShift = Param(nameof(AngleStartShift), 6)
		.SetGreaterThanZero()
		.SetDisplay("Angle Start Shift", "Older bar index used in angle calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(2, 12, 1);

		_angleEndShift = Param(nameof(AngleEndShift), 0)
		.SetDisplay("Angle End Shift", "Recent bar index used in angle calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);

		_angleThreshold = Param(nameof(AngleThreshold), 0.2m)
		.SetDisplay("Angle Threshold", "Minimum slope to allow trading", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.5m, 0.05m);

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 2);

		_cciThreshold = Param(nameof(CciThreshold), 5m)
		.SetDisplay("CCI Threshold", "Absolute CCI level for confirmation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(2m, 50m, 1m);

		_laguerreGamma = Param(nameof(LaguerreGamma), 0.7m)
		.SetDisplay("Laguerre Gamma", "Smoothing factor for Laguerre RSI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.4m, 0.9m, 0.05m);

		_laguerreOversold = Param(nameof(LaguerreOversold), 0.05m)
		.SetDisplay("Laguerre Oversold", "Entry level for longs (0-1)", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0m, 0.2m, 0.01m);

		_laguerreOverbought = Param(nameof(LaguerreOverbought), 0.95m)
		.SetDisplay("Laguerre Overbought", "Entry level for shorts (0-1)", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.8m, 1m, 0.01m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for signal calculations", "General");

		_fridayBlockHour = Param(nameof(FridayBlockHour), 18)
		.SetDisplay("Friday Block Hour", "Hour after which new trades stop", "Safety")
		.SetCanOptimize(true)
		.SetOptimize(16, 22, 1);

		_fridayExitHour = Param(nameof(FridayExitHour), 20)
		.SetDisplay("Friday Exit Hour", "Hour when open trades are closed", "Safety")
		.SetCanOptimize(true)
		.SetOptimize(18, 23, 1);
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

		_slowEma = null!;
		_fastEma = null!;
		_angleEma = null!;
		_cci = null!;

		_previousSlowEma = null;
		_previousFastEma = null;
		_previousLaguerre = null;
		_laguerreInitialized = false;

		_angleBuffer.Clear();

		_lagL0 = _lagL1 = _lagL2 = _lagL3 = 0m;

		_entryPrice = null;
		_highestPrice = null;
		_lowestPrice = null;

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_angleEma = new ExponentialMovingAverage { Length = AngleEmaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };

		_pipSize = Security?.PriceStep ?? 0.0001m;
		if (_pipSize <= 0m)
			_pipSize = 0.0001m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _angleEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;

		var slowValue = _slowEma.Process(medianPrice, candle.OpenTime, true);
		var fastValue = _fastEma.Process(medianPrice, candle.OpenTime, true);
		var angleValue = _angleEma.Process(medianPrice, candle.OpenTime, true);
		var cciValue = _cci.Process(candle.ClosePrice, candle.OpenTime, true);

		var slow = slowValue.ToDecimal();
		var fast = fastValue.ToDecimal();
		var angle = angleValue.ToDecimal();
		var cci = cciValue.ToDecimal();

		var prevSlow = _previousSlowEma;
		var prevFast = _previousFastEma;
		var prevLaguerre = _previousLaguerre;

		var laguerre = CalculateLaguerre(candle.ClosePrice);
		_previousLaguerre = laguerre;

		var slope = CalculateAngleSlope(angle);

		_previousSlowEma = slow;
		_previousFastEma = fast;

		if (!_slowEma.IsFormed || !_fastEma.IsFormed || !_angleEma.IsFormed || prevSlow is null || prevFast is null || slope is null)
			return;

		if (!_laguerreInitialized || prevLaguerre is null)
		{
			_laguerreInitialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.CloseTime.LocalDateTime;
		var isFriday = time.DayOfWeek == DayOfWeek.Friday;
		var blockFridayTrades = isFriday && time.Hour >= FridayBlockHour;
		var exitOnFriday = isFriday && time.Hour >= FridayExitHour;

		if (exitOnFriday && Position != 0m && !HasActiveOrders())
		{
			ClosePosition();
			return;
		}

		ManageRisk(candle);

		if (Position != 0m || HasActiveOrders())
			return;

		if (blockFridayTrades)
			return;

		var trendBias = EvaluateTrendBias(slope.Value);
		var allowLong = trendBias == TrendBiases.Bullish;
		var allowShort = trendBias == TrendBiases.Bearish;

		var laguerreOkLong = prevLaguerre <= LaguerreOversold && laguerre <= LaguerreOversold;
		var laguerreOkShort = prevLaguerre >= LaguerreOverbought && laguerre >= LaguerreOverbought;

		var emaMomentumLong = slow > prevSlow && fast > prevFast;
		var emaMomentumShort = slow < prevSlow && fast < prevFast;

		var cciSupportsLong = cci <= -CciThreshold;
		var cciSupportsShort = cci >= CciThreshold;

		if (allowLong && laguerreOkLong && emaMomentumLong && cciSupportsLong)
		{
			BuyMarket(Volume);
		}
		else if (allowShort && laguerreOkShort && emaMomentumShort && cciSupportsShort)
		{
			SellMarket(Volume);
		}
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (_entryPrice is null || Position == 0m)
			return;

		var entryPrice = _entryPrice.Value;
		var volume = Math.Abs(Position);
		var pipDistance = _pipSize;

		if (pipDistance <= 0m)
			return;

		if (Position > 0m)
		{
			_highestPrice = _highestPrice.HasValue ? Math.Max(_highestPrice.Value, candle.HighPrice) : candle.HighPrice;

			var stopPrice = entryPrice - pipDistance * StopLossPips;
			if (StopLossPips > 0 && candle.LowPrice <= stopPrice && volume > 0m)
			{
				SellMarket(volume);
				return;
			}

			var targetPrice = entryPrice + pipDistance * TakeProfitPips;
			if (TakeProfitPips > 0 && candle.HighPrice >= targetPrice && volume > 0m)
			{
				SellMarket(volume);
				return;
			}

			if (TrailingStopPips > 0 && _highestPrice.HasValue)
			{
				var trailPrice = _highestPrice.Value - pipDistance * TrailingStopPips;
				if (candle.LowPrice <= trailPrice && volume > 0m)
				{
					SellMarket(volume);
				}
			}
		}
		else if (Position < 0m)
		{
			_lowestPrice = _lowestPrice.HasValue ? Math.Min(_lowestPrice.Value, candle.LowPrice) : candle.LowPrice;

			var stopPrice = entryPrice + pipDistance * StopLossPips;
			if (StopLossPips > 0 && candle.HighPrice >= stopPrice && volume > 0m)
			{
				BuyMarket(volume);
				return;
			}

			var targetPrice = entryPrice - pipDistance * TakeProfitPips;
			if (TakeProfitPips > 0 && candle.LowPrice <= targetPrice && volume > 0m)
			{
				BuyMarket(volume);
				return;
			}

			if (TrailingStopPips > 0 && _lowestPrice.HasValue)
			{
				var trailPrice = _lowestPrice.Value + pipDistance * TrailingStopPips;
				if (candle.HighPrice >= trailPrice && volume > 0m)
				{
					BuyMarket(volume);
				}
			}
		}
	}

	private void ClosePosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (Position > 0m)
			SellMarket(volume);
		else if (Position < 0m)
			BuyMarket(volume);
	}

	private decimal CalculateLaguerre(decimal price)
	{
		var gamma = LaguerreGamma;

		var l0Prev = _lagL0;
		var l1Prev = _lagL1;
		var l2Prev = _lagL2;
		var l3Prev = _lagL3;

		_lagL0 = (1m - gamma) * price + gamma * l0Prev;
		_lagL1 = -gamma * _lagL0 + l0Prev + gamma * l1Prev;
		_lagL2 = -gamma * _lagL1 + l1Prev + gamma * l2Prev;
		_lagL3 = -gamma * _lagL2 + l2Prev + gamma * l3Prev;

		decimal cu = 0m;
		decimal cd = 0m;

		if (_lagL0 >= _lagL1)
			cu = _lagL0 - _lagL1;
		else
			cd = _lagL1 - _lagL0;

		if (_lagL1 >= _lagL2)
			cu += _lagL1 - _lagL2;
		else
			cd += _lagL2 - _lagL1;

		if (_lagL2 >= _lagL3)
			cu += _lagL2 - _lagL3;
		else
			cd += _lagL3 - _lagL2;

		var denominator = cu + cd;
		var result = denominator == 0m ? 0m : cu / denominator;

		return result;
	}

	private decimal? CalculateAngleSlope(decimal current)
	{
		var maxCount = Math.Max(AngleStartShift, AngleEndShift) + 1;
		if (maxCount < 2)
			return null;

		_angleBuffer.Add(current);
		if (_angleBuffer.Count > maxCount)
			_angleBuffer.RemoveAt(0);

		if (_angleBuffer.Count < maxCount)
			return null;

		var startIndex = 0;
		var endIndex = _angleBuffer.Count - 1 - AngleEndShift;
		if (endIndex < 0 || startIndex >= _angleBuffer.Count)
			return null;

		var startValue = _angleBuffer[startIndex];
		var endValue = _angleBuffer[endIndex];
		var shiftDiff = AngleStartShift - AngleEndShift;
		if (shiftDiff <= 0)
			return null;

		var pip = _pipSize;
		if (pip <= 0m)
			pip = 0.0001m;

		var slope = (endValue - startValue) / (pip * shiftDiff);
		return slope;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}

	private static TrendBiases EvaluateTrendBias(decimal slope)
	{
		if (slope > 0)
			return TrendBiases.Bullish;
		if (slope < 0)
			return TrendBiases.Bearish;
		return TrendBiases.Flat;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order is null)
			return;

		var price = trade.Trade.Price;

		if (order.Direction == Sides.Buy)
		{
			if (Position > 0m)
			{
				_entryPrice = price;
				_highestPrice = price;
				_lowestPrice = null;
			}
			else if (Position == 0m)
			{
				_entryPrice = null;
				_highestPrice = null;
				_lowestPrice = null;
			}
		}
		else if (order.Direction == Sides.Sell)
		{
			if (Position < 0m)
			{
				_entryPrice = price;
				_lowestPrice = price;
				_highestPrice = null;
			}
			else if (Position == 0m)
			{
				_entryPrice = null;
				_highestPrice = null;
				_lowestPrice = null;
			}
		}
	}

	private enum TrendBiases
	{
		Flat,
		Bullish,
		Bearish,
	}
}

