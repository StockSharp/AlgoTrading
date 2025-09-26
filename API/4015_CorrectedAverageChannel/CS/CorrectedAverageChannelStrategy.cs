using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert e-CA-5 that trades breakouts around the Corrected Average indicator.
/// The strategy subscribes to candles, rebuilds the indicator and places market orders when price crosses
/// the corrected moving average by the configured sigma offsets.
/// </summary>
public class CorrectedAverageChannelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _trailingPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<int> _sigmaBuyPoints;
	private readonly StrategyParam<int> _sigmaSellPoints;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal>? _ma;
	private StandardDeviation? _std;

	private decimal _priceStep;
	private decimal _sigmaBuyOffset;
	private decimal _sigmaSellOffset;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingDistance;
	private decimal _trailingStepDistance;

	private decimal? _previousCorrected;
	private decimal? _previousClose;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal _previousPosition;
	private decimal? _lastTradePrice;
	private Sides? _lastTradeSide;

	/// <summary>
	/// Order size used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop trigger expressed in price steps.
	/// </summary>
	public int TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Minimum increment required to advance the trailing stop in price steps.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Moving average period used by the Corrected Average filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type replicated from the MetaTrader input.
	/// </summary>
	public MaType MaTypeOption
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Buy-side sigma expressed in price steps.
	/// </summary>
	public int SigmaBuyPoints
	{
		get => _sigmaBuyPoints.Value;
		set => _sigmaBuyPoints.Value = value;
	}

	/// <summary>
	/// Sell-side sigma expressed in price steps.
	/// </summary>
	public int SigmaSellPoints
	{
		get => _sigmaSellPoints.Value;
		set => _sigmaSellPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations and signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CorrectedAverageChannelStrategy"/> class.
	/// </summary>
	public CorrectedAverageChannelStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Market order size used for entries", "Trading")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 60)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take Profit (points)", "Distance from entry to the profit target in price steps", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 40)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Distance from entry to the protective stop in price steps", "Risk")
			.SetCanOptimize(true);

		_trailingPoints = Param(nameof(TrailingPoints), 0)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Trailing Trigger (points)", "Profit distance required before the trailing stop activates", "Risk")
			.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 0)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Trailing Step (points)", "Minimum advance in price steps before the trailing stop moves", "Risk")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 35)
			.SetRange(2, 500)
			.SetDisplay("MA Period", "Period of the moving average and standard deviation", "Indicator")
			.SetCanOptimize(true);

		_maType = Param(nameof(MaTypeOption), MaType.Sma)
			.SetDisplay("MA Type", "Moving average type used inside the Corrected Average", "Indicator");

		_sigmaBuyPoints = Param(nameof(SigmaBuyPoints), 5)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Sigma BUY (points)", "Offset added above the corrected average before buying", "Signal")
			.SetCanOptimize(true);

		_sigmaSellPoints = Param(nameof(SigmaSellPoints), 5)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Sigma SELL (points)", "Offset subtracted from the corrected average before selling", "Signal")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "Data");
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

		_ma = null;
		_std = null;
		_priceStep = 0m;
		_sigmaBuyOffset = 0m;
		_sigmaSellOffset = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingDistance = 0m;
		_trailingStepDistance = 0m;
		_previousCorrected = null;
		_previousClose = null;
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_previousPosition = 0m;
		_lastTradePrice = null;
		_lastTradeSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMa(MaTypeOption, MaPeriod);
		_std = new StandardDeviation
		{
			Length = MaPeriod
		};

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
		}

		_sigmaBuyOffset = GetPriceOffset(SigmaBuyPoints);
		_sigmaSellOffset = GetPriceOffset(SigmaSellPoints);
		_stopLossDistance = GetPriceOffset(StopLossPoints);
		_takeProfitDistance = GetPriceOffset(TakeProfitPoints);
		_trailingDistance = GetPriceOffset(TrailingPoints);
		_trailingStepDistance = GetPriceOffset(TrailingStepPoints);

		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ma, _std, ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Trade != null)
		{
			_lastTradePrice = trade.Trade.Price;
		}

		_lastTradeSide = trade.Order.Direction;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (_previousPosition == 0m && Position != 0m)
		{
			var entryPrice = _lastTradePrice ?? _previousClose ?? Security?.LastPrice;
			if (entryPrice is decimal price)
			{
				if (Position > 0m && _lastTradeSide == Sides.Buy)
				{
					InitializeRiskState(price, true);
				}
				else if (Position < 0m && _lastTradeSide == Sides.Sell)
				{
					InitializeRiskState(price, false);
				}
			}
		}
		else if (Position == 0m && _previousPosition != 0m)
		{
			ResetRiskState();
		}

		_previousPosition = Position;
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_ma is null || _std is null)
			return;

		if (!_ma.IsFormed || !_std.IsFormed)
		{
			_previousCorrected = maValue;
			_previousClose = candle.ClosePrice;
			return;
		}

		var previousCorrected = _previousCorrected;
		var previousClose = _previousClose;

		decimal corrected;

		if (previousCorrected is not decimal prevCorrected)
		{
			corrected = maValue;
		}
		else
		{
			var diff = prevCorrected - maValue;
			var v2 = diff * diff;
			var v1 = stdValue * stdValue;
			var k = (v2 <= 0m || v2 < v1) ? 0m : 1m - (v1 / v2);
			corrected = prevCorrected + k * (maValue - prevCorrected);
		}

		if (HandleTrailing(candle))
		{
			_previousCorrected = corrected;
			_previousClose = candle.ClosePrice;
			return;
		}

		if (HandleRiskExit(candle))
		{
			_previousCorrected = corrected;
			_previousClose = candle.ClosePrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCorrected = corrected;
			_previousClose = candle.ClosePrice;
			return;
		}

		if (Position == 0m && previousCorrected is decimal prevCorr && previousClose is decimal prevCls)
		{
			var buyThreshold = corrected + _sigmaBuyOffset;
			var sellThreshold = corrected - _sigmaSellOffset;

			var buySignal = prevCls < prevCorr + _sigmaBuyOffset && candle.ClosePrice >= buyThreshold;
			var sellSignal = prevCls > prevCorr - _sigmaSellOffset && candle.ClosePrice <= sellThreshold;

			if (buySignal)
			{
				BuyMarket();
			}
			else if (sellSignal)
			{
				SellMarket();
			}
		}

		_previousCorrected = corrected;
		_previousClose = candle.ClosePrice;
	}

	private bool HandleTrailing(ICandleMessage candle)
	{
		if (_trailingDistance <= 0m || _entryPrice is null)
			return false;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		if (Position > 0m)
		{
			var moved = candle.ClosePrice - _entryPrice.Value;
			if (moved > _trailingDistance)
			{
				var candidate = candle.ClosePrice - _trailingDistance;
				if (_longTrailingStop is null || candidate - _longTrailingStop.Value >= _trailingStepDistance)
				{
					_longTrailingStop = Security?.ShrinkPrice(candidate) ?? candidate;
				}
			}

			if (_longTrailingStop is decimal trailing && candle.LowPrice <= trailing)
			{
				SellMarket(volume);
				ResetRiskState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			var moved = _entryPrice.Value - candle.ClosePrice;
			if (moved > _trailingDistance)
			{
				var candidate = candle.ClosePrice + _trailingDistance;
				if (_shortTrailingStop is null || _shortTrailingStop.Value - candidate >= _trailingStepDistance)
				{
					_shortTrailingStop = Security?.ShrinkPrice(candidate) ?? candidate;
				}
			}

			if (_shortTrailingStop is decimal trailing && candle.HighPrice >= trailing)
			{
				BuyMarket(volume);
				ResetRiskState();
				return true;
			}
		}

		return false;
	}

	private bool HandleRiskExit(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		if (Position > 0m)
		{
			if (_stopLossPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(volume);
				ResetRiskState();
				return true;
			}

			if (_takeProfitPrice is decimal target && candle.HighPrice >= target)
			{
				SellMarket(volume);
				ResetRiskState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (_stopLossPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				ResetRiskState();
				return true;
			}

			if (_takeProfitPrice is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(volume);
				ResetRiskState();
				return true;
			}
		}

		return false;
	}

	private void InitializeRiskState(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;

		if (_stopLossDistance > 0m)
		{
			var rawPrice = isLong ? entryPrice - _stopLossDistance : entryPrice + _stopLossDistance;
			_stopLossPrice = Security?.ShrinkPrice(rawPrice) ?? rawPrice;
		}

		if (_takeProfitDistance > 0m)
		{
			var rawPrice = isLong ? entryPrice + _takeProfitDistance : entryPrice - _takeProfitDistance;
			_takeProfitPrice = Security?.ShrinkPrice(rawPrice) ?? rawPrice;
		}
	}

	private void ResetRiskState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private decimal GetPriceOffset(int points)
	{
		if (points <= 0 || _priceStep <= 0m)
			return 0m;

		return points * _priceStep;
	}

	private static LengthIndicator<decimal> CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.Sma => new SimpleMovingAverage { Length = length },
			MaType.Ema => new ExponentialMovingAverage { Length = length },
			MaType.Smma => new SmoothedMovingAverage { Length = length },
			MaType.Lwma => new WeightedMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type))
		};
	}

	/// <summary>
	/// Supported moving average types.
	/// </summary>
	public enum MaType
	{
		Sma,
		Ema,
		Smma,
		Lwma
	}
}
