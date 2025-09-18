using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bullish and bearish candle following strategy with independent martingale logic.
/// Buys after a strong bullish candle and sells after a strong bearish candle.
/// Applies separate stop-loss, take-profit, and martingale multipliers for each direction.
/// </summary>
public class BullBearCandleMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _bullMultiplier;
	private readonly StrategyParam<decimal> _bearMultiplier;
	private readonly StrategyParam<int> _bullStopLossPips;
	private readonly StrategyParam<int> _bullTakeProfitPips;
	private readonly StrategyParam<int> _bearStopLossPips;
	private readonly StrategyParam<int> _bearTakeProfitPips;
	private readonly StrategyParam<int> _bullMinBodyPips;
	private readonly StrategyParam<int> _bearMinBodyPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _alignedInitialVolume;
	private decimal _bullCurrentVolume;
	private decimal _bearCurrentVolume;
	private decimal _previousPosition;
	private decimal _lastRealizedPnL;
	private Sides? _activeDirection;

	/// <summary>
	/// The base volume used to start the martingale sequence.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the next bullish trade volume after a loss.
	/// </summary>
	public decimal BullMultiplier
	{
		get => _bullMultiplier.Value;
		set => _bullMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the next bearish trade volume after a loss.
	/// </summary>
	public decimal BearMultiplier
	{
		get => _bearMultiplier.Value;
		set => _bearMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for bullish trades expressed in pips.
	/// </summary>
	public int BullStopLossPips
	{
		get => _bullStopLossPips.Value;
		set => _bullStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for bullish trades expressed in pips.
	/// </summary>
	public int BullTakeProfitPips
	{
		get => _bullTakeProfitPips.Value;
		set => _bullTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for bearish trades expressed in pips.
	/// </summary>
	public int BearStopLossPips
	{
		get => _bearStopLossPips.Value;
		set => _bearStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for bearish trades expressed in pips.
	/// </summary>
	public int BearTakeProfitPips
	{
		get => _bearTakeProfitPips.Value;
		set => _bearTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Minimum bullish candle body size measured in pips.
	/// </summary>
	public int BullMinBodyPips
	{
		get => _bullMinBodyPips.Value;
		set => _bullMinBodyPips.Value = value;
	}

	/// <summary>
	/// Minimum bearish candle body size measured in pips.
	/// </summary>
	public int BearMinBodyPips
	{
		get => _bearMinBodyPips.Value;
		set => _bearMinBodyPips.Value = value;
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
	/// Initializes a new instance of the <see cref="BullBearCandleMartingaleStrategy"/> class.
	/// </summary>
	public BullBearCandleMartingaleStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Base volume used when a new martingale cycle starts", "Money Management")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);

		_bullMultiplier = Param(nameof(BullMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bull Multiplier", "Volume multiplier after a losing bullish trade", "Money Management")
		.SetCanOptimize(true)
		.SetOptimize(1.5m, 4m, 0.5m);

		_bearMultiplier = Param(nameof(BearMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bear Multiplier", "Volume multiplier after a losing bearish trade", "Money Management")
		.SetCanOptimize(true)
		.SetOptimize(1.5m, 4m, 0.5m);

		_bullStopLossPips = Param(nameof(BullStopLossPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Bull Stop Loss", "Stop-loss distance for bullish trades in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_bullTakeProfitPips = Param(nameof(BullTakeProfitPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Bull Take Profit", "Take-profit distance for bullish trades in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_bearStopLossPips = Param(nameof(BearStopLossPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Bear Stop Loss", "Stop-loss distance for bearish trades in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_bearTakeProfitPips = Param(nameof(BearTakeProfitPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Bear Take Profit", "Take-profit distance for bearish trades in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_bullMinBodyPips = Param(nameof(BullMinBodyPips), 40)
		.SetGreaterThanZero()
		.SetDisplay("Bull Body Filter", "Minimum bullish candle body size in pips", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(5, 100, 5);

		_bearMinBodyPips = Param(nameof(BearMinBodyPips), 40)
		.SetGreaterThanZero()
		.SetDisplay("Bear Body Filter", "Minimum bearish candle body size in pips", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(5, 100, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used to detect bullish and bearish candles", "General");
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

		_pipSize = 0m;
		_alignedInitialVolume = 0m;
		_bullCurrentVolume = 0m;
		_bearCurrentVolume = 0m;
		_previousPosition = 0m;
		_lastRealizedPnL = 0m;
		_activeDirection = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePipSize();

		_alignedInitialVolume = AlignVolume(InitialVolume);
		_bullCurrentVolume = _alignedInitialVolume;
		_bearCurrentVolume = _alignedInitialVolume;
		Volume = _alignedInitialVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.WhenCandlesFinished(ProcessCandle)
		.Start();

		StartProtection(useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var body = candle.ClosePrice - candle.OpenPrice;
		if (body == 0m)
		return;

		if (Position != 0m)
		return;

		if (body > 0m)
		{
			var minBody = BullMinBodyPips * _pipSize;
			if (Math.Abs(body) < minBody)
			return;

			ExecuteBullishTrade(candle.ClosePrice);
		}
		else
		{
			var minBody = BearMinBodyPips * _pipSize;
			if (Math.Abs(body) < minBody)
			return;

			ExecuteBearishTrade(candle.ClosePrice);
		}
	}

	private void ExecuteBullishTrade(decimal entryPrice)
	{
		var volume = _bullCurrentVolume;
		if (volume <= 0m)
		return;

		volume = AlignVolume(volume);
		var resultingPosition = Position + volume;

		// Store trade direction to evaluate martingale adjustments later.
		_activeDirection = Sides.Buy;

		BuyMarket(volume);

		ApplyProtection(BullStopLossPips, BullTakeProfitPips, entryPrice, resultingPosition);
	}

	private void ExecuteBearishTrade(decimal entryPrice)
	{
		var volume = _bearCurrentVolume;
		if (volume <= 0m)
		return;

		volume = AlignVolume(volume);
		var resultingPosition = Position - volume;

		// Store trade direction to evaluate martingale adjustments later.
		_activeDirection = Sides.Sell;

		SellMarket(volume);

		ApplyProtection(BearStopLossPips, BearTakeProfitPips, entryPrice, resultingPosition);
	}

	private void ApplyProtection(int stopPips, int takePips, decimal entryPrice, decimal resultingPosition)
	{
		if (_pipSize <= 0m)
		return;

		if (stopPips > 0)
		{
			var stopDistance = stopPips * _pipSize;
			SetStopLoss(stopDistance, entryPrice, resultingPosition);
		}

		if (takePips > 0)
		{
			var takeDistance = takePips * _pipSize;
			SetTakeProfit(takeDistance, entryPrice, resultingPosition);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (_previousPosition == 0m && Position != 0m)
		{
			// Record the baseline PnL when a new trade starts.
			_lastRealizedPnL = PnL;
		}
		else if (_previousPosition != 0m && Position == 0m)
		{
			var tradePnL = PnL - _lastRealizedPnL;
			_lastRealizedPnL = PnL;

			AdjustMartingale(tradePnL);
		}

		_previousPosition = Position;
	}

	private void AdjustMartingale(decimal tradePnL)
	{
		if (_activeDirection is null)
		return;

		var multiplier = _activeDirection == Sides.Buy ? BullMultiplier : BearMultiplier;
		var currentVolume = _activeDirection == Sides.Buy ? _bullCurrentVolume : _bearCurrentVolume;

		if (tradePnL < 0m)
		{
			currentVolume = AlignVolume(currentVolume * multiplier);
		}
		else
		{
			currentVolume = _alignedInitialVolume;
		}

		if (_activeDirection == Sides.Buy)
		_bullCurrentVolume = currentVolume;
		else
		_bearCurrentVolume = currentVolume;

		// Reset the active direction until a new trade starts.
		_activeDirection = null;
	}

	private void InitializePipSize()
	{
		var step = Security?.PriceStep;
		_pipSize = step.HasValue && step.Value > 0m ? step.Value : 0.0001m;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = Security?.VolumeStep;
		if (!step.HasValue || step.Value <= 0m)
		return volume;

		var stepValue = step.Value;
		var steps = Math.Max(1m, Math.Round(volume / stepValue));
		return steps * stepValue;
	}
}
