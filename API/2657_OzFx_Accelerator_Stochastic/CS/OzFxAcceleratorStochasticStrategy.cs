namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// OzFx strategy converted from MetaTrader 5 to the StockSharp high-level API.
/// Stacks multiple entries when the Acceleration/Deceleration oscillator and stochastic agree.
/// Implements layered targets and dynamic protection to mimic the expert advisor behaviour.
/// </summary>
public class OzFxAcceleratorStochasticStrategy : Strategy
{
	private const int MaxLayers = 5;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _smoothingPeriod;
	private readonly StrategyParam<decimal> _stochasticLevel;
	private readonly StrategyParam<DataType> _candleType;

	private AwesomeOscillator _ao = null!;
	private SimpleMovingAverage _aoSma = null!;
	private StochasticOscillator _stochastic = null!;

	private decimal? _lastAc;
	private bool _lastExitWasTakeProfit;
	private decimal _pipSize;
	private bool _pipInitialized;

	private readonly List<EntryInfo> _longEntries = new();
	private readonly List<EntryInfo> _shortEntries = new();

	/// <summary>
	/// Defines exit origin to replicate modok flag logic.
	/// </summary>
	private enum ExitReason
	{
		Manual,
		TakeProfit,
		StopLoss,
	}

	/// <summary>
	/// Stores layered entry metadata (volume, price, protective levels).
	/// </summary>
	private sealed class EntryInfo
	{
		public decimal Volume;
		public decimal EntryPrice;
		public decimal? StopPrice;
		public decimal? TakeProfitPrice;
		public int Layer;
	}

	/// <summary>
	/// Order volume for each layer.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Base take profit distance per layer measured in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Zero disables trailing mode.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional distance in pips before the trailing stop is advanced.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Main stochastic lookback period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing length.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Final smoothing applied to %K.
	/// </summary>
	public int SmoothingPeriod
	{
		get => _smoothingPeriod.Value;
		set => _smoothingPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic threshold separating bullish and bearish regimes.
	/// </summary>
	public decimal StochasticLevel
	{
		get => _stochasticLevel.Value;
		set => _stochasticLevel.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="OzFxAcceleratorStochasticStrategy"/>.
	/// </summary>
	public OzFxAcceleratorStochasticStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for each layer", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 100m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Base take profit increment in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 50m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Extra move required before advancing the trailing stop", "Risk");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic lookback window", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Smoothing length for %D", "Stochastic");

		_smoothingPeriod = Param(nameof(SmoothingPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Final smoothing for %K", "Stochastic");

		_stochasticLevel = Param(nameof(StochasticLevel), 50m)
			.SetDisplay("Stochastic Level", "Threshold used to trigger signals", "Stochastic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
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

		_longEntries.Clear();
		_shortEntries.Clear();
		_lastAc = null;
		_lastExitWasTakeProfit = false;
		_pipInitialized = false;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ao = new AwesomeOscillator
		{
			ShortPeriod = 5,
			LongPeriod = 34,
		};

		_aoSma = new SimpleMovingAverage
		{
			Length = 5,
		};

		_stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			K = { Length = SmoothingPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ao, _stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ao);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Processes finished candles, updates indicators, and manages entries/exits.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aoValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!aoValue.IsFinal || !stochValue.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal stochK)
			return;

		var ao = aoValue.GetValue<decimal>();
		var aoSmaValue = _aoSma.Process(new DecimalIndicatorValue(_aoSma, ao, candle.ServerTime));
		if (!aoSmaValue.IsFinal)
			return;

		var ac = ao - aoSmaValue.GetValue<decimal>();
		var prevAcNullable = _lastAc;
		if (prevAcNullable is not decimal prevAc)
		{
			_lastAc = ac;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_lastAc = ac;
			return;
		}

		var pip = GetPipSize();
		var stopDistance = StopLossPips > 0m ? StopLossPips * pip : 0m;
		var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * pip : 0m;
		var trailingStopDistance = TrailingStopPips > 0m ? TrailingStopPips * pip : 0m;
		var trailingStepDistance = TrailingStepPips > 0m ? TrailingStepPips * pip : 0m;
		var useTrailing = TrailingStopPips > 0m;

		TryEnterLong(candle, stochK, ac, prevAc, stopDistance, takeDistance);
		TryEnterShort(candle, stochK, ac, prevAc, stopDistance, takeDistance);

		ManageLongPositions(candle, stochK, ac, prevAc, trailingStopDistance, trailingStepDistance, useTrailing);
		ManageShortPositions(candle, stochK, ac, prevAc, trailingStopDistance, trailingStepDistance, useTrailing);

		_lastAc = ac;
	}

	/// <summary>
	/// Opens up to five long layers when momentum turns bullish.
	/// </summary>
	private void TryEnterLong(ICandleMessage candle, decimal stochK, decimal currentAc, decimal previousAc, decimal stopDistance, decimal takeDistance)
	{
		if (_longEntries.Count != 0 || _shortEntries.Count != 0)
			return;

		if (!(stochK > StochasticLevel && currentAc > previousAc && currentAc > 0m && previousAc < 0m))
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		var entryPrice = candle.ClosePrice;

		// First layer mirrors the expert advisor: no stop or target until trailing engages.
		BuyMarket(volume);
		_longEntries.Add(new EntryInfo
		{
			Volume = volume,
			EntryPrice = entryPrice,
			StopPrice = null,
			TakeProfitPrice = null,
			Layer = 0,
		});

		for (var i = 1; i < MaxLayers; i++)
		{
			BuyMarket(volume);

			var stopPrice = stopDistance > 0m ? entryPrice - stopDistance : (decimal?)null;
			var takePrice = takeDistance > 0m ? entryPrice + takeDistance * i : (decimal?)null;

			_longEntries.Add(new EntryInfo
			{
				Volume = volume,
				EntryPrice = entryPrice,
				StopPrice = stopPrice,
				TakeProfitPrice = takePrice,
				Layer = i,
			});
		}
	}

	/// <summary>
	/// Opens up to five short layers when momentum turns bearish.
	/// </summary>
	private void TryEnterShort(ICandleMessage candle, decimal stochK, decimal currentAc, decimal previousAc, decimal stopDistance, decimal takeDistance)
	{
		if (_shortEntries.Count != 0 || _longEntries.Count != 0)
			return;

		if (!(stochK < StochasticLevel && currentAc < previousAc && currentAc < 0m && previousAc > 0m))
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		var entryPrice = candle.ClosePrice;

		SellMarket(volume);
		_shortEntries.Add(new EntryInfo
		{
			Volume = volume,
			EntryPrice = entryPrice,
			StopPrice = null,
			TakeProfitPrice = null,
			Layer = 0,
		});

		for (var i = 1; i < MaxLayers; i++)
		{
			SellMarket(volume);

			var stopPrice = stopDistance > 0m ? entryPrice + stopDistance : (decimal?)null;
			var takePrice = takeDistance > 0m ? entryPrice - takeDistance * i : (decimal?)null;

			_shortEntries.Add(new EntryInfo
			{
				Volume = volume,
				EntryPrice = entryPrice,
				StopPrice = stopPrice,
				TakeProfitPrice = takePrice,
				Layer = i,
			});
		}
	}

	/// <summary>
	/// Manages open long layers including trailing logic and staged targets.
	/// </summary>
	private void ManageLongPositions(ICandleMessage candle, decimal stochK, decimal currentAc, decimal previousAc, decimal trailingStopDistance, decimal trailingStepDistance, bool useTrailing)
	{
		if (_longEntries.Count == 0)
			return;

		if (Position <= 0m)
		{
			_longEntries.Clear();
			return;
		}

		var closePrice = candle.ClosePrice;
		var highPrice = candle.HighPrice;
		var lowPrice = candle.LowPrice;

		var exitSignal = stochK < 50m && currentAc < previousAc && currentAc < 0m && previousAc > 0m;

		if (useTrailing)
		{
			if (exitSignal)
			{
				CloseAllLong(ExitReason.Manual);
				return;
			}

			if (trailingStopDistance > 0m)
			{
				for (var i = 0; i < _longEntries.Count; i++)
				{
					var entry = _longEntries[i];
					var profit = closePrice - entry.EntryPrice;
					if (profit > trailingStopDistance + trailingStepDistance)
					{
						var newStop = closePrice - trailingStopDistance;
						if (entry.StopPrice is not decimal existing || newStop > existing)
							entry.StopPrice = newStop;
					}
				}
			}
		}
		else if (_lastExitWasTakeProfit)
		{
			if (exitSignal)
			{
				CloseAllLong(ExitReason.Manual);
				return;
			}

			for (var i = 0; i < _longEntries.Count; i++)
			{
				var entry = _longEntries[i];
				if (entry.StopPrice is null && closePrice > entry.EntryPrice)
					entry.StopPrice = entry.EntryPrice;
			}
		}

		for (var i = 0; i < _longEntries.Count; i++)
		{
			var entry = _longEntries[i];
			if (entry.StopPrice is decimal stopPrice && lowPrice <= stopPrice)
			{
				CloseAllLong(ExitReason.StopLoss);
				return;
			}
		}

		var anyTakeProfit = false;
		for (var i = _longEntries.Count - 1; i >= 0; i--)
		{
			var entry = _longEntries[i];
			if (entry.TakeProfitPrice is decimal takePrice && highPrice >= takePrice)
			{
				SellMarket(entry.Volume);
				_longEntries.RemoveAt(i);
				anyTakeProfit = true;
			}
		}

		if (anyTakeProfit)
			_lastExitWasTakeProfit = true;
	}

	/// <summary>
	/// Manages open short layers including trailing logic and staged targets.
	/// </summary>
	private void ManageShortPositions(ICandleMessage candle, decimal stochK, decimal currentAc, decimal previousAc, decimal trailingStopDistance, decimal trailingStepDistance, bool useTrailing)
	{
		if (_shortEntries.Count == 0)
			return;

		if (Position >= 0m)
		{
			_shortEntries.Clear();
			return;
		}

		var closePrice = candle.ClosePrice;
		var highPrice = candle.HighPrice;
		var lowPrice = candle.LowPrice;

		var exitSignal = stochK > 50m && currentAc > previousAc && currentAc > 0m && previousAc < 0m;

		if (useTrailing)
		{
			if (exitSignal)
			{
				CloseAllShort(ExitReason.Manual);
				return;
			}

			if (trailingStopDistance > 0m)
			{
				for (var i = 0; i < _shortEntries.Count; i++)
				{
					var entry = _shortEntries[i];
					var profit = entry.EntryPrice - closePrice;
					if (profit > trailingStopDistance + trailingStepDistance)
					{
						var newStop = closePrice + trailingStopDistance;
						if (entry.StopPrice is not decimal existing || newStop < existing)
							entry.StopPrice = newStop;
					}
				}
			}
		}
		else if (_lastExitWasTakeProfit)
		{
			if (exitSignal)
			{
				CloseAllShort(ExitReason.Manual);
				return;
			}

			for (var i = 0; i < _shortEntries.Count; i++)
			{
				var entry = _shortEntries[i];
				if (entry.StopPrice is null && closePrice < entry.EntryPrice)
					entry.StopPrice = entry.EntryPrice;
			}
		}

		for (var i = 0; i < _shortEntries.Count; i++)
		{
			var entry = _shortEntries[i];
			if (entry.StopPrice is decimal stopPrice && highPrice >= stopPrice)
			{
				CloseAllShort(ExitReason.StopLoss);
				return;
			}
		}

		var anyTakeProfit = false;
		for (var i = _shortEntries.Count - 1; i >= 0; i--)
		{
			var entry = _shortEntries[i];
			if (entry.TakeProfitPrice is decimal takePrice && lowPrice <= takePrice)
			{
				BuyMarket(entry.Volume);
				_shortEntries.RemoveAt(i);
				anyTakeProfit = true;
			}
		}

		if (anyTakeProfit)
			_lastExitWasTakeProfit = true;
	}

	/// <summary>
	/// Closes all long layers and updates the modok-like flag.
	/// </summary>
	private void CloseAllLong(ExitReason reason)
	{
		var volume = 0m;
		for (var i = 0; i < _longEntries.Count; i++)
			volume += _longEntries[i].Volume;

		if (volume > 0m && Position > 0m)
			SellMarket(volume);

		_longEntries.Clear();

		if (reason == ExitReason.TakeProfit)
			_lastExitWasTakeProfit = true;
		else if (reason == ExitReason.StopLoss)
			_lastExitWasTakeProfit = false;
	}

	/// <summary>
	/// Closes all short layers and updates the modok-like flag.
	/// </summary>
	private void CloseAllShort(ExitReason reason)
	{
		var volume = 0m;
		for (var i = 0; i < _shortEntries.Count; i++)
			volume += _shortEntries[i].Volume;

		if (volume > 0m && Position < 0m)
			BuyMarket(volume);

		_shortEntries.Clear();

		if (reason == ExitReason.TakeProfit)
			_lastExitWasTakeProfit = true;
		else if (reason == ExitReason.StopLoss)
			_lastExitWasTakeProfit = false;
	}

	/// <summary>
	/// Calculates pip value based on the security tick size and decimal digits.
	/// </summary>
	private decimal GetPipSize()
	{
		if (_pipInitialized)
			return _pipSize;

		var security = Security;
		var step = security?.MinPriceStep ?? 0m;
		if (step <= 0m)
			step = 0.0001m;

		var decimals = security?.Decimals ?? 0;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;

		_pipSize = step * adjust;
		if (_pipSize <= 0m)
			_pipSize = step;

		if (_pipSize <= 0m)
			_pipSize = 0.0001m;

		_pipInitialized = true;
		return _pipSize;
	}
}
