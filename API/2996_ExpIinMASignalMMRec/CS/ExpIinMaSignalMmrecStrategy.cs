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

public class ExpIinMaSignalMmrecStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<MovingAverageTypes> _fastType;
	private readonly StrategyParam<MovingAverageTypes> _slowType;
	private readonly StrategyParam<AppliedPriceTypes> _fastPrice;
	private readonly StrategyParam<AppliedPriceTypes> _slowPrice;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<int> _buyTotalTrigger;
	private readonly StrategyParam<int> _buyLossTrigger;
	private readonly StrategyParam<int> _sellTotalTrigger;
	private readonly StrategyParam<int> _sellLossTrigger;
	private readonly StrategyParam<decimal> _normalVolume;
	private readonly StrategyParam<decimal> _reducedVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<MoneyManagementModes> _moneyMode;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _fastMa = null!;
	private IIndicator _slowMa = null!;
	private decimal? _prevFast;
	private decimal? _prevSlow;
	private readonly Queue<SignalTypes> _signals = new();
	private readonly Queue<decimal> _recentBuyPnL = new();
	private readonly Queue<decimal> _recentSellPnL = new();
	private Sides? _currentSide;
	private decimal? _entryPrice;
	private decimal _entryVolume;

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public MovingAverageTypes FastType
	{
		get => _fastType.Value;
		set => _fastType.Value = value;
	}

	public MovingAverageTypes SlowType
	{
		get => _slowType.Value;
		set => _slowType.Value = value;
	}

	public AppliedPriceTypes FastPrice
	{
		get => _fastPrice.Value;
		set => _fastPrice.Value = value;
	}

	public AppliedPriceTypes SlowPrice
	{
		get => _slowPrice.Value;
		set => _slowPrice.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	public int BuyTotalTrigger
	{
		get => _buyTotalTrigger.Value;
		set => _buyTotalTrigger.Value = value;
	}

	public int BuyLossTrigger
	{
		get => _buyLossTrigger.Value;
		set => _buyLossTrigger.Value = value;
	}

	public int SellTotalTrigger
	{
		get => _sellTotalTrigger.Value;
		set => _sellTotalTrigger.Value = value;
	}

	public int SellLossTrigger
	{
		get => _sellLossTrigger.Value;
		set => _sellLossTrigger.Value = value;
	}

	public decimal NormalVolume
	{
		get => _normalVolume.Value;
		set => _normalVolume.Value = value;
	}

	public decimal ReducedVolume
	{
		get => _reducedVolume.Value;
		set => _reducedVolume.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public MoneyManagementModes MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ExpIinMaSignalMmrecStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Period for the fast moving average", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 22)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Period for the slow moving average", "Indicator");

		_fastType = Param(nameof(FastType), MovingAverageTypes.Exponential)
			.SetDisplay("Fast MA Type", "Type of the fast moving average", "Indicator");

		_slowType = Param(nameof(SlowType), MovingAverageTypes.Simple)
			.SetDisplay("Slow MA Type", "Type of the slow moving average", "Indicator");

		_fastPrice = Param(nameof(FastPrice), AppliedPriceTypes.Close)
			.SetDisplay("Fast MA Price", "Price input for the fast moving average", "Indicator");

		_slowPrice = Param(nameof(SlowPrice), AppliedPriceTypes.Close)
			.SetDisplay("Slow MA Price", "Price input for the slow moving average", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Delay in bars before acting on a signal", "Signal");

		_allowBuyOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_allowSellOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_allowBuyClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Long on Sell", "Close longs when a sell signal appears", "Trading");

		_allowSellClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Short on Buy", "Close shorts when a buy signal appears", "Trading");

		_buyTotalTrigger = Param(nameof(BuyTotalTrigger), 5)
			.SetGreaterThanZero()
			.SetDisplay("Buy Trigger Count", "Number of recent long trades to inspect", "Risk");

		_buyLossTrigger = Param(nameof(BuyLossTrigger), 3)
			.SetGreaterThanZero()
			.SetDisplay("Buy Loss Trigger", "Loss threshold that switches long volume to the reduced size", "Risk");

		_sellTotalTrigger = Param(nameof(SellTotalTrigger), 5)
			.SetGreaterThanZero()
			.SetDisplay("Sell Trigger Count", "Number of recent short trades to inspect", "Risk");

		_sellLossTrigger = Param(nameof(SellLossTrigger), 3)
			.SetGreaterThanZero()
			.SetDisplay("Sell Loss Trigger", "Loss threshold that switches short volume to the reduced size", "Risk");

		_normalVolume = Param(nameof(NormalVolume), 0.1m)
			.SetDisplay("Normal Volume", "Default position volume or risk factor", "Risk");

		_reducedVolume = Param(nameof(ReducedVolume), 0.01m)
			.SetDisplay("Reduced Volume", "Volume used after a losing streak", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in price points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit (pts)", "Take profit distance in price points", "Risk");

		_moneyMode = Param(nameof(MoneyMode), MoneyManagementModes.Lot)
			.SetDisplay("Money Management Mode", "Volume calculation mode", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculations", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	// Reset internal state when the strategy is reset.
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = null;
		_prevSlow = null;
		_signals.Clear();
		_recentBuyPnL.Clear();
		_recentSellPnL.Clear();
		_currentSide = null;
		_entryPrice = null;
		_entryVolume = 0m;
	}

	// Initialize indicators and subscriptions when the strategy starts.
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMovingAverage(FastType, FastPeriod);
		_slowMa = CreateMovingAverage(SlowType, SlowPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	// Process incoming candles and evaluate delayed signals.
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (TryHandleRisk(candle))
			return;

		var fastInput = GetAppliedPrice(candle, FastPrice);
		var slowInput = GetAppliedPrice(candle, SlowPrice);

		var fastValue = _fastMa.Process(fastInput);
		var slowValue = _slowMa.Process(slowInput);

		if (!fastValue.IsFinal || !slowValue.IsFinal)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		var signal = SignalTypes.None;

		if (_prevFast is decimal prevFast && _prevSlow is decimal prevSlow)
		{
			if (prevFast <= prevSlow && fast > slow)
				signal = SignalTypes.Buy;
			else if (prevFast >= prevSlow && fast < slow)
				signal = SignalTypes.Sell;
		}

		_prevFast = fast;
		_prevSlow = slow;

		_signals.Enqueue(signal);

		var required = Math.Max(0, SignalBar);
		if (_signals.Count > required)
		{
			var toProcess = _signals.Dequeue();
			if (toProcess != SignalTypes.None)
				HandleSignal(toProcess, candle);
		}
	}

	// React to the evaluated signal by managing entries and exits.
	private void HandleSignal(SignalTypes signal, ICandleMessage candle)
	{
		switch (signal)
		{
			case SignalTypes.Buy:
			{
				if (_currentSide == Sides.Sell)
				{
					if (!SellPosClose)
						return;

					if (BuyPosOpen)
						ReversePositionToLong(candle);
					else
						ClosePositionByMarket(candle.ClosePrice);
				}
				else if (_currentSide is null && BuyPosOpen)
				{
					EnterLong(candle);
				}
				break;
			}
			case SignalTypes.Sell:
			{
				if (_currentSide == Sides.Buy)
				{
					if (!BuyPosClose)
						return;

					if (SellPosOpen)
						ReversePositionToShort(candle);
					else
						ClosePositionByMarket(candle.ClosePrice);
				}
				else if (_currentSide is null && SellPosOpen)
				{
					EnterShort(candle);
				}
				break;
			}
		}
	}

	// Check protective stop and take-profit levels for open positions.
	private bool TryHandleRisk(ICandleMessage candle)
	{
		if (_currentSide is null || _entryPrice is null || _entryVolume <= 0m)
			return false;

		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints > 0m ? StopLossPoints * step : 0m;
		var takeDistance = TakeProfitPoints > 0m ? TakeProfitPoints * step : 0m;

		if (_currentSide == Sides.Buy)
		{
			if (stopDistance > 0m && candle.LowPrice <= _entryPrice.Value - stopDistance)
			{
				ClosePositionByMarket(_entryPrice.Value - stopDistance);
				return true;
			}

			if (takeDistance > 0m && candle.HighPrice >= _entryPrice.Value + takeDistance)
			{
				ClosePositionByMarket(_entryPrice.Value + takeDistance);
				return true;
			}
		}
		else if (_currentSide == Sides.Sell)
		{
			if (stopDistance > 0m && candle.HighPrice >= _entryPrice.Value + stopDistance)
			{
				ClosePositionByMarket(_entryPrice.Value + stopDistance);
				return true;
			}

			if (takeDistance > 0m && candle.LowPrice <= _entryPrice.Value - takeDistance)
			{
				ClosePositionByMarket(_entryPrice.Value - takeDistance);
				return true;
			}
		}

		return false;
	}

	// Open a new long position with the calculated volume.
	private void EnterLong(ICandleMessage candle)
	{
		var volume = GetBuyVolume(candle);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_currentSide = Sides.Buy;
		_entryPrice = candle.ClosePrice;
		_entryVolume = volume;
	}

	// Open a new short position with the calculated volume.
	private void EnterShort(ICandleMessage candle)
	{
		var volume = GetSellVolume(candle);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_currentSide = Sides.Sell;
		_entryPrice = candle.ClosePrice;
		_entryVolume = volume;
	}

	// Reverse an existing short position into a long one.
	private void ReversePositionToLong(ICandleMessage candle)
	{
		var volume = GetBuyVolume(candle);
		var shortVolume = _entryVolume > 0m ? _entryVolume : (Position < 0m ? Math.Abs(Position) : 0m);
		var totalVolume = volume + shortVolume;

		if (totalVolume <= 0m)
			return;

		if (_entryPrice is decimal entry)
		{
			var pnl = (entry - candle.ClosePrice) * shortVolume;
			RegisterTradeResult(false, pnl);
		}

		BuyMarket(totalVolume);

		_currentSide = Sides.Buy;
		_entryPrice = candle.ClosePrice;
		_entryVolume = volume;
	}

	// Reverse an existing long position into a short one.
	private void ReversePositionToShort(ICandleMessage candle)
	{
		var volume = GetSellVolume(candle);
		var longVolume = _entryVolume > 0m ? _entryVolume : (Position > 0m ? Position : 0m);
		var totalVolume = volume + longVolume;

		if (totalVolume <= 0m)
			return;

		if (_entryPrice is decimal entry)
		{
			var pnl = (candle.ClosePrice - entry) * longVolume;
			RegisterTradeResult(true, pnl);
		}

		SellMarket(totalVolume);

		_currentSide = Sides.Sell;
		_entryPrice = candle.ClosePrice;
		_entryVolume = volume;
	}

	// Close the current position and record the trade outcome.
	private void ClosePositionByMarket(decimal exitPrice)
	{
		if (_currentSide is null || _entryVolume <= 0m)
			return;

		if (_currentSide == Sides.Buy)
		{
			var volume = Position > 0m ? Position : _entryVolume;
			if (volume > 0m)
				SellMarket(volume);

			var entry = _entryPrice ?? exitPrice;
			var pnl = (exitPrice - entry) * _entryVolume;
			RegisterTradeResult(true, pnl);
		}
		else if (_currentSide == Sides.Sell)
		{
			var volume = Position < 0m ? Math.Abs(Position) : _entryVolume;
			if (volume > 0m)
				BuyMarket(volume);

			var entry = _entryPrice ?? exitPrice;
			var pnl = (entry - exitPrice) * _entryVolume;
			RegisterTradeResult(false, pnl);
		}

		_currentSide = null;
		_entryPrice = null;
		_entryVolume = 0m;
	}

	// Determine volume for long entries based on the money management rules.
	private decimal GetBuyVolume(ICandleMessage candle)
	{
		var mmValue = UseReducedVolume(_recentBuyPnL, BuyTotalTrigger, BuyLossTrigger) ? ReducedVolume : NormalVolume;
		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints > 0m ? StopLossPoints * step : 0m;
		return CalculateVolume(mmValue, stopDistance, candle.ClosePrice);
	}

	// Determine volume for short entries based on the money management rules.
	private decimal GetSellVolume(ICandleMessage candle)
	{
		var mmValue = UseReducedVolume(_recentSellPnL, SellTotalTrigger, SellLossTrigger) ? ReducedVolume : NormalVolume;
		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints > 0m ? StopLossPoints * step : 0m;
		return CalculateVolume(mmValue, stopDistance, candle.ClosePrice);
	}

	// Translate the money management setting into an executable volume.
	private decimal CalculateVolume(decimal mmValue, decimal stopDistance, decimal price)
	{
		if (mmValue <= 0m)
			return 0m;

		var capital = Portfolio?.CurrentValue ?? 0m;

		switch (MoneyMode)
		{
			case MoneyManagementModes.Lot:
				return mmValue;
			case MoneyManagementModes.Balance:
			case MoneyManagementModes.FreeMargin:
				return price > 0m ? capital * mmValue / price : 0m;
			case MoneyManagementModes.BalanceRisk:
			case MoneyManagementModes.FreeMarginRisk:
				if (stopDistance > 0m)
					return capital > 0m ? capital * mmValue / stopDistance : 0m;
				return price > 0m ? capital * mmValue / price : 0m;
			default:
				return mmValue;
		}
	}

	// Pick the requested price component from the candle.
	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceTypes type)
	{
		return type switch
		{
			AppliedPriceTypes.Open => candle.OpenPrice,
			AppliedPriceTypes.High => candle.HighPrice,
			AppliedPriceTypes.Low => candle.LowPrice,
			AppliedPriceTypes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceTypes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceTypes.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	// Instantiate the requested moving average indicator.
	private static IIndicator CreateMovingAverage(MovingAverageTypes type, int length)
	{
		return type switch
		{
			MovingAverageTypes.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypes.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypes.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypes.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	// Check whether the reduced volume should be used after a losing streak.
	private static bool UseReducedVolume(Queue<decimal> history, int totalTrigger, int lossTrigger)
	{
		if (lossTrigger <= 0 || totalTrigger <= 0)
			return false;

		var losses = 0;
		var inspected = 0;

		foreach (var pnl in history)
		{
			inspected++;
			if (pnl < 0m)
				losses++;

			if (inspected >= totalTrigger)
				break;
		}

		return losses >= lossTrigger;
	}

	// Store the result of the latest trade for money management decisions.
	private void RegisterTradeResult(bool isLong, decimal pnl)
	{
		var queue = isLong ? _recentBuyPnL : _recentSellPnL;
		var maxCount = isLong ? BuyTotalTrigger : SellTotalTrigger;

		if (maxCount <= 0)
			return;

		queue.Enqueue(pnl);
		while (queue.Count > maxCount)
			queue.Dequeue();
	}

	public enum MovingAverageTypes
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
		VolumeWeighted
	}

	public enum AppliedPriceTypes
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}

	public enum MoneyManagementModes
	{
		FreeMargin,
		Balance,
		FreeMarginRisk,
		BalanceRisk,
		Lot
	}

	private enum SignalTypes
	{
		None,
		Buy,
		Sell
	}
}