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
/// GlamTrader strategy converted from the MetaTrader expert advisor.
/// Combines a shifted moving average, Laguerre RSI filter and Awesome Oscillator momentum.
/// </summary>
public class GlamTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossBuyPips;
	private readonly StrategyParam<decimal> _takeProfitBuyPips;
	private readonly StrategyParam<decimal> _stopLossSellPips;
	private readonly StrategyParam<decimal> _takeProfitSellPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMethod> _maMethod;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<decimal> _laguerreGamma;

	private LengthIndicator<decimal> _maIndicator = null!;
	private AwesomeOscillator _awesomeOscillator = null!;
	private readonly List<decimal> _maBuffer = new();
	private decimal? _previousAo;
	private decimal _lagL0;
	private decimal _lagL1;
	private decimal _lagL2;
	private decimal _lagL3;
	private bool _laguerreInitialized;
	private decimal _priceStep;
	private decimal _pipSize;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _longEntry;
	private decimal? _shortEntry;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Initializes a new instance of the <see cref="GlamTraderStrategy"/> class.
	/// </summary>
	public GlamTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe processed by the strategy", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Order size used for entries", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 50m)
		.SetNotNegative()
		.SetDisplay("Buy Stop Loss (pips)", "Protective stop distance for long trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 150m, 5m);

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 50m)
		.SetNotNegative()
		.SetDisplay("Buy Take Profit (pips)", "Profit target distance for long trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 5m);

		_stopLossSellPips = Param(nameof(StopLossSellPips), 50m)
		.SetNotNegative()
		.SetDisplay("Sell Stop Loss (pips)", "Protective stop distance for short trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 150m, 5m);

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 50m)
		.SetNotNegative()
		.SetDisplay("Sell Take Profit (pips)", "Profit target distance for short trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 5m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance applied after profits accumulate", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 50m, 1m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 15m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Extra movement required before adjusting the trailing stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 100m, 1m);

		_maPeriod = Param(nameof(MaPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the moving average applied to price", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_maShift = Param(nameof(MaShift), 1)
		.SetNotNegative()
		.SetDisplay("MA Shift", "Bars of displacement applied to the moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0, 5, 1);

		_maMethod = Param(nameof(MaMethod), MaMethod.LinearWeighted)
		.SetDisplay("MA Method", "Averaging method applied to the moving average", "Indicators");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Weighted)
		.SetDisplay("Applied Price", "Price source used for both MA and Laguerre filters", "Indicators");

		_laguerreGamma = Param(nameof(LaguerreGamma), 0.7m)
		.SetRange(0.1m, 0.9m)
		.SetDisplay("Laguerre Gamma", "Smoothing factor of the Laguerre RSI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.4m, 0.9m, 0.02m);
	}

	/// <summary>
	/// Primary candle data type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume submitted on new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance for long trades expressed in pips.
	/// </summary>
	public decimal StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for long trades expressed in pips.
	/// </summary>
	public decimal TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short trades expressed in pips.
	/// </summary>
	public decimal StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for short trades expressed in pips.
	/// </summary>
	public decimal TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
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
	/// Extra distance before the trailing stop is adjusted.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Displacement applied to the moving average in bars.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average method replicated from MetaTrader constants.
	/// </summary>
	public MaMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source used by the moving average and Laguerre filter.
	/// </summary>
	public AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Laguerre smoothing coefficient.
	/// </summary>
	public decimal LaguerreGamma
	{
		get => _laguerreGamma.Value;
		set => _laguerreGamma.Value = value;
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

		_maBuffer.Clear();
		_previousAo = null;
		_lagL0 = _lagL1 = _lagL2 = _lagL3 = 0m;
		_laguerreInitialized = false;
		_priceStep = 0m;
		_pipSize = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_longEntry = null;
		_shortEntry = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
		}

		var decimals = Security?.Decimals ?? 0;
		_pipSize = _priceStep;
		if (decimals is 3 or 5)
		{
			_pipSize = _priceStep * 10m;
		}

		_maIndicator = CreateMovingAverage(MaMethod, MaPeriod);
		_awesomeOscillator = new AwesomeOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_maIndicator, _awesomeOscillator, ProcessCandle)
		.Start();

		var mainArea = CreateChartArea();
		if (mainArea != null)
		{
			DrawCandles(mainArea, subscription);
			DrawIndicator(mainArea, _maIndicator);
			DrawOwnTrades(mainArea);
		}

		var oscillatorArea = CreateChartArea("Awesome Oscillator");
		if (oscillatorArea != null)
		{
			DrawIndicator(oscillatorArea, _awesomeOscillator);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var exitTriggered = ManageOpenPosition(candle);

		// Store the latest moving average value while respecting the configured shift.
		_maBuffer.Add(maValue);
		var required = MaShift + 2;
		while (_maBuffer.Count > required)
		{
			_maBuffer.RemoveAt(0);
		}

		// Update Laguerre RSI using the selected price source.
		var laguerre = UpdateLaguerre(GetAppliedPrice(candle));

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousAo = aoValue;
			return;
		}

		if (!_maIndicator.IsFormed)
		{
			_previousAo = aoValue;
			return;
		}

		if (_maBuffer.Count <= MaShift)
		{
			_previousAo = aoValue;
			return;
		}

		if (_previousAo is null)
		{
			_previousAo = aoValue;
			return;
		}

		if (exitTriggered)
		{
			_previousAo = aoValue;
			return;
		}

		var shiftedMa = _maBuffer[_maBuffer.Count - 1 - MaShift];
		var previousAo = _previousAo.Value;
		var close = candle.ClosePrice;

		var canEnterLong = Position == 0m && shiftedMa > close && laguerre > 0.15m && aoValue > previousAo;
		var canEnterShort = Position == 0m && shiftedMa < close && laguerre < 0.75m && aoValue < previousAo;

		if (canEnterLong)
		{
			TryEnterLong(close);
		}
		else if (canEnterShort)
		{
			TryEnterShort(close);
		}

		_previousAo = aoValue;
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var entry = PositionPrice != 0m ? PositionPrice : _longEntry ?? candle.ClosePrice;
			_longEntry = entry;

			if (_longStop is null && StopLossBuyPips > 0m)
			{
				var candidate = NormalizePrice(entry - StopLossBuyPips * _pipSize);
				_longStop = candidate > 0m ? candidate : null;
			}

			if (_longTake is null && TakeProfitBuyPips > 0m)
			{
				var candidate = NormalizePrice(entry + TakeProfitBuyPips * _pipSize);
				_longTake = candidate > 0m ? candidate : null;
			}

			UpdateTrailingForLong(candle.ClosePrice, entry);

			if (!_longExitRequested && _longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_longExitRequested = true;
				return true;
			}

			if (!_longExitRequested && _longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				_longExitRequested = true;
				return true;
			}
		}
		else if (Position < 0m)
		{
			var entry = PositionPrice != 0m ? PositionPrice : _shortEntry ?? candle.ClosePrice;
			_shortEntry = entry;
			var volume = Math.Abs(Position);

			if (_shortStop is null && StopLossSellPips > 0m)
			{
				var candidate = NormalizePrice(entry + StopLossSellPips * _pipSize);
				_shortStop = candidate > 0m ? candidate : null;
			}

			if (_shortTake is null && TakeProfitSellPips > 0m)
			{
				var candidate = NormalizePrice(entry - TakeProfitSellPips * _pipSize);
				_shortTake = candidate > 0m ? candidate : null;
			}

			UpdateTrailingForShort(candle.ClosePrice, entry);

			if (!_shortExitRequested && _shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				return true;
			}

			if (!_shortExitRequested && _shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				return true;
			}
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private void TryEnterLong(decimal referencePrice)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);
		_longEntry = referencePrice;
		_longStop = StopLossBuyPips > 0m ? NormalizePrice(referencePrice - StopLossBuyPips * _pipSize) : null;
		_longTake = TakeProfitBuyPips > 0m ? NormalizePrice(referencePrice + TakeProfitBuyPips * _pipSize) : null;
		_longExitRequested = false;

		_shortEntry = null;
		_shortStop = null;
		_shortTake = null;
		_shortExitRequested = false;
	}

	private void TryEnterShort(decimal referencePrice)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);
		_shortEntry = referencePrice;
		_shortStop = StopLossSellPips > 0m ? NormalizePrice(referencePrice + StopLossSellPips * _pipSize) : null;
		_shortTake = TakeProfitSellPips > 0m ? NormalizePrice(referencePrice - TakeProfitSellPips * _pipSize) : null;
		_shortExitRequested = false;

		_longEntry = null;
		_longStop = null;
		_longTake = null;
		_longExitRequested = false;
	}

	private decimal UpdateLaguerre(decimal price)
	{
		if (!_laguerreInitialized)
		{
			_lagL0 = _lagL1 = _lagL2 = _lagL3 = price;
			_laguerreInitialized = true;
			return 0m;
		}

		var gamma = LaguerreGamma;
		var prevL0 = _lagL0;
		var prevL1 = _lagL1;
		var prevL2 = _lagL2;
		var prevL3 = _lagL3;

		_lagL0 = (1m - gamma) * price + gamma * prevL0;
		_lagL1 = -gamma * _lagL0 + prevL0 + gamma * prevL1;
		_lagL2 = -gamma * _lagL1 + prevL1 + gamma * prevL2;
		_lagL3 = -gamma * _lagL2 + prevL2 + gamma * prevL3;

		decimal cu = 0m;
		decimal cd = 0m;

		if (_lagL0 >= _lagL1)
		{
			cu = _lagL0 - _lagL1;
		}
		else
		{
			cd = _lagL1 - _lagL0;
		}

		if (_lagL1 >= _lagL2)
		{
			cu += _lagL1 - _lagL2;
		}
		else
		{
			cd += _lagL2 - _lagL1;
		}

		if (_lagL2 >= _lagL3)
		{
			cu += _lagL2 - _lagL3;
		}
		else
		{
			cd += _lagL3 - _lagL2;
		}

		var denominator = cu + cd;
		return denominator == 0m ? 0m : cu / denominator;
	}

	private void UpdateTrailingForLong(decimal price, decimal entry)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m)
		{
			return;
		}

		var trail = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (price - entry <= trail + step)
		{
			return;
		}

		var threshold = price - (trail + step);
		if (_longStop is null || _longStop < threshold)
		{
			var candidate = NormalizePrice(price - trail);
			if (candidate > 0m)
			{
				_longStop = candidate;
			}
		}
	}

	private void UpdateTrailingForShort(decimal price, decimal entry)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m)
		{
			return;
		}

		var trail = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (entry - price <= trail + step)
		{
			return;
		}

		var threshold = price + trail + step;
		if (_shortStop is null || _shortStop > threshold)
		{
			var candidate = NormalizePrice(price + trail);
			if (candidate > 0m)
			{
				_shortStop = candidate;
			}
		}
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int length)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = length },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MaMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
		{
			return price;
		}

		return Math.Round(price / _priceStep, MidpointRounding.AwayFromZero) * _priceStep;
	}

	private void ResetPositionState()
	{
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_longEntry = null;
		_shortEntry = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <summary>
	/// Moving average methods corresponding to MetaTrader modes.
	/// </summary>
	public enum MaMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted,
	}

	/// <summary>
	/// Price source equivalents of MetaTrader's applied price constants.
	/// </summary>
	public enum AppliedPrice
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
	}
}

