using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple moving average crossover strategy converted from the MetaTrader expert S!mple.mq4.
/// Trades when the 50-period LWMA crosses the 200-period SMA and optionally applies money-based stops.
/// </summary>
public class SimpleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _numOrders;
	private readonly StrategyParam<decimal> _stopLossMoney;
	private readonly StrategyParam<decimal> _takeProfitMoney;
	private readonly StrategyParam<int> _trendMargin;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _enableTrading;
	private readonly StrategyParam<DataType> _candleType;

	private LinearWeightedMovingAverage _fastMa;
	private SMA _slowMa;

	private decimal? _fastPrev;
	private decimal? _fastPrev2;
	private decimal? _slowPrev;
	private decimal? _slowPrev2;

	private readonly Queue<decimal> _slowTrendValues = new();
	private decimal? _trendReference;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private bool _stepConversionWarningIssued;

	private enum SignalDirection
	{
		None,
		Buy,
		Sell
	}

	private enum TrendDirection
	{
		Wait,
		Up,
		Down
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleStrategy"/> class.
	/// </summary>
	public SimpleStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Order volume sent on every entry", "General")
			.SetGreaterThanZero();

		_numOrders = Param(nameof(NumOrders), 1)
			.SetDisplay("Number of Orders", "Maximum number of volume blocks replicated from the original EA", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_stopLossMoney = Param(nameof(StopLossMoney), 0m)
			.SetDisplay("Stop Loss (Money)", "Protective amount in account currency per entry block", "Risk")
			.SetNotNegative();

		_takeProfitMoney = Param(nameof(TakeProfitMoney), 0m)
			.SetDisplay("Take Profit (Money)", "Target amount in account currency per entry block", "Risk")
			.SetNotNegative();

		_trendMargin = Param(nameof(TrendMargin), 10)
			.SetDisplay("Trend Margin", "Number of bars used to evaluate the slow-trend reference", "Analysis")
			.SetGreaterThanZero();

		_fastLength = Param(nameof(FastLength), 50)
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_slowLength = Param(nameof(SlowLength), 200)
			.SetDisplay("Slow SMA", "Length of the slow simple moving average", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(100, 400, 20);

		_enableTrading = Param(nameof(EnableTrading), false)
			.SetDisplay("Enable Trading", "Replicates the original makeTrades switch", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame used for indicator calculations", "Data");

		Volume = _tradeVolume.Value;
	}

	/// <summary>
	/// Volume of each entry block.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Maximum number of entry blocks that can be accumulated.
	/// </summary>
	public int NumOrders
	{
		get => _numOrders.Value;
		set => _numOrders.Value = value;
	}

	/// <summary>
	/// Stop-loss amount expressed in account currency per entry block.
	/// </summary>
	public decimal StopLossMoney
	{
		get => _stopLossMoney.Value;
		set => _stopLossMoney.Value = value;
	}

	/// <summary>
	/// Take-profit amount expressed in account currency per entry block.
	/// </summary>
	public decimal TakeProfitMoney
	{
		get => _takeProfitMoney.Value;
		set => _takeProfitMoney.Value = value;
	}

	/// <summary>
	/// Number of finished candles between the current slow SMA value and the trend reference.
	/// </summary>
	public int TrendMargin
	{
		get => _trendMargin.Value;
		set => _trendMargin.Value = value;
	}

	/// <summary>
	/// Length of the fast linear weighted moving average.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Length of the slow simple moving average.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Enables market order execution when true.
	/// </summary>
	public bool EnableTrading
	{
		get => _enableTrading.Value;
		set => _enableTrading.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_fastMa = null;
		_slowMa = null;

		_fastPrev = null;
		_fastPrev2 = null;
		_slowPrev = null;
		_slowPrev2 = null;
		_trendReference = null;
		_slowTrendValues.Clear();
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_stepConversionWarningIssued = false;
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new LinearWeightedMovingAverage { Length = FastLength };
		_slowMa = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		// Work with finished candles to mirror the MetaTrader implementation.
		if (candle.State != CandleStates.Finished)
			return;

		// Store the slow SMA history for the delayed trend comparison.
		UpdateTrendQueue(slowValue);

		// Wait until both moving averages are fully formed.
		if (_fastMa == null || _slowMa == null || !_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			UpdateMovingAverageHistory(fastValue, slowValue);
			return;
		}

		// Ensure the strategy is ready to trade and the subscription is online.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateMovingAverageHistory(fastValue, slowValue);
			return;
		}

		// Apply protective logic before acting on new signals.
		CheckProtectiveLevels(candle);

		var signal = DetectSignal();
		var diffSteps = CalculateStepDifference(fastValue, slowValue);
		var trendState = GetTrendState(slowValue);

		LogSignal(candle, fastValue, slowValue, signal, diffSteps, trendState);

		switch (signal)
		{
			case SignalDirection.Buy:
				HandleBuySignal(candle);
				break;
			case SignalDirection.Sell:
				HandleSellSignal(candle);
				break;
		}

		UpdateMovingAverageHistory(fastValue, slowValue);
	}

	private void HandleBuySignal(ICandleMessage candle)
	{
		// Skip order placement when trading is disabled.
		if (!EnableTrading)
			return;

		// Close any existing short exposure before opening a new long position.
		if (Position < 0m)
		{
			var volumeToCover = Math.Abs(Position);
			if (volumeToCover > 0m)
			{
				BuyMarket(volumeToCover);
				ResetShortTargets();
			}
		}

		var targetVolume = TradeVolume * NumOrders;
		var currentLong = Position > 0m ? Position : 0m;
		var volumeToBuy = targetVolume - currentLong;

		if (volumeToBuy > 0m)
		{
			BuyMarket(volumeToBuy);
		}

		var entryPrice = PositionPrice != 0m ? PositionPrice : candle.ClosePrice;
		var stopDistance = CalculatePriceOffset(StopLossMoney, TradeVolume);
		var takeDistance = CalculatePriceOffset(TakeProfitMoney, TradeVolume);

		_longStop = stopDistance > 0m ? entryPrice - stopDistance : null;
		_longTake = takeDistance > 0m ? entryPrice + takeDistance : null;
		ResetShortTargets();

		LogInfo($"Executed BUY action at {entryPrice:0.#####}. Stop={FormatPrice(_longStop)}, Take={FormatPrice(_longTake)}. Target volume={targetVolume:0.#####}.");
	}

	private void HandleSellSignal(ICandleMessage candle)
	{
		// Skip order placement when trading is disabled.
		if (!EnableTrading)
			return;

		// Close any existing long exposure before opening a new short position.
		if (Position > 0m)
		{
			var volumeToLiquidate = Math.Abs(Position);
			if (volumeToLiquidate > 0m)
			{
				SellMarket(volumeToLiquidate);
				ResetLongTargets();
			}
		}

		var targetVolume = TradeVolume * NumOrders;
		var currentShort = Position < 0m ? Math.Abs(Position) : 0m;
		var volumeToSell = targetVolume - currentShort;

		if (volumeToSell > 0m)
		{
			SellMarket(volumeToSell);
		}

		var entryPrice = PositionPrice != 0m ? PositionPrice : candle.ClosePrice;
		var stopDistance = CalculatePriceOffset(StopLossMoney, TradeVolume);
		var takeDistance = CalculatePriceOffset(TakeProfitMoney, TradeVolume);

		_shortStop = stopDistance > 0m ? entryPrice + stopDistance : null;
		_shortTake = takeDistance > 0m ? entryPrice - takeDistance : null;
		ResetLongTargets();

		LogInfo($"Executed SELL action at {entryPrice:0.#####}. Stop={FormatPrice(_shortStop)}, Take={FormatPrice(_shortTake)}. Target volume={targetVolume:0.#####}.");
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		var position = Position;

		if (position > 0m)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(position));
				LogInfo($"Long stop-loss triggered at {stop:0.#####}.");
				ResetLongTargets();
			}
			else if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(position));
				LogInfo($"Long take-profit triggered at {take:0.#####}.");
				ResetLongTargets();
			}
		}
		else if (position < 0m)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(position));
				LogInfo($"Short stop-loss triggered at {stop:0.#####}.");
				ResetShortTargets();
			}
			else if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(position));
				LogInfo($"Short take-profit triggered at {take:0.#####}.");
				ResetShortTargets();
			}
		}
		else
		{
			ResetLongTargets();
			ResetShortTargets();
		}
	}

	private SignalDirection DetectSignal()
	{
		if (_fastPrev2 is decimal fastHist && _fastPrev is decimal fastPrev &&
			_slowPrev2 is decimal slowHist && _slowPrev is decimal slowPrev)
		{
			if (fastHist < slowHist && fastPrev > slowPrev)
				return SignalDirection.Buy;

			if (fastHist > slowHist && fastPrev < slowPrev)
				return SignalDirection.Sell;
		}

		return SignalDirection.None;
	}

	private void UpdateMovingAverageHistory(decimal fastValue, decimal slowValue)
	{
		if (_fastPrev.HasValue)
			_fastPrev2 = _fastPrev;
		_fastPrev = fastValue;

		if (_slowPrev.HasValue)
			_slowPrev2 = _slowPrev;
		_slowPrev = slowValue;
	}

	private void UpdateTrendQueue(decimal slowValue)
	{
		var margin = TrendMargin;
		if (margin <= 0)
		{
			_slowTrendValues.Clear();
			_trendReference = null;
			return;
		}

		_slowTrendValues.Enqueue(slowValue);

		while (_slowTrendValues.Count > margin + 1)
			_slowTrendValues.Dequeue();

		_trendReference = _slowTrendValues.Count > margin ? _slowTrendValues.Peek() : null;
	}

	private (TrendDirection Direction, decimal? Difference) GetTrendState(decimal slowValue)
	{
		if (_trendReference is not decimal reference)
			return (TrendDirection.Wait, null);

		if (slowValue > reference)
			return (TrendDirection.Up, CalculateStepDifference(slowValue, reference));

		if (slowValue < reference)
			return (TrendDirection.Down, CalculateStepDifference(slowValue, reference));

		return (TrendDirection.Wait, 0m);
	}

	private decimal? CalculateStepDifference(decimal value1, decimal value2)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return null;

		return Math.Abs(value1 - value2) / priceStep;
	}

	private decimal CalculatePriceOffset(decimal moneyAmount, decimal perOrderVolume)
	{
		var amount = Math.Abs(moneyAmount);
		if (amount <= 0m || perOrderVolume <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		{
			if (!_stepConversionWarningIssued)
			{
				LogWarning("PriceStep or StepPrice is not configured. Money-based stops and targets are disabled.");
				_stepConversionWarningIssued = true;
			}
			return 0m;
		}

		var moneyPerStep = stepPrice * perOrderVolume;
		if (moneyPerStep <= 0m)
			return 0m;

		var steps = decimal.Floor(amount / moneyPerStep);
		if (steps <= 0m)
			return 0m;

		return steps * priceStep;
	}

	private void LogSignal(ICandleMessage candle, decimal fastValue, decimal slowValue, SignalDirection signal, decimal? diffSteps, (TrendDirection Direction, decimal? Difference) trendState)
	{
		var signalText = signal switch
		{
			SignalDirection.Buy => "BUY",
			SignalDirection.Sell => "SELL",
			_ => "WAIT"
		};

		var diffText = diffSteps.HasValue ? diffSteps.Value.ToString("0.0", CultureInfo.InvariantCulture) : "n/a";
		var trendText = trendState.Direction switch
		{
			TrendDirection.Up => "UP",
			TrendDirection.Down => "DOWN",
			_ => "WAIT"
		};
		var trendDiffText = trendState.Difference.HasValue ? trendState.Difference.Value.ToString("0.0", CultureInfo.InvariantCulture) : "n/a";

		var fastText = fastValue.ToString("0.#####", CultureInfo.InvariantCulture);
		var slowText = slowValue.ToString("0.#####", CultureInfo.InvariantCulture);

		LogInfo($"[{candle.OpenTime:yyyy-MM-dd HH:mm}] Signal={signalText}, FastMA={fastText}, SlowMA={slowText}, MA diff={diffText} steps, Trend={trendText}, Trend diff={trendDiffText} steps.");
	}

	private void ResetLongTargets()
	{
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortTargets()
	{
		_shortStop = null;
		_shortTake = null;
	}

	private static string FormatPrice(decimal? price)
	{
		return price.HasValue ? price.Value.ToString("0.#####", CultureInfo.InvariantCulture) : "n/a";
	}
}
