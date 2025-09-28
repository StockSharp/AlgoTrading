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

/// <summary>
/// Port of the MetaTrader expert "Exp_ColorMaRsi-Trigger_MMRec_Duplex" using StockSharp high level API.
/// Two independent MaRsi-Trigger blocks manage long and short signals while a money management filter
/// lowers the trade volume after a streak of losses.
/// </summary>
public class ColorMaRsiTriggerMmRecDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<bool> _longAllowOpen;
	private readonly StrategyParam<bool> _longAllowClose;
	private readonly StrategyParam<int> _longStopLossPoints;
	private readonly StrategyParam<int> _longTakeProfitPoints;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<int> _longRsiPeriod;
	private readonly StrategyParam<int> _longRsiLongPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<int> _longMaLongPeriod;
	private readonly StrategyParam<AppliedPriceTypes> _longRsiPrice;
	private readonly StrategyParam<AppliedPriceTypes> _longRsiLongPrice;
	private readonly StrategyParam<AppliedPriceTypes> _longMaPrice;
	private readonly StrategyParam<AppliedPriceTypes> _longMaLongPrice;
	private readonly StrategyParam<MovingAverageMethods> _longMaType;
	private readonly StrategyParam<MovingAverageMethods> _longMaLongType;
	private readonly StrategyParam<decimal> _longNormalVolume;
	private readonly StrategyParam<decimal> _longReducedVolume;
	private readonly StrategyParam<int> _longHistoryDepth;
	private readonly StrategyParam<int> _longLossTrigger;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<bool> _shortAllowOpen;
	private readonly StrategyParam<bool> _shortAllowClose;
	private readonly StrategyParam<int> _shortStopLossPoints;
	private readonly StrategyParam<int> _shortTakeProfitPoints;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<int> _shortRsiPeriod;
	private readonly StrategyParam<int> _shortRsiLongPeriod;
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _shortMaLongPeriod;
	private readonly StrategyParam<AppliedPriceTypes> _shortRsiPrice;
	private readonly StrategyParam<AppliedPriceTypes> _shortRsiLongPrice;
	private readonly StrategyParam<AppliedPriceTypes> _shortMaPrice;
	private readonly StrategyParam<AppliedPriceTypes> _shortMaLongPrice;
	private readonly StrategyParam<MovingAverageMethods> _shortMaType;
	private readonly StrategyParam<MovingAverageMethods> _shortMaLongType;
	private readonly StrategyParam<decimal> _shortNormalVolume;
	private readonly StrategyParam<decimal> _shortReducedVolume;
	private readonly StrategyParam<int> _shortHistoryDepth;
	private readonly StrategyParam<int> _shortLossTrigger;

	private ColorMaRsiTriggerCalculator _longCalculator = null!;
	private ColorMaRsiTriggerCalculator _shortCalculator = null!;

	private readonly List<decimal> _longHistory = new();
	private readonly List<decimal> _shortHistory = new();
	private readonly List<bool> _longLossHistory = new();
	private readonly List<bool> _shortLossHistory = new();

	private decimal? _longEntryPrice;
	private decimal _longEntryVolume;
	private decimal? _shortEntryPrice;
	private decimal _shortEntryVolume;

	/// <summary>
	/// Initializes a new instance of the <see cref="ColorMaRsiTriggerMmRecDuplexStrategy"/> class.
	/// </summary>
	public ColorMaRsiTriggerMmRecDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Time-frame used by the long MaRsi block", "Long Block");

		_longAllowOpen = Param(nameof(LongAllowOpen), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Long Block");

		_longAllowClose = Param(nameof(LongAllowClose), true)
			.SetDisplay("Allow Long Exit", "Enable closing long positions", "Long Block");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Long Stop Loss", "Protective stop for long trades in price steps", "Long Block");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000)
			.SetNotNegative()
			.SetDisplay("Long Take Profit", "Profit target for long trades in price steps", "Long Block");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Long Signal Bar", "Shift used when sampling the indicator", "Long Block");

		_longRsiPeriod = Param(nameof(LongRsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long RSI Fast", "Fast RSI length for the long block", "Long Block");

		_longRsiLongPeriod = Param(nameof(LongRsiLongPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Long RSI Slow", "Slow RSI length for the long block", "Long Block");

		_longMaPeriod = Param(nameof(LongMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Fast", "Fast moving average length for the long block", "Long Block");

		_longMaLongPeriod = Param(nameof(LongMaLongPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Slow", "Slow moving average length for the long block", "Long Block");

		_longRsiPrice = Param(nameof(LongRsiPrice), AppliedPriceTypes.Weighted)
			.SetDisplay("Long RSI Price", "Price mode for the fast RSI", "Long Block");

		_longRsiLongPrice = Param(nameof(LongRsiLongPrice), AppliedPriceTypes.Median)
			.SetDisplay("Long RSI Slow Price", "Price mode for the slow RSI", "Long Block");

		_longMaPrice = Param(nameof(LongMaPrice), AppliedPriceTypes.Close)
			.SetDisplay("Long MA Price", "Price mode for the fast moving average", "Long Block");

		_longMaLongPrice = Param(nameof(LongMaLongPrice), AppliedPriceTypes.Close)
			.SetDisplay("Long MA Slow Price", "Price mode for the slow moving average", "Long Block");

		_longMaType = Param(nameof(LongMaType), MovingAverageMethods.Exponential)
			.SetDisplay("Long MA Method", "Smoothing algorithm for the fast moving average", "Long Block");

		_longMaLongType = Param(nameof(LongMaLongType), MovingAverageMethods.Exponential)
			.SetDisplay("Long MA Slow Method", "Smoothing algorithm for the slow moving average", "Long Block");

		_longNormalVolume = Param(nameof(LongNormalVolume), 0.1m)
			.SetNotNegative()
			.SetDisplay("Long Normal Volume", "Default long volume before the money management filter", "Money Management");

		_longReducedVolume = Param(nameof(LongReducedVolume), 0.01m)
			.SetNotNegative()
			.SetDisplay("Long Reduced Volume", "Volume used after a long loss streak", "Money Management");

		_longHistoryDepth = Param(nameof(LongHistoryDepth), 5)
			.SetGreaterThanZero()
			.SetDisplay("Long History Depth", "Number of recent long trades inspected for losses", "Money Management");

		_longLossTrigger = Param(nameof(LongLossTrigger), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long Loss Trigger", "Loss count that switches to the reduced long volume", "Money Management");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Time-frame used by the short MaRsi block", "Short Block");

		_shortAllowOpen = Param(nameof(ShortAllowOpen), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Short Block");

		_shortAllowClose = Param(nameof(ShortAllowClose), true)
			.SetDisplay("Allow Short Exit", "Enable closing short positions", "Short Block");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Short Stop Loss", "Protective stop for short trades in price steps", "Short Block");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000)
			.SetNotNegative()
			.SetDisplay("Short Take Profit", "Profit target for short trades in price steps", "Short Block");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Short Signal Bar", "Shift used when sampling the indicator", "Short Block");

		_shortRsiPeriod = Param(nameof(ShortRsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short RSI Fast", "Fast RSI length for the short block", "Short Block");

		_shortRsiLongPeriod = Param(nameof(ShortRsiLongPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Short RSI Slow", "Slow RSI length for the short block", "Short Block");

		_shortMaPeriod = Param(nameof(ShortMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Fast", "Fast moving average length for the short block", "Short Block");

		_shortMaLongPeriod = Param(nameof(ShortMaLongPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Slow", "Slow moving average length for the short block", "Short Block");

		_shortRsiPrice = Param(nameof(ShortRsiPrice), AppliedPriceTypes.Weighted)
			.SetDisplay("Short RSI Price", "Price mode for the fast RSI", "Short Block");

		_shortRsiLongPrice = Param(nameof(ShortRsiLongPrice), AppliedPriceTypes.Median)
			.SetDisplay("Short RSI Slow Price", "Price mode for the slow RSI", "Short Block");

		_shortMaPrice = Param(nameof(ShortMaPrice), AppliedPriceTypes.Close)
			.SetDisplay("Short MA Price", "Price mode for the fast moving average", "Short Block");

		_shortMaLongPrice = Param(nameof(ShortMaLongPrice), AppliedPriceTypes.Close)
			.SetDisplay("Short MA Slow Price", "Price mode for the slow moving average", "Short Block");

		_shortMaType = Param(nameof(ShortMaType), MovingAverageMethods.Exponential)
			.SetDisplay("Short MA Method", "Smoothing algorithm for the fast moving average", "Short Block");

		_shortMaLongType = Param(nameof(ShortMaLongType), MovingAverageMethods.Exponential)
			.SetDisplay("Short MA Slow Method", "Smoothing algorithm for the slow moving average", "Short Block");

		_shortNormalVolume = Param(nameof(ShortNormalVolume), 0.1m)
			.SetNotNegative()
			.SetDisplay("Short Normal Volume", "Default short volume before the money management filter", "Money Management");

		_shortReducedVolume = Param(nameof(ShortReducedVolume), 0.01m)
			.SetNotNegative()
			.SetDisplay("Short Reduced Volume", "Volume used after a short loss streak", "Money Management");

		_shortHistoryDepth = Param(nameof(ShortHistoryDepth), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short History Depth", "Number of recent short trades inspected for losses", "Money Management");

		_shortLossTrigger = Param(nameof(ShortLossTrigger), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short Loss Trigger", "Loss count that switches to the reduced short volume", "Money Management");
	}

	/// <summary>
	/// Candle type used for the long MaRsi block.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool LongAllowOpen
	{
		get => _longAllowOpen.Value;
		set => _longAllowOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool LongAllowClose
	{
		get => _longAllowClose.Value;
		set => _longAllowClose.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points for long trades.
	/// </summary>
	public int LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points for long trades.
	/// </summary>
	public int LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Shift used when sampling the indicator for long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Fast RSI period for the long block.
	/// </summary>
	public int LongRsiPeriod
	{
		get => _longRsiPeriod.Value;
		set => _longRsiPeriod.Value = value;
	}

	/// <summary>
	/// Slow RSI period for the long block.
	/// </summary>
	public int LongRsiLongPeriod
	{
		get => _longRsiLongPeriod.Value;
		set => _longRsiLongPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average period for the long block.
	/// </summary>
	public int LongMaPeriod
	{
		get => _longMaPeriod.Value;
		set => _longMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period for the long block.
	/// </summary>
	public int LongMaLongPeriod
	{
		get => _longMaLongPeriod.Value;
		set => _longMaLongPeriod.Value = value;
	}

	/// <summary>
	/// Applied price used by the fast RSI in the long block.
	/// </summary>
	public AppliedPriceTypes LongRsiPrice
	{
		get => _longRsiPrice.Value;
		set => _longRsiPrice.Value = value;
	}

	/// <summary>
	/// Applied price used by the slow RSI in the long block.
	/// </summary>
	public AppliedPriceTypes LongRsiLongPrice
	{
		get => _longRsiLongPrice.Value;
		set => _longRsiLongPrice.Value = value;
	}

	/// <summary>
	/// Applied price used by the fast moving average in the long block.
	/// </summary>
	public AppliedPriceTypes LongMaPrice
	{
		get => _longMaPrice.Value;
		set => _longMaPrice.Value = value;
	}

	/// <summary>
	/// Applied price used by the slow moving average in the long block.
	/// </summary>
	public AppliedPriceTypes LongMaLongPrice
	{
		get => _longMaLongPrice.Value;
		set => _longMaLongPrice.Value = value;
	}

	/// <summary>
	/// Moving average method for the fast line in the long block.
	/// </summary>
	public MovingAverageMethods LongMaType
	{
		get => _longMaType.Value;
		set => _longMaType.Value = value;
	}

	/// <summary>
	/// Moving average method for the slow line in the long block.
	/// </summary>
	public MovingAverageMethods LongMaLongType
	{
		get => _longMaLongType.Value;
		set => _longMaLongType.Value = value;
	}

	/// <summary>
	/// Volume used for long entries before the loss filter triggers.
	/// </summary>
	public decimal LongNormalVolume
	{
		get => _longNormalVolume.Value;
		set => _longNormalVolume.Value = value;
	}

	/// <summary>
	/// Volume used for long entries after the loss filter triggers.
	/// </summary>
	public decimal LongReducedVolume
	{
		get => _longReducedVolume.Value;
		set => _longReducedVolume.Value = value;
	}

	/// <summary>
	/// Number of recent long trades inspected by the money management filter.
	/// </summary>
	public int LongHistoryDepth
	{
		get => _longHistoryDepth.Value;
		set => _longHistoryDepth.Value = value;
	}

	/// <summary>
	/// Loss threshold that switches long entries to the reduced volume.
	/// </summary>
	public int LongLossTrigger
	{
		get => _longLossTrigger.Value;
		set => _longLossTrigger.Value = value;
	}

	/// <summary>
	/// Candle type used for the short MaRsi block.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool ShortAllowOpen
	{
		get => _shortAllowOpen.Value;
		set => _shortAllowOpen.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool ShortAllowClose
	{
		get => _shortAllowClose.Value;
		set => _shortAllowClose.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points for short trades.
	/// </summary>
	public int ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points for short trades.
	/// </summary>
	public int ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Shift used when sampling the indicator for short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Fast RSI period for the short block.
	/// </summary>
	public int ShortRsiPeriod
	{
		get => _shortRsiPeriod.Value;
		set => _shortRsiPeriod.Value = value;
	}

	/// <summary>
	/// Slow RSI period for the short block.
	/// </summary>
	public int ShortRsiLongPeriod
	{
		get => _shortRsiLongPeriod.Value;
		set => _shortRsiLongPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average period for the short block.
	/// </summary>
	public int ShortMaPeriod
	{
		get => _shortMaPeriod.Value;
		set => _shortMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period for the short block.
	/// </summary>
	public int ShortMaLongPeriod
	{
		get => _shortMaLongPeriod.Value;
		set => _shortMaLongPeriod.Value = value;
	}

	/// <summary>
	/// Applied price used by the fast RSI in the short block.
	/// </summary>
	public AppliedPriceTypes ShortRsiPrice
	{
		get => _shortRsiPrice.Value;
		set => _shortRsiPrice.Value = value;
	}

	/// <summary>
	/// Applied price used by the slow RSI in the short block.
	/// </summary>
	public AppliedPriceTypes ShortRsiLongPrice
	{
		get => _shortRsiLongPrice.Value;
		set => _shortRsiLongPrice.Value = value;
	}

	/// <summary>
	/// Applied price used by the fast moving average in the short block.
	/// </summary>
	public AppliedPriceTypes ShortMaPrice
	{
		get => _shortMaPrice.Value;
		set => _shortMaPrice.Value = value;
	}

	/// <summary>
	/// Applied price used by the slow moving average in the short block.
	/// </summary>
	public AppliedPriceTypes ShortMaLongPrice
	{
		get => _shortMaLongPrice.Value;
		set => _shortMaLongPrice.Value = value;
	}

	/// <summary>
	/// Moving average method for the fast line in the short block.
	/// </summary>
	public MovingAverageMethods ShortMaType
	{
		get => _shortMaType.Value;
		set => _shortMaType.Value = value;
	}

	/// <summary>
	/// Moving average method for the slow line in the short block.
	/// </summary>
	public MovingAverageMethods ShortMaLongType
	{
		get => _shortMaLongType.Value;
		set => _shortMaLongType.Value = value;
	}

	/// <summary>
	/// Volume used for short entries before the loss filter triggers.
	/// </summary>
	public decimal ShortNormalVolume
	{
		get => _shortNormalVolume.Value;
		set => _shortNormalVolume.Value = value;
	}

	/// <summary>
	/// Volume used for short entries after the loss filter triggers.
	/// </summary>
	public decimal ShortReducedVolume
	{
		get => _shortReducedVolume.Value;
		set => _shortReducedVolume.Value = value;
	}

	/// <summary>
	/// Number of recent short trades inspected by the money management filter.
	/// </summary>
	public int ShortHistoryDepth
	{
		get => _shortHistoryDepth.Value;
		set => _shortHistoryDepth.Value = value;
	}

	/// <summary>
	/// Loss threshold that switches short entries to the reduced volume.
	/// </summary>
	public int ShortLossTrigger
	{
		get => _shortLossTrigger.Value;
		set => _shortLossTrigger.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, LongCandleType);

		if (LongCandleType != ShortCandleType)
			yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longHistory.Clear();
		_shortHistory.Clear();
		_longLossHistory.Clear();
		_shortLossHistory.Clear();
		_longEntryPrice = null;
		_longEntryVolume = 0m;
		_shortEntryPrice = null;
		_shortEntryVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longCalculator = new ColorMaRsiTriggerCalculator(
			CreateMovingAverage(LongMaType, LongMaPeriod),
			CreateMovingAverage(LongMaLongType, LongMaLongPeriod),
			new RelativeStrengthIndex { Length = LongRsiPeriod },
			new RelativeStrengthIndex { Length = LongRsiLongPeriod },
			LongMaPrice,
			LongMaLongPrice,
			LongRsiPrice,
			LongRsiLongPrice);

		_shortCalculator = new ColorMaRsiTriggerCalculator(
			CreateMovingAverage(ShortMaType, ShortMaPeriod),
			CreateMovingAverage(ShortMaLongType, ShortMaLongPeriod),
			new RelativeStrengthIndex { Length = ShortRsiPeriod },
			new RelativeStrengthIndex { Length = ShortRsiLongPeriod },
			ShortMaPrice,
			ShortMaLongPrice,
			ShortRsiPrice,
			ShortRsiLongPrice);

		if (LongCandleType == ShortCandleType)
		{
			SubscribeCandles(LongCandleType)
				.WhenCandlesFinished(ProcessCombinedCandle)
				.Start();
		}
		else
		{
			SubscribeCandles(LongCandleType)
				.WhenCandlesFinished(ProcessLongCandle)
				.Start();

			SubscribeCandles(ShortCandleType)
				.WhenCandlesFinished(ProcessShortCandle)
				.Start();
		}
	}

	private void ProcessCombinedCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var longValue = _longCalculator.Process(candle);
		if (longValue is not null)
			UpdateHistory(_longHistory, longValue.Value, LongSignalBar);

		var shortValue = _shortCalculator.Process(candle);
		if (shortValue is not null)
			UpdateHistory(_shortHistory, shortValue.Value, ShortSignalBar);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EvaluateLongSignals(candle);
		EvaluateShortSignals(candle);
		UpdateRiskManagement(candle);
	}

	private void ProcessLongCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = _longCalculator.Process(candle);
		if (value is null)
			return;

		UpdateHistory(_longHistory, value.Value, LongSignalBar);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EvaluateLongSignals(candle);
		UpdateRiskManagement(candle);
	}

	private void ProcessShortCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = _shortCalculator.Process(candle);
		if (value is null)
			return;

		UpdateHistory(_shortHistory, value.Value, ShortSignalBar);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EvaluateShortSignals(candle);
		UpdateRiskManagement(candle);
	}

	private void EvaluateLongSignals(ICandleMessage candle)
	{
		if (_longHistory.Count <= LongSignalBar + 1)
			return;

		var recent = _longHistory[LongSignalBar];
		var older = _longHistory[LongSignalBar + 1];

		if (LongAllowClose && Position > 0m && older < 0m)
			CloseLong(candle.ClosePrice);

		if (LongAllowOpen && Position <= 0m && older > 0m && recent <= 0m)
			OpenLong(candle.ClosePrice);
	}

	private void EvaluateShortSignals(ICandleMessage candle)
	{
		if (_shortHistory.Count <= ShortSignalBar + 1)
			return;

		var recent = _shortHistory[ShortSignalBar];
		var older = _shortHistory[ShortSignalBar + 1];

		if (ShortAllowClose && Position < 0m && older > 0m)
			CloseShort(candle.ClosePrice);

		if (ShortAllowOpen && Position >= 0m && older < 0m && recent >= 0m)
			OpenShort(candle.ClosePrice);
	}

	private void OpenLong(decimal entryPrice)
	{
		if (LongNormalVolume <= 0m && LongReducedVolume <= 0m)
			return;

		if (Position < 0m)
		{
			if (!ShortAllowClose)
				return;

			CloseShort(entryPrice);
		}

		var volume = GetLongVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_longEntryPrice = entryPrice;
		_longEntryVolume = volume;
	}

	private void OpenShort(decimal entryPrice)
	{
		if (ShortNormalVolume <= 0m && ShortReducedVolume <= 0m)
			return;

		if (Position > 0m)
		{
			if (!LongAllowClose)
				return;

			CloseLong(entryPrice);
		}

		var volume = GetShortVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_shortEntryPrice = entryPrice;
		_shortEntryVolume = volume;
	}

	private void CloseLong(decimal exitPrice)
	{
		if (Position <= 0m)
			return;

		RegisterLongResult(exitPrice);
		SellMarket(Position);
	}

	private void CloseShort(decimal exitPrice)
	{
		if (Position >= 0m)
			return;

		RegisterShortResult(exitPrice);
		BuyMarket(Math.Abs(Position));
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;

		if (Position > 0m && _longEntryPrice is decimal entryLong)
		{
			var stop = LongStopLossPoints > 0 ? entryLong - LongStopLossPoints * step : (decimal?)null;
			var take = LongTakeProfitPoints > 0 ? entryLong + LongTakeProfitPoints * step : (decimal?)null;

			if (stop.HasValue && candle.LowPrice <= stop.Value)
			{
				CloseLong(stop.Value);
				return;
			}

			if (take.HasValue && candle.HighPrice >= take.Value)
			{
				CloseLong(take.Value);
				return;
			}
		}
		else if (Position < 0m && _shortEntryPrice is decimal entryShort)
		{
			var stop = ShortStopLossPoints > 0 ? entryShort + ShortStopLossPoints * step : (decimal?)null;
			var take = ShortTakeProfitPoints > 0 ? entryShort - ShortTakeProfitPoints * step : (decimal?)null;

			if (stop.HasValue && candle.HighPrice >= stop.Value)
			{
				CloseShort(stop.Value);
				return;
			}

			if (take.HasValue && candle.LowPrice <= take.Value)
				CloseShort(take.Value);
		}
	}

	private decimal GetLongVolume()
	{
		if (LongLossTrigger <= 0)
			return LongReducedVolume;

		var losses = CountRecentLosses(_longLossHistory, LongHistoryDepth);
		return losses >= LongLossTrigger ? LongReducedVolume : LongNormalVolume;
	}

	private decimal GetShortVolume()
	{
		if (ShortLossTrigger <= 0)
			return ShortReducedVolume;

		var losses = CountRecentLosses(_shortLossHistory, ShortHistoryDepth);
		return losses >= ShortLossTrigger ? ShortReducedVolume : ShortNormalVolume;
	}

	private static int CountRecentLosses(List<bool> history, int depth)
	{
		var inspected = 0;
		var losses = 0;
		for (var i = history.Count - 1; i >= 0 && inspected < depth; i--)
		{
			if (history[i])
				losses++;
			inspected++;
		}

		return losses;
	}

	private void RegisterLongResult(decimal exitPrice)
	{
		if (_longEntryPrice is decimal entry && _longEntryVolume > 0m)
		{
			var isLoss = exitPrice < entry;
			AddResult(_longLossHistory, isLoss, Math.Max(LongHistoryDepth, LongLossTrigger));
		}

		_longEntryPrice = null;
		_longEntryVolume = 0m;
	}

	private void RegisterShortResult(decimal exitPrice)
	{
		if (_shortEntryPrice is decimal entry && _shortEntryVolume > 0m)
		{
			var isLoss = exitPrice > entry;
			AddResult(_shortLossHistory, isLoss, Math.Max(ShortHistoryDepth, ShortLossTrigger));
		}

		_shortEntryPrice = null;
		_shortEntryVolume = 0m;
	}

	private static void AddResult(List<bool> history, bool isLoss, int limit)
	{
		history.Add(isLoss);

		var maxCount = Math.Max(1, limit * 2);
		if (history.Count > maxCount)
			history.RemoveRange(0, history.Count - maxCount);
	}

	private static void UpdateHistory(List<decimal> history, decimal value, int signalBar)
	{
		history.Insert(0, value);

		var maxHistory = Math.Max(2, signalBar + 2);
		if (history.Count > maxHistory)
			history.RemoveAt(history.Count - 1);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethods method, int length)
	{
		var maLength = Math.Max(1, length);
		return method switch
		{
			MovingAverageMethods.Simple => new SimpleMovingAverage { Length = maLength },
			MovingAverageMethods.Exponential => new ExponentialMovingAverage { Length = maLength },
			MovingAverageMethods.Smoothed => new SmoothedMovingAverage { Length = maLength },
			MovingAverageMethods.Weighted => new WeightedMovingAverage { Length = maLength },
			_ => new SimpleMovingAverage { Length = maLength },
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceTypes priceType)
	{
		return priceType switch
		{
			AppliedPriceTypes.Close => candle.ClosePrice,
			AppliedPriceTypes.Open => candle.OpenPrice,
			AppliedPriceTypes.High => candle.HighPrice,
			AppliedPriceTypes.Low => candle.LowPrice,
			AppliedPriceTypes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceTypes.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPriceTypes.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice,
		};
	}

	/// <summary>
	/// Supported moving average methods.
	/// </summary>
	public enum MovingAverageMethods
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average (SMMA).</summary>
		Smoothed,
		/// <summary>Linear weighted moving average.</summary>
		Weighted,
	}

	/// <summary>
	/// Price selection modes matching MetaTrader's applied price enumeration.
	/// </summary>
	public enum AppliedPriceTypes
	{
		/// <summary>Use the candle close price.</summary>
		Close,
		/// <summary>Use the candle open price.</summary>
		Open,
		/// <summary>Use the candle high price.</summary>
		High,
		/// <summary>Use the candle low price.</summary>
		Low,
		/// <summary>Use the median price (high + low) / 2.</summary>
		Median,
		/// <summary>Use the typical price (close + high + low) / 3.</summary>
		Typical,
		/// <summary>Use the weighted price (high + low + 2 * close) / 4.</summary>
		Weighted,
	}

	private sealed class ColorMaRsiTriggerCalculator
	{
		private readonly LengthIndicator<decimal> _fastMa;
		private readonly LengthIndicator<decimal> _slowMa;
		private readonly RelativeStrengthIndex _fastRsi;
		private readonly RelativeStrengthIndex _slowRsi;
		private readonly AppliedPriceTypes _fastMaPrice;
		private readonly AppliedPriceTypes _slowMaPrice;
		private readonly AppliedPriceTypes _fastRsiPrice;
		private readonly AppliedPriceTypes _slowRsiPrice;

		public ColorMaRsiTriggerCalculator(
			LengthIndicator<decimal> fastMa,
			LengthIndicator<decimal> slowMa,
			RelativeStrengthIndex fastRsi,
			RelativeStrengthIndex slowRsi,
			AppliedPriceTypes fastMaPrice,
			AppliedPriceTypes slowMaPrice,
			AppliedPriceTypes fastRsiPrice,
			AppliedPriceTypes slowRsiPrice)
		{
			_fastMa = fastMa ?? throw new ArgumentNullException(nameof(fastMa));
			_slowMa = slowMa ?? throw new ArgumentNullException(nameof(slowMa));
			_fastRsi = fastRsi ?? throw new ArgumentNullException(nameof(fastRsi));
			_slowRsi = slowRsi ?? throw new ArgumentNullException(nameof(slowRsi));
			_fastMaPrice = fastMaPrice;
			_slowMaPrice = slowMaPrice;
			_fastRsiPrice = fastRsiPrice;
			_slowRsiPrice = slowRsiPrice;
		}

		public decimal? Process(ICandleMessage candle)
		{
			var time = candle.CloseTime ?? candle.OpenTime;

			var fastMaValue = _fastMa.Process(GetAppliedPrice(candle, _fastMaPrice), time, true);
			if (!fastMaValue.IsFinal)
				return null;

			var slowMaValue = _slowMa.Process(GetAppliedPrice(candle, _slowMaPrice), time, true);
			if (!slowMaValue.IsFinal)
				return null;

			var fastRsiValue = _fastRsi.Process(GetAppliedPrice(candle, _fastRsiPrice), time, true);
			if (!fastRsiValue.IsFinal)
				return null;

			var slowRsiValue = _slowRsi.Process(GetAppliedPrice(candle, _slowRsiPrice), time, true);
			if (!slowRsiValue.IsFinal)
				return null;

			var maFast = fastMaValue.ToDecimal();
			var maSlow = slowMaValue.ToDecimal();
			var rsiFast = fastRsiValue.ToDecimal();
			var rsiSlow = slowRsiValue.ToDecimal();

			var score = 0m;

			if (maFast > maSlow)
				score = 1m;
			else if (maFast < maSlow)
				score = -1m;

			if (rsiFast > rsiSlow)
				score += 1m;
			else if (rsiFast < rsiSlow)
				score -= 1m;

			if (score > 1m)
				score = 1m;
			else if (score < -1m)
				score = -1m;

			return score;
		}
	}
}

