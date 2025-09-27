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
/// Recreates the Exp_ColorMaRsi-Trigger_Duplex.mq5 expert advisor using the high level StockSharp API.
/// The strategy runs two independent MaRsi-Trigger blocks: one dedicated to long signals and another to short signals.
/// Each block evaluates the colour code (+1 / 0 / -1) produced by combining moving average and RSI comparisons on a configurable timeframe.
/// </summary>
public class ColorMaRsiTriggerDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<decimal> _longVolume;
	private readonly StrategyParam<bool> _longAllowOpen;
	private readonly StrategyParam<bool> _longAllowClose;
	private readonly StrategyParam<int> _longStopLossPoints;
	private readonly StrategyParam<int> _longTakeProfitPoints;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<int> _longRsiPeriod;
	private readonly StrategyParam<int> _longRsiLongPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<int> _longMaLongPeriod;
	private readonly StrategyParam<AppliedPriceType> _longRsiPrice;
	private readonly StrategyParam<AppliedPriceType> _longRsiLongPrice;
	private readonly StrategyParam<AppliedPriceType> _longMaPrice;
	private readonly StrategyParam<AppliedPriceType> _longMaLongPrice;
	private readonly StrategyParam<MovingAverageMethod> _longMaType;
	private readonly StrategyParam<MovingAverageMethod> _longMaLongType;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<decimal> _shortVolume;
	private readonly StrategyParam<bool> _shortAllowOpen;
	private readonly StrategyParam<bool> _shortAllowClose;
	private readonly StrategyParam<int> _shortStopLossPoints;
	private readonly StrategyParam<int> _shortTakeProfitPoints;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<int> _shortRsiPeriod;
	private readonly StrategyParam<int> _shortRsiLongPeriod;
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _shortMaLongPeriod;
	private readonly StrategyParam<AppliedPriceType> _shortRsiPrice;
	private readonly StrategyParam<AppliedPriceType> _shortRsiLongPrice;
	private readonly StrategyParam<AppliedPriceType> _shortMaPrice;
	private readonly StrategyParam<AppliedPriceType> _shortMaLongPrice;
	private readonly StrategyParam<MovingAverageMethod> _shortMaType;
	private readonly StrategyParam<MovingAverageMethod> _shortMaLongType;

	private ColorMaRsiTriggerCalculator _longCalculator = null!;
	private ColorMaRsiTriggerCalculator _shortCalculator = null!;

	private readonly List<decimal> _longHistory = new();
	private readonly List<decimal> _shortHistory = new();

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ColorMaRsiTriggerDuplexStrategy"/> class.
	/// </summary>
	public ColorMaRsiTriggerDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Time-frame used by the long MaRsi-Trigger block", "Long Block");

		_longVolume = Param(nameof(LongVolume), 0.1m)
			.SetDisplay("Long Volume", "Market volume opened by the long block", "Long Block");

		_longAllowOpen = Param(nameof(LongAllowOpen), true)
			.SetDisplay("Allow Long Entries", "Enable opening new long positions", "Long Block");

		_longAllowClose = Param(nameof(LongAllowClose), true)
			.SetDisplay("Allow Long Closes", "Enable closing long positions on opposite signals", "Long Block");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000)
			.SetDisplay("Long Stop Loss", "Protective stop in price steps for long positions", "Long Block");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000)
			.SetDisplay("Long Take Profit", "Profit target in price steps for long positions", "Long Block");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetDisplay("Long Signal Bar", "Shift applied when reading the MaRsi-Trigger buffer", "Long Block");

		_longRsiPeriod = Param(nameof(LongRsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long RSI Period", "Fast RSI length for the long block", "Long Block");

		_longRsiLongPeriod = Param(nameof(LongRsiLongPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Long RSI Slow Period", "Slow RSI length for the long block", "Long Block");

		_longMaPeriod = Param(nameof(LongMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Period", "Fast moving average length for the long block", "Long Block");

		_longMaLongPeriod = Param(nameof(LongMaLongPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Slow Period", "Slow moving average length for the long block", "Long Block");

		_longRsiPrice = Param(nameof(LongRsiPrice), AppliedPriceType.Weighted)
			.SetDisplay("Long RSI Price", "Price mode fed into the fast RSI", "Long Block");

		_longRsiLongPrice = Param(nameof(LongRsiLongPrice), AppliedPriceType.Median)
			.SetDisplay("Long RSI Slow Price", "Price mode fed into the slow RSI", "Long Block");

		_longMaPrice = Param(nameof(LongMaPrice), AppliedPriceType.Close)
			.SetDisplay("Long MA Price", "Price mode fed into the fast moving average", "Long Block");

		_longMaLongPrice = Param(nameof(LongMaLongPrice), AppliedPriceType.Close)
			.SetDisplay("Long MA Slow Price", "Price mode fed into the slow moving average", "Long Block");

		_longMaType = Param(nameof(LongMaType), MovingAverageMethod.Exponential)
			.SetDisplay("Long MA Type", "Smoothing algorithm for the fast moving average", "Long Block");

		_longMaLongType = Param(nameof(LongMaLongType), MovingAverageMethod.Exponential)
			.SetDisplay("Long MA Slow Type", "Smoothing algorithm for the slow moving average", "Long Block");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Time-frame used by the short MaRsi-Trigger block", "Short Block");

		_shortVolume = Param(nameof(ShortVolume), 0.1m)
			.SetDisplay("Short Volume", "Market volume opened by the short block", "Short Block");

		_shortAllowOpen = Param(nameof(ShortAllowOpen), true)
			.SetDisplay("Allow Short Entries", "Enable opening new short positions", "Short Block");

		_shortAllowClose = Param(nameof(ShortAllowClose), true)
			.SetDisplay("Allow Short Closes", "Enable closing short positions on opposite signals", "Short Block");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000)
			.SetDisplay("Short Stop Loss", "Protective stop in price steps for short positions", "Short Block");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000)
			.SetDisplay("Short Take Profit", "Profit target in price steps for short positions", "Short Block");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetDisplay("Short Signal Bar", "Shift applied when reading the MaRsi-Trigger buffer", "Short Block");

		_shortRsiPeriod = Param(nameof(ShortRsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short RSI Period", "Fast RSI length for the short block", "Short Block");

		_shortRsiLongPeriod = Param(nameof(ShortRsiLongPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Short RSI Slow Period", "Slow RSI length for the short block", "Short Block");

		_shortMaPeriod = Param(nameof(ShortMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Period", "Fast moving average length for the short block", "Short Block");

		_shortMaLongPeriod = Param(nameof(ShortMaLongPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Slow Period", "Slow moving average length for the short block", "Short Block");

		_shortRsiPrice = Param(nameof(ShortRsiPrice), AppliedPriceType.Weighted)
			.SetDisplay("Short RSI Price", "Price mode fed into the fast RSI", "Short Block");

		_shortRsiLongPrice = Param(nameof(ShortRsiLongPrice), AppliedPriceType.Median)
			.SetDisplay("Short RSI Slow Price", "Price mode fed into the slow RSI", "Short Block");

		_shortMaPrice = Param(nameof(ShortMaPrice), AppliedPriceType.Close)
			.SetDisplay("Short MA Price", "Price mode fed into the fast moving average", "Short Block");

		_shortMaLongPrice = Param(nameof(ShortMaLongPrice), AppliedPriceType.Close)
			.SetDisplay("Short MA Slow Price", "Price mode fed into the slow moving average", "Short Block");

		_shortMaType = Param(nameof(ShortMaType), MovingAverageMethod.Exponential)
			.SetDisplay("Short MA Type", "Smoothing algorithm for the fast moving average", "Short Block");

		_shortMaLongType = Param(nameof(ShortMaLongType), MovingAverageMethod.Exponential)
			.SetDisplay("Short MA Slow Type", "Smoothing algorithm for the slow moving average", "Short Block");
	}
	/// <summary>
	/// Candle type that feeds the long MaRsi-Trigger block.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Volume used when opening long positions.
	/// </summary>
	public decimal LongVolume
	{
		get => _longVolume.Value;
		set => _longVolume.Value = value;
	}

	/// <summary>
	/// Enables opening of new long positions.
	/// </summary>
	public bool LongAllowOpen
	{
		get => _longAllowOpen.Value;
		set => _longAllowOpen.Value = value;
	}

	/// <summary>
	/// Enables closing of existing long positions.
	/// </summary>
	public bool LongAllowClose
	{
		get => _longAllowClose.Value;
		set => _longAllowClose.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps for long trades.
	/// </summary>
	public int LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps for long trades.
	/// </summary>
	public int LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of completed bars used when sampling the indicator value for long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Fast RSI length for the long block.
	/// </summary>
	public int LongRsiPeriod
	{
		get => _longRsiPeriod.Value;
		set => _longRsiPeriod.Value = value;
	}

	/// <summary>
	/// Slow RSI length for the long block.
	/// </summary>
	public int LongRsiLongPeriod
	{
		get => _longRsiLongPeriod.Value;
		set => _longRsiLongPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average length for the long block.
	/// </summary>
	public int LongMaPeriod
	{
		get => _longMaPeriod.Value;
		set => _longMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average length for the long block.
	/// </summary>
	public int LongMaLongPeriod
	{
		get => _longMaLongPeriod.Value;
		set => _longMaLongPeriod.Value = value;
	}

	/// <summary>
	/// Price source used by the fast RSI in the long block.
	/// </summary>
	public AppliedPriceType LongRsiPrice
	{
		get => _longRsiPrice.Value;
		set => _longRsiPrice.Value = value;
	}

	/// <summary>
	/// Price source used by the slow RSI in the long block.
	/// </summary>
	public AppliedPriceType LongRsiLongPrice
	{
		get => _longRsiLongPrice.Value;
		set => _longRsiLongPrice.Value = value;
	}

	/// <summary>
	/// Price source used by the fast moving average in the long block.
	/// </summary>
	public AppliedPriceType LongMaPrice
	{
		get => _longMaPrice.Value;
		set => _longMaPrice.Value = value;
	}

	/// <summary>
	/// Price source used by the slow moving average in the long block.
	/// </summary>
	public AppliedPriceType LongMaLongPrice
	{
		get => _longMaLongPrice.Value;
		set => _longMaLongPrice.Value = value;
	}

	/// <summary>
	/// Moving average method applied to the fast moving average in the long block.
	/// </summary>
	public MovingAverageMethod LongMaType
	{
		get => _longMaType.Value;
		set => _longMaType.Value = value;
	}

	/// <summary>
	/// Moving average method applied to the slow moving average in the long block.
	/// </summary>
	public MovingAverageMethod LongMaLongType
	{
		get => _longMaLongType.Value;
		set => _longMaLongType.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the short MaRsi-Trigger block.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Volume used when opening short positions.
	/// </summary>
	public decimal ShortVolume
	{
		get => _shortVolume.Value;
		set => _shortVolume.Value = value;
	}

	/// <summary>
	/// Enables opening of new short positions.
	/// </summary>
	public bool ShortAllowOpen
	{
		get => _shortAllowOpen.Value;
		set => _shortAllowOpen.Value = value;
	}

	/// <summary>
	/// Enables closing of existing short positions.
	/// </summary>
	public bool ShortAllowClose
	{
		get => _shortAllowClose.Value;
		set => _shortAllowClose.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps for short trades.
	/// </summary>
	public int ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps for short trades.
	/// </summary>
	public int ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of completed bars used when sampling the indicator value for short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Fast RSI length for the short block.
	/// </summary>
	public int ShortRsiPeriod
	{
		get => _shortRsiPeriod.Value;
		set => _shortRsiPeriod.Value = value;
	}

	/// <summary>
	/// Slow RSI length for the short block.
	/// </summary>
	public int ShortRsiLongPeriod
	{
		get => _shortRsiLongPeriod.Value;
		set => _shortRsiLongPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average length for the short block.
	/// </summary>
	public int ShortMaPeriod
	{
		get => _shortMaPeriod.Value;
		set => _shortMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average length for the short block.
	/// </summary>
	public int ShortMaLongPeriod
	{
		get => _shortMaLongPeriod.Value;
		set => _shortMaLongPeriod.Value = value;
	}

	/// <summary>
	/// Price source used by the fast RSI in the short block.
	/// </summary>
	public AppliedPriceType ShortRsiPrice
	{
		get => _shortRsiPrice.Value;
		set => _shortRsiPrice.Value = value;
	}

	/// <summary>
	/// Price source used by the slow RSI in the short block.
	/// </summary>
	public AppliedPriceType ShortRsiLongPrice
	{
		get => _shortRsiLongPrice.Value;
		set => _shortRsiLongPrice.Value = value;
	}

	/// <summary>
	/// Price source used by the fast moving average in the short block.
	/// </summary>
	public AppliedPriceType ShortMaPrice
	{
		get => _shortMaPrice.Value;
		set => _shortMaPrice.Value = value;
	}

	/// <summary>
	/// Price source used by the slow moving average in the short block.
	/// </summary>
	public AppliedPriceType ShortMaLongPrice
	{
		get => _shortMaLongPrice.Value;
		set => _shortMaLongPrice.Value = value;
	}

	/// <summary>
	/// Moving average method applied to the fast moving average in the short block.
	/// </summary>
	public MovingAverageMethod ShortMaType
	{
		get => _shortMaType.Value;
		set => _shortMaType.Value = value;
	}

	/// <summary>
	/// Moving average method applied to the slow moving average in the short block.
	/// </summary>
	public MovingAverageMethod ShortMaLongType
	{
		get => _shortMaLongType.Value;
		set => _shortMaLongType.Value = value;
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
		_longEntryPrice = null;
		_shortEntryPrice = null;
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

		SubscribeCandles(LongCandleType)
			.WhenCandlesFinished(ProcessLongCandle)
			.Start();

		if (LongCandleType == ShortCandleType)
			return;

		SubscribeCandles(ShortCandleType)
			.WhenCandlesFinished(ProcessShortCandle)
			.Start();
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

		if (LongAllowClose && Position > 0 && older < 0m)
			CloseLong();

		if (LongAllowOpen && Position <= 0 && older > 0m && recent <= 0m)
			OpenLong(candle.ClosePrice);
	}

	private void EvaluateShortSignals(ICandleMessage candle)
	{
		if (_shortHistory.Count <= ShortSignalBar + 1)
			return;

		var recent = _shortHistory[ShortSignalBar];
		var older = _shortHistory[ShortSignalBar + 1];

		if (ShortAllowClose && Position < 0 && older > 0m)
			CloseShort();

		if (ShortAllowOpen && Position >= 0 && older < 0m && recent >= 0m)
			OpenShort(candle.ClosePrice);
	}

	private void OpenLong(decimal entryPrice)
	{
		if (LongVolume <= 0m)
			return;

		if (Position < 0m)
		{
			if (!ShortAllowClose)
				return;

			var coverVolume = Math.Abs(Position);
			if (coverVolume > 0m)
			{
				BuyMarket(coverVolume);
				_shortEntryPrice = null;
			}
		}

		BuyMarket(LongVolume);
		_longEntryPrice = entryPrice;
	}

	private void OpenShort(decimal entryPrice)
	{
		if (ShortVolume <= 0m)
			return;

		if (Position > 0m)
		{
			if (!LongAllowClose)
				return;

			var coverVolume = Position;
			if (coverVolume > 0m)
			{
				SellMarket(coverVolume);
				_longEntryPrice = null;
			}
		}

		SellMarket(ShortVolume);
		_shortEntryPrice = entryPrice;
	}

	private void CloseLong()
	{
		if (Position <= 0m)
			return;

		SellMarket(Position);
		_longEntryPrice = null;
	}

	private void CloseShort()
	{
		if (Position >= 0m)
			return;

		BuyMarket(Math.Abs(Position));
		_shortEntryPrice = null;
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;

		if (Position > 0m && _longEntryPrice.HasValue)
		{
			var stop = LongStopLossPoints > 0 ? _longEntryPrice.Value - LongStopLossPoints * step : (decimal?)null;
			var take = LongTakeProfitPoints > 0 ? _longEntryPrice.Value + LongTakeProfitPoints * step : (decimal?)null;

			if (stop.HasValue && candle.LowPrice <= stop.Value)
			{
				CloseLong();
				return;
			}

			if (take.HasValue && candle.HighPrice >= take.Value)
				CloseLong();
		}
		else if (Position < 0m && _shortEntryPrice.HasValue)
		{
			var stop = ShortStopLossPoints > 0 ? _shortEntryPrice.Value + ShortStopLossPoints * step : (decimal?)null;
			var take = ShortTakeProfitPoints > 0 ? _shortEntryPrice.Value - ShortTakeProfitPoints * step : (decimal?)null;

			if (stop.HasValue && candle.HighPrice >= stop.Value)
			{
				CloseShort();
				return;
			}

			if (take.HasValue && candle.LowPrice <= take.Value)
				CloseShort();
		}
	}
	private static void UpdateHistory(List<decimal> history, decimal value, int signalBar)
	{
		history.Insert(0, value);

		var maxHistory = Math.Max(2, signalBar + 2);
		if (history.Count > maxHistory)
			history.RemoveAt(history.Count - 1);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType priceType)
	{
		return priceType switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice,
		};
	}

	/// <summary>
	/// Supported moving average smoothing algorithms.
	/// </summary>
	public enum MovingAverageMethod
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
	/// Price selection modes compatible with MetaTrader's ENUM_APPLIED_PRICE.
	/// </summary>
	public enum AppliedPriceType
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
		private readonly AppliedPriceType _fastMaPrice;
		private readonly AppliedPriceType _slowMaPrice;
		private readonly AppliedPriceType _fastRsiPrice;
		private readonly AppliedPriceType _slowRsiPrice;

		public ColorMaRsiTriggerCalculator(
			LengthIndicator<decimal> fastMa,
			LengthIndicator<decimal> slowMa,
			RelativeStrengthIndex fastRsi,
			RelativeStrengthIndex slowRsi,
			AppliedPriceType fastMaPrice,
			AppliedPriceType slowMaPrice,
			AppliedPriceType fastRsiPrice,
			AppliedPriceType slowRsiPrice)
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
			var time = candle.CloseTime;

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

