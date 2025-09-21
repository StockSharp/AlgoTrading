using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DeMarker martingale strategy converted from the MetaTrader expert "Demarker Martingale".
/// Combines DeMarker and higher timeframe MACD filters with martingale position sizing and
/// trailing risk management rules.
/// </summary>
public class DemarkerMartingaleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _demarkerThreshold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _lotIncrement;
	private readonly StrategyParam<bool> _doubleLotSize;
	private readonly StrategyParam<int> _maxMartingaleSteps;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;

	private readonly Queue<ICandleMessage> _recentCandles = new();

	private decimal _pipSize;
	private decimal _currentVolume;
	private int _martingaleStep;
	private decimal _averageEntryPrice;
	private decimal _currentPosition;
	private Sides? _entrySide;
	private decimal _stopLossPrice;
	private decimal _takeProfitPrice;
	private decimal _trailingStopPrice;
	private decimal _breakEvenPrice;
	private decimal? _latestMacdMain;
	private decimal? _latestMacdSignal;

	/// <summary>
	/// Initializes a new instance of <see cref="DemarkerMartingaleStrategy"/>.
	/// </summary>
	public DemarkerMartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Trading Candles", "Primary timeframe used for signals", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Candles", "Higher timeframe used for MACD filter", "General");

		_demarkerPeriod = Param(nameof(DemarkerPeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Period", "Lookback for DeMarker indicator", "Indicators");

		_demarkerThreshold = Param(nameof(DemarkerThreshold), 0.5m)
		.SetRange(0m, 1m)
		.SetDisplay("DeMarker Threshold", "Neutral level separating long and short logic", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators");

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Base order volume", "Trading");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Multiplier", "Volume multiplier applied after a loss", "Trading");

		_lotIncrement = Param(nameof(LotIncrement), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Increment", "Additive lot increment when doubling is disabled", "Trading");

		_doubleLotSize = Param(nameof(DoubleLotSize), false)
		.SetDisplay("Use Multiplier", "When true volumes are multiplied, otherwise incremented", "Trading");

		_maxMartingaleSteps = Param(nameof(MaxMartingaleSteps), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Steps", "Maximum consecutive martingale escalations", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 5m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 5m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Target profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break-Even", "Enable break-even shift of the stop", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 10m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Break-Even Trigger", "Profit in pips required before moving stop", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Break-Even Offset", "Extra buffer beyond entry when moving stop", "Risk");
	}

	/// <summary>
	/// Primary timeframe used for the trading logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe for MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// DeMarker indicator lookback period.
	/// </summary>
	public int DemarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}

	/// <summary>
	/// Neutral DeMarker threshold separating long and short modes.
	/// </summary>
	public decimal DemarkerThreshold
	{
		get => _demarkerThreshold.Value;
		set => _demarkerThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD filter.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD filter.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal EMA length for MACD filter.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Base trading volume before martingale escalation.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplication factor applied after a losing sequence when <see cref="DoubleLotSize"/> is enabled.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Additive increment used after losses when <see cref="DoubleLotSize"/> is disabled.
	/// </summary>
	public decimal LotIncrement
	{
		get => _lotIncrement.Value;
		set => _lotIncrement.Value = value;
	}

	/// <summary>
	/// Determines whether to multiply (true) or increment (false) the order size after losses.
	/// </summary>
	public bool DoubleLotSize
	{
		get => _doubleLotSize.Value;
		set => _doubleLotSize.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive martingale escalations.
	/// </summary>
	public int MaxMartingaleSteps
	{
		get => _maxMartingaleSteps.Value;
		set => _maxMartingaleSteps.Value = value;
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
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enables the break-even protection behaviour.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit required before moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional offset applied when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (MacdCandleType != CandleType)
			yield return (Security, MacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_recentCandles.Clear();
		_currentVolume = 0m;
		_martingaleStep = 0;
		_averageEntryPrice = 0m;
		_currentPosition = 0m;
		_entrySide = null;
		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;
		_trailingStopPrice = 0m;
		_breakEvenPrice = 0m;
		_latestMacdMain = null;
		_latestMacdSignal = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePipSize();
		_currentVolume = AlignVolume(InitialVolume);
		Volume = _currentVolume;

		var deMarker = new DeMarker { Length = DemarkerPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription.Bind(deMarker, ProcessTradingCandle).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.BindEx(macd, ProcessMacdCandle).Start();

		StartProtection(useMarketOrders: true);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Trade is null || trade.Order is null)
			return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;
		var direction = trade.Order.Side;

		if (direction == Sides.Buy)
		{
			if (_currentPosition >= 0m)
			{
				var previousVolume = _currentPosition;
				_currentPosition += volume;

				if (previousVolume <= 0m)
				{
					_averageEntryPrice = price;
					_entrySide = Sides.Buy;
					InitializeTargets();
				}
				else
				{
					_averageEntryPrice = ((previousVolume * _averageEntryPrice) + volume * price) / _currentPosition;
					InitializeTargets();
				}
			}
			else
			{
				var closingVolume = Math.Min(volume, Math.Abs(_currentPosition));
				_currentPosition += volume;
				var profit = (_averageEntryPrice - price) * closingVolume;

				if (_currentPosition >= 0m)
				{
					FinalizePosition(profit);
				}
			}
		}
		else if (direction == Sides.Sell)
		{
			if (_currentPosition <= 0m)
			{
				var previousVolume = Math.Abs(_currentPosition);
				_currentPosition -= volume;

				if (previousVolume <= 0m)
				{
					_averageEntryPrice = price;
					_entrySide = Sides.Sell;
					InitializeTargets();
				}
				else
				{
					var totalVolume = previousVolume + volume;
					_averageEntryPrice = ((previousVolume * _averageEntryPrice) + volume * price) / totalVolume;
					_currentPosition = -totalVolume;
					InitializeTargets();
				}
			}
			else
			{
				var closingVolume = Math.Min(volume, _currentPosition);
				_currentPosition -= volume;
				var profit = (price - _averageEntryPrice) * closingVolume;

				if (_currentPosition <= 0m)
				{
					FinalizePosition(profit);
				}
			}
		}
	}

	private void ProcessTradingCandle(ICandleMessage candle, decimal demarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_recentCandles.Enqueue(candle);
		while (_recentCandles.Count > 3)
			_recentCandles.Dequeue();

		ManageActivePosition(candle);

		if (Position != 0m)
			return;

		if (_recentCandles.Count < 3)
			return;

		if (_latestMacdMain is not decimal macdMain || _latestMacdSignal is not decimal macdSignal)
			return;

		var candles = _recentCandles.ToArray();
		var previous1 = candles[^2];
		var previous2 = candles[^3];

		var buyCondition = demarkerValue > DemarkerThreshold &&
			previous2.LowPrice < previous1.HighPrice &&
			IsMacdBullish(macdMain, macdSignal);

		var sellCondition = demarkerValue < DemarkerThreshold &&
			previous1.LowPrice < previous2.HighPrice &&
			IsMacdBearish(macdMain, macdSignal);

		if (buyCondition)
		{
			OpenLong(candle.ClosePrice);
		}
		else if (sellCondition)
		{
			OpenShort(candle.ClosePrice);
		}
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (!macdValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macd)
			return;

		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
			return;

		_latestMacdMain = macdMain;
		_latestMacdSignal = macdSignal;
	}

	private void OpenLong(decimal referencePrice)
	{
		if (_currentVolume <= 0m)
			return;

		var volume = AlignVolume(_currentVolume);
		if (volume <= 0m)
			return;

		LogInfo($"Opening long. Price={referencePrice:F5}, Volume={volume}, Step={_martingaleStep}");
		BuyMarket(volume);
	}

	private void OpenShort(decimal referencePrice)
	{
		if (_currentVolume <= 0m)
			return;

		var volume = AlignVolume(_currentVolume);
		if (volume <= 0m)
			return;

		LogInfo($"Opening short. Price={referencePrice:F5}, Volume={volume}, Step={_martingaleStep}");
		SellMarket(volume);
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (_entrySide is null || _currentPosition == 0m)
			return;

		var price = candle.ClosePrice;
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		if (_entrySide == Sides.Buy)
		{
			if (_takeProfitPrice > 0m && price >= _takeProfitPrice)
			{
				LogInfo($"Take-profit hit at {price:F5}. Closing long position.");
				SellMarket(volume);
				return;
			}

			if (_stopLossPrice > 0m && price <= _stopLossPrice)
			{
				LogInfo($"Stop-loss hit at {price:F5}. Closing long position.");
				SellMarket(volume);
				return;
			}

			if (_breakEvenPrice > 0m && price <= _breakEvenPrice)
			{
				LogInfo($"Break-even stop hit at {price:F5}. Closing long position.");
				SellMarket(volume);
				return;
			}

			if (_trailingStopPrice > 0m && price <= _trailingStopPrice)
			{
				LogInfo($"Trailing stop hit at {price:F5}. Closing long position.");
				SellMarket(volume);
				return;
			}

			UpdateDynamicStops(price);
		}
		else
		{
			if (_takeProfitPrice > 0m && price <= _takeProfitPrice)
			{
				LogInfo($"Take-profit hit at {price:F5}. Closing short position.");
				BuyMarket(volume);
				return;
			}

			if (_stopLossPrice > 0m && price >= _stopLossPrice)
			{
				LogInfo($"Stop-loss hit at {price:F5}. Closing short position.");
				BuyMarket(volume);
				return;
			}

			if (_breakEvenPrice > 0m && price >= _breakEvenPrice)
			{
				LogInfo($"Break-even stop hit at {price:F5}. Closing short position.");
				BuyMarket(volume);
				return;
			}

			if (_trailingStopPrice > 0m && price >= _trailingStopPrice)
			{
				LogInfo($"Trailing stop hit at {price:F5}. Closing short position.");
				BuyMarket(volume);
				return;
			}

			UpdateDynamicStops(price);
		}
	}

	private void UpdateDynamicStops(decimal currentPrice)
	{
		if (_entrySide is null)
			return;

		var profitDistance = _entrySide == Sides.Buy
			? currentPrice - _averageEntryPrice
			: _averageEntryPrice - currentPrice;

		if (profitDistance <= 0m)
			return;

		var trailingDistance = PipToPrice(TrailingStopPips);
		if (TrailingStopPips > 0m && trailingDistance > 0m)
		{
			if (_entrySide == Sides.Buy)
			{
				var desired = currentPrice - trailingDistance;
				if (desired > _trailingStopPrice)
					_trailingStopPrice = desired;
			}
			else
			{
				var desired = currentPrice + trailingDistance;
				if (_trailingStopPrice == 0m || desired < _trailingStopPrice)
					_trailingStopPrice = desired;
			}
		}

		if (UseBreakEven && BreakEvenTriggerPips > 0m)
		{
			var trigger = PipToPrice(BreakEvenTriggerPips);
			if (trigger > 0m && profitDistance >= trigger)
			{
				var offset = PipToPrice(BreakEvenOffsetPips);
				if (_entrySide == Sides.Buy)
				{
					var breakeven = _averageEntryPrice + offset;
					if (breakeven > _breakEvenPrice)
						_breakEvenPrice = breakeven;
				}
				else
				{
					var breakeven = _averageEntryPrice - offset;
					if (_breakEvenPrice == 0m || breakeven < _breakEvenPrice)
						_breakEvenPrice = breakeven;
				}
			}
		}
	}

	private void FinalizePosition(decimal profit)
	{
		LogInfo($"Position closed. Result={profit:F5}");
		_currentPosition = 0m;
		_entrySide = null;
		_averageEntryPrice = 0m;
		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;
		_trailingStopPrice = 0m;
		_breakEvenPrice = 0m;

		AdjustMartingale(profit);
	}

	private void InitializeTargets()
	{
		if (_entrySide is null)
			return;

		var stopDistance = PipToPrice(StopLossPips);
		var takeDistance = PipToPrice(TakeProfitPips);

		if (_entrySide == Sides.Buy)
		{
			_stopLossPrice = stopDistance > 0m ? _averageEntryPrice - stopDistance : 0m;
			_takeProfitPrice = takeDistance > 0m ? _averageEntryPrice + takeDistance : 0m;
		}
		else
		{
			_stopLossPrice = stopDistance > 0m ? _averageEntryPrice + stopDistance : 0m;
			_takeProfitPrice = takeDistance > 0m ? _averageEntryPrice - takeDistance : 0m;
		}

		_trailingStopPrice = 0m;
		_breakEvenPrice = 0m;
	}

	private void AdjustMartingale(decimal profit)
	{
		if (profit > 0m)
		{
			_martingaleStep = 0;
			_currentVolume = AlignVolume(InitialVolume);
		}
		else
		{
			if (_martingaleStep >= MaxMartingaleSteps)
			{
				LogInfo("Maximum martingale steps reached. Volume will not increase further.");
				return;
			}

			_martingaleStep++;

			var nextVolume = DoubleLotSize
				? _currentVolume * MartingaleMultiplier
				: _currentVolume + LotIncrement;

			_currentVolume = AlignVolume(nextVolume);
		}

		Volume = _currentVolume;
	}

	private bool IsMacdBullish(decimal macdMain, decimal macdSignal)
	{
		return (macdMain > 0m && macdMain > macdSignal) || (macdMain < 0m && macdMain > macdSignal);
	}

	private bool IsMacdBearish(decimal macdMain, decimal macdSignal)
	{
		return (macdMain > 0m && macdMain < macdSignal) || (macdMain < 0m && macdMain < macdSignal);
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
