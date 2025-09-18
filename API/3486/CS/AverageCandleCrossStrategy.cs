using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that waits for a bullish or bearish candle close cross relative to a moving average while confirming the trend with two additional moving average filters.
/// Only one position can be open at a time and each entry immediately receives a protective stop-loss and take-profit derived from the stop distance.
/// </summary>
public class AverageCandleCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _firstTrendFastPeriod;
	private readonly StrategyParam<MaMethodOption> _firstTrendFastMethod;
	private readonly StrategyParam<int> _firstTrendSlowPeriod;
	private readonly StrategyParam<MaMethodOption> _firstTrendSlowMethod;
	private readonly StrategyParam<int> _secondTrendFastPeriod;
	private readonly StrategyParam<MaMethodOption> _secondTrendFastMethod;
	private readonly StrategyParam<int> _secondTrendSlowPeriod;
	private readonly StrategyParam<MaMethodOption> _secondTrendSlowMethod;
	private readonly StrategyParam<int> _bullCrossPeriod;
	private readonly StrategyParam<MaMethodOption> _bullCrossMethod;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _buyStopLossPips;
	private readonly StrategyParam<decimal> _buyTakeProfitPercent;
	private readonly StrategyParam<int> _firstTrendBearFastPeriod;
	private readonly StrategyParam<MaMethodOption> _firstTrendBearFastMethod;
	private readonly StrategyParam<int> _firstTrendBearSlowPeriod;
	private readonly StrategyParam<MaMethodOption> _firstTrendBearSlowMethod;
	private readonly StrategyParam<int> _secondTrendBearFastPeriod;
	private readonly StrategyParam<MaMethodOption> _secondTrendBearFastMethod;
	private readonly StrategyParam<int> _secondTrendBearSlowPeriod;
	private readonly StrategyParam<MaMethodOption> _secondTrendBearSlowMethod;
	private readonly StrategyParam<int> _bearCrossPeriod;
	private readonly StrategyParam<MaMethodOption> _bearCrossMethod;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<decimal> _sellStopLossPips;
	private readonly StrategyParam<decimal> _sellTakeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _pipSize;

	private LengthIndicator<decimal>? _firstTrendFastMa;
	private LengthIndicator<decimal>? _firstTrendSlowMa;
	private LengthIndicator<decimal>? _secondTrendFastMa;
	private LengthIndicator<decimal>? _secondTrendSlowMa;
	private LengthIndicator<decimal>? _bullCrossMa;
	private LengthIndicator<decimal>? _firstTrendBearFastMa;
	private LengthIndicator<decimal>? _firstTrendBearSlowMa;
	private LengthIndicator<decimal>? _secondTrendBearFastMa;
	private LengthIndicator<decimal>? _secondTrendBearSlowMa;
	private LengthIndicator<decimal>? _bearCrossMa;

	private decimal? _prevFirstTrendFast;
	private decimal? _prevFirstTrendSlow;
	private decimal? _prevSecondTrendFast;
	private decimal? _prevSecondTrendSlow;
	private decimal? _prevFirstTrendBearFast;
	private decimal? _prevFirstTrendBearSlow;
	private decimal? _prevSecondTrendBearFast;
	private decimal? _prevSecondTrendBearSlow;
	private decimal? _prevBullCross;
	private decimal? _prevPrevBullCross;
	private decimal? _prevBearCross;
	private decimal? _prevPrevBearCross;
	private decimal? _prevClose;
	private decimal? _prevPrevClose;
	private decimal? _prevPrevFirstTrendFast;
	private decimal? _prevPrevFirstTrendSlow;
	private decimal? _prevPrevSecondTrendFast;
	private decimal? _prevPrevSecondTrendSlow;
	private decimal? _prevPrevFirstTrendBearFast;
	private decimal? _prevPrevFirstTrendBearSlow;
	private decimal? _prevPrevSecondTrendBearFast;
	private decimal? _prevPrevSecondTrendBearSlow;

	private decimal _actualPipSize;

	private Order? _stopOrder;
	private Order? _takeProfitOrder;

	/// <summary>
	/// Initializes a new instance of <see cref="AverageCandleCrossStrategy"/>.
	/// </summary>
	public AverageCandleCrossStrategy()
	{
		_firstTrendFastPeriod = Param(nameof(FirstTrendFastPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("First Trend Fast Period", "Period of the fast trend filter moving average (buy side)", "Buy Filters")
		.SetCanOptimize();

		_firstTrendFastMethod = Param(nameof(FirstTrendFastMethod), MaMethodOption.Simple)
		.SetDisplay("First Trend Fast Method", "Smoothing method for the fast trend filter (buy side)", "Buy Filters")
		.SetCanOptimize();

		_firstTrendSlowPeriod = Param(nameof(FirstTrendSlowPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("First Trend Slow Period", "Period of the slow trend filter moving average (buy side)", "Buy Filters")
		.SetCanOptimize();

		_firstTrendSlowMethod = Param(nameof(FirstTrendSlowMethod), MaMethodOption.Simple)
		.SetDisplay("First Trend Slow Method", "Smoothing method for the slow trend filter (buy side)", "Buy Filters")
		.SetCanOptimize();

		_secondTrendFastPeriod = Param(nameof(SecondTrendFastPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Second Trend Fast Period", "Period of the secondary fast trend filter (buy side)", "Buy Filters")
		.SetCanOptimize();

		_secondTrendFastMethod = Param(nameof(SecondTrendFastMethod), MaMethodOption.Simple)
		.SetDisplay("Second Trend Fast Method", "Smoothing method for the secondary fast trend filter (buy side)", "Buy Filters")
		.SetCanOptimize();

		_secondTrendSlowPeriod = Param(nameof(SecondTrendSlowPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Second Trend Slow Period", "Period of the secondary slow trend filter (buy side)", "Buy Filters")
		.SetCanOptimize();

		_secondTrendSlowMethod = Param(nameof(SecondTrendSlowMethod), MaMethodOption.Simple)
		.SetDisplay("Second Trend Slow Method", "Smoothing method for the secondary slow trend filter (buy side)", "Buy Filters")
		.SetCanOptimize();

		_bullCrossPeriod = Param(nameof(BullCrossPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Bull Cross Period", "Period of the moving average used for bullish candle cross detection", "Buy Filters")
		.SetCanOptimize();

		_bullCrossMethod = Param(nameof(BullCrossMethod), MaMethodOption.Simple)
		.SetDisplay("Bull Cross Method", "Smoothing method for the candle cross moving average (buy side)", "Buy Filters")
		.SetCanOptimize();

		_buyVolume = Param(nameof(BuyVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Buy Volume", "Order volume for long entries", "Execution");

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Buy Stop Loss (pips)", "Stop-loss distance for long trades in MetaTrader pips", "Execution");

		_buyTakeProfitPercent = Param(nameof(BuyTakeProfitPercent), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Buy Take Profit (% of SL)", "Take-profit distance as a percentage of the stop distance for long trades", "Execution");

		_firstTrendBearFastPeriod = Param(nameof(FirstTrendBearFastPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("First Trend Fast Period (Sell)", "Period of the fast trend filter moving average (sell side)", "Sell Filters")
		.SetCanOptimize();

		_firstTrendBearFastMethod = Param(nameof(FirstTrendBearFastMethod), MaMethodOption.Simple)
		.SetDisplay("First Trend Fast Method (Sell)", "Smoothing method for the fast trend filter (sell side)", "Sell Filters")
		.SetCanOptimize();

		_firstTrendBearSlowPeriod = Param(nameof(FirstTrendBearSlowPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("First Trend Slow Period (Sell)", "Period of the slow trend filter moving average (sell side)", "Sell Filters")
		.SetCanOptimize();

		_firstTrendBearSlowMethod = Param(nameof(FirstTrendBearSlowMethod), MaMethodOption.Simple)
		.SetDisplay("First Trend Slow Method (Sell)", "Smoothing method for the slow trend filter (sell side)", "Sell Filters")
		.SetCanOptimize();

		_secondTrendBearFastPeriod = Param(nameof(SecondTrendBearFastPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Second Trend Fast Period (Sell)", "Period of the secondary fast trend filter (sell side)", "Sell Filters")
		.SetCanOptimize();

		_secondTrendBearFastMethod = Param(nameof(SecondTrendBearFastMethod), MaMethodOption.Simple)
		.SetDisplay("Second Trend Fast Method (Sell)", "Smoothing method for the secondary fast trend filter (sell side)", "Sell Filters")
		.SetCanOptimize();

		_secondTrendBearSlowPeriod = Param(nameof(SecondTrendBearSlowPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Second Trend Slow Period (Sell)", "Period of the secondary slow trend filter (sell side)", "Sell Filters")
		.SetCanOptimize();

		_secondTrendBearSlowMethod = Param(nameof(SecondTrendBearSlowMethod), MaMethodOption.Simple)
		.SetDisplay("Second Trend Slow Method (Sell)", "Smoothing method for the secondary slow trend filter (sell side)", "Sell Filters")
		.SetCanOptimize();

		_bearCrossPeriod = Param(nameof(BearCrossPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Bear Cross Period", "Period of the moving average used for bearish candle cross detection", "Sell Filters")
		.SetCanOptimize();

		_bearCrossMethod = Param(nameof(BearCrossMethod), MaMethodOption.Simple)
		.SetDisplay("Bear Cross Method", "Smoothing method for the candle cross moving average (sell side)", "Sell Filters")
		.SetCanOptimize();

		_sellVolume = Param(nameof(SellVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Sell Volume", "Order volume for short entries", "Execution");

		_sellStopLossPips = Param(nameof(SellStopLossPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Sell Stop Loss (pips)", "Stop-loss distance for short trades in MetaTrader pips", "Execution");

		_sellTakeProfitPercent = Param(nameof(SellTakeProfitPercent), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Sell Take Profit (% of SL)", "Take-profit distance as a percentage of the stop distance for short trades", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for calculations", "General");

		_pipSize = Param(nameof(PipSize), 0.0001m)
		.SetGreaterThanZero()
		.SetDisplay("Pip Size", "MetaTrader pip size used to convert pip distances into price offsets", "General");
	}

	/// <summary>
	/// Fast period of the first trend filter for the long setup.
	/// </summary>
	public int FirstTrendFastPeriod { get => _firstTrendFastPeriod.Value; set => _firstTrendFastPeriod.Value = value; }

	/// <summary>
	/// Moving average method of the first trend fast filter for the long setup.
	/// </summary>
	public MaMethodOption FirstTrendFastMethod { get => _firstTrendFastMethod.Value; set => _firstTrendFastMethod.Value = value; }

	/// <summary>
	/// Slow period of the first trend filter for the long setup.
	/// </summary>
	public int FirstTrendSlowPeriod { get => _firstTrendSlowPeriod.Value; set => _firstTrendSlowPeriod.Value = value; }

	/// <summary>
	/// Moving average method of the first trend slow filter for the long setup.
	/// </summary>
	public MaMethodOption FirstTrendSlowMethod { get => _firstTrendSlowMethod.Value; set => _firstTrendSlowMethod.Value = value; }

	/// <summary>
	/// Fast period of the second trend filter for the long setup.
	/// </summary>
	public int SecondTrendFastPeriod { get => _secondTrendFastPeriod.Value; set => _secondTrendFastPeriod.Value = value; }

	/// <summary>
	/// Moving average method of the second trend fast filter for the long setup.
	/// </summary>
	public MaMethodOption SecondTrendFastMethod { get => _secondTrendFastMethod.Value; set => _secondTrendFastMethod.Value = value; }

	/// <summary>
	/// Slow period of the second trend filter for the long setup.
	/// </summary>
	public int SecondTrendSlowPeriod { get => _secondTrendSlowPeriod.Value; set => _secondTrendSlowPeriod.Value = value; }

	/// <summary>
	/// Moving average method of the second trend slow filter for the long setup.
	/// </summary>
	public MaMethodOption SecondTrendSlowMethod { get => _secondTrendSlowMethod.Value; set => _secondTrendSlowMethod.Value = value; }

	/// <summary>
	/// Period of the moving average that participates in the bullish candle cross check.
	/// </summary>
	public int BullCrossPeriod { get => _bullCrossPeriod.Value; set => _bullCrossPeriod.Value = value; }

	/// <summary>
	/// Moving average method used in the bullish candle cross check.
	/// </summary>
	public MaMethodOption BullCrossMethod { get => _bullCrossMethod.Value; set => _bullCrossMethod.Value = value; }

	/// <summary>
	/// Volume of a single long trade.
	/// </summary>
	public decimal BuyVolume { get => _buyVolume.Value; set => _buyVolume.Value = value; }

	/// <summary>
	/// Stop loss distance for long trades in MetaTrader pips.
	/// </summary>
	public decimal BuyStopLossPips { get => _buyStopLossPips.Value; set => _buyStopLossPips.Value = value; }

	/// <summary>
	/// Take profit distance expressed as a percentage of the stop-loss distance for long trades.
	/// </summary>
	public decimal BuyTakeProfitPercent { get => _buyTakeProfitPercent.Value; set => _buyTakeProfitPercent.Value = value; }

	/// <summary>
	/// Fast period of the first trend filter for the short setup.
	/// </summary>
	public int FirstTrendBearFastPeriod { get => _firstTrendBearFastPeriod.Value; set => _firstTrendBearFastPeriod.Value = value; }

	/// <summary>
	/// Moving average method of the first trend fast filter for the short setup.
	/// </summary>
	public MaMethodOption FirstTrendBearFastMethod { get => _firstTrendBearFastMethod.Value; set => _firstTrendBearFastMethod.Value = value; }

	/// <summary>
	/// Slow period of the first trend filter for the short setup.
	/// </summary>
	public int FirstTrendBearSlowPeriod { get => _firstTrendBearSlowPeriod.Value; set => _firstTrendBearSlowPeriod.Value = value; }

	/// <summary>
	/// Moving average method of the first trend slow filter for the short setup.
	/// </summary>
	public MaMethodOption FirstTrendBearSlowMethod { get => _firstTrendBearSlowMethod.Value; set => _firstTrendBearSlowMethod.Value = value; }

	/// <summary>
	/// Fast period of the second trend filter for the short setup.
	/// </summary>
	public int SecondTrendBearFastPeriod { get => _secondTrendBearFastPeriod.Value; set => _secondTrendBearFastPeriod.Value = value; }

	/// <summary>
	/// Moving average method of the second trend fast filter for the short setup.
	/// </summary>
	public MaMethodOption SecondTrendBearFastMethod { get => _secondTrendBearFastMethod.Value; set => _secondTrendBearFastMethod.Value = value; }

	/// <summary>
	/// Slow period of the second trend filter for the short setup.
	/// </summary>
	public int SecondTrendBearSlowPeriod { get => _secondTrendBearSlowPeriod.Value; set => _secondTrendBearSlowPeriod.Value = value; }

	/// <summary>
	/// Moving average method of the second trend slow filter for the short setup.
	/// </summary>
	public MaMethodOption SecondTrendBearSlowMethod { get => _secondTrendBearSlowMethod.Value; set => _secondTrendBearSlowMethod.Value = value; }

	/// <summary>
	/// Period of the moving average that participates in the bearish candle cross check.
	/// </summary>
	public int BearCrossPeriod { get => _bearCrossPeriod.Value; set => _bearCrossPeriod.Value = value; }

	/// <summary>
	/// Moving average method used in the bearish candle cross check.
	/// </summary>
	public MaMethodOption BearCrossMethod { get => _bearCrossMethod.Value; set => _bearCrossMethod.Value = value; }

	/// <summary>
	/// Volume of a single short trade.
	/// </summary>
	public decimal SellVolume { get => _sellVolume.Value; set => _sellVolume.Value = value; }

	/// <summary>
	/// Stop loss distance for short trades in MetaTrader pips.
	/// </summary>
	public decimal SellStopLossPips { get => _sellStopLossPips.Value; set => _sellStopLossPips.Value = value; }

	/// <summary>
	/// Take profit distance expressed as a percentage of the stop-loss distance for short trades.
	/// </summary>
	public decimal SellTakeProfitPercent { get => _sellTakeProfitPercent.Value; set => _sellTakeProfitPercent.Value = value; }

	/// <summary>
	/// Candle series used to drive the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// MetaTrader pip size for price conversion.
	/// </summary>
	public decimal PipSize { get => _pipSize.Value; set => _pipSize.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_firstTrendFastMa = null;
		_firstTrendSlowMa = null;
		_secondTrendFastMa = null;
		_secondTrendSlowMa = null;
		_bullCrossMa = null;
		_firstTrendBearFastMa = null;
		_firstTrendBearSlowMa = null;
		_secondTrendBearFastMa = null;
		_secondTrendBearSlowMa = null;
		_bearCrossMa = null;

		_prevFirstTrendFast = null;
		_prevFirstTrendSlow = null;
		_prevSecondTrendFast = null;
		_prevSecondTrendSlow = null;
		_prevFirstTrendBearFast = null;
		_prevFirstTrendBearSlow = null;
		_prevSecondTrendBearFast = null;
		_prevSecondTrendBearSlow = null;
		_prevBullCross = null;
		_prevPrevBullCross = null;
		_prevBearCross = null;
		_prevPrevBearCross = null;
		_prevClose = null;
		_prevPrevClose = null;

		_actualPipSize = 0m;

		CancelProtection(ref _stopOrder);
		CancelProtection(ref _takeProfitOrder);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_firstTrendFastMa = CreateMovingAverage(FirstTrendFastMethod, FirstTrendFastPeriod);
		_firstTrendSlowMa = CreateMovingAverage(FirstTrendSlowMethod, FirstTrendSlowPeriod);
		_secondTrendFastMa = CreateMovingAverage(SecondTrendFastMethod, SecondTrendFastPeriod);
		_secondTrendSlowMa = CreateMovingAverage(SecondTrendSlowMethod, SecondTrendSlowPeriod);
		_bullCrossMa = CreateMovingAverage(BullCrossMethod, BullCrossPeriod);
		_firstTrendBearFastMa = CreateMovingAverage(FirstTrendBearFastMethod, FirstTrendBearFastPeriod);
		_firstTrendBearSlowMa = CreateMovingAverage(FirstTrendBearSlowMethod, FirstTrendBearSlowPeriod);
		_secondTrendBearFastMa = CreateMovingAverage(SecondTrendBearFastMethod, SecondTrendBearFastPeriod);
		_secondTrendBearSlowMa = CreateMovingAverage(SecondTrendBearSlowMethod, SecondTrendBearSlowPeriod);
		_bearCrossMa = CreateMovingAverage(BearCrossMethod, BearCrossPeriod);

		_actualPipSize = PipSize > 0m ? PipSize : Security?.PriceStep ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _firstTrendFastMa!);
			DrawIndicator(area, _firstTrendSlowMa!);
			DrawIndicator(area, _secondTrendFastMa!);
			DrawIndicator(area, _secondTrendSlowMa!);
			DrawIndicator(area, _bullCrossMa!);
			DrawIndicator(area, _firstTrendBearFastMa!);
			DrawIndicator(area, _firstTrendBearSlowMa!);
			DrawIndicator(area, _secondTrendBearFastMa!);
			DrawIndicator(area, _secondTrendBearSlowMa!);
			DrawIndicator(area, _bearCrossMa!);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_actualPipSize <= 0m)
		_actualPipSize = PipSize > 0m ? PipSize : Security?.PriceStep ?? 0m;

		var currentTime = candle.OpenTime;
		var closePrice = candle.ClosePrice;

		var firstFast = ProcessIndicator(_firstTrendFastMa, closePrice, currentTime);
		var firstSlow = ProcessIndicator(_firstTrendSlowMa, closePrice, currentTime);
		var secondFast = ProcessIndicator(_secondTrendFastMa, closePrice, currentTime);
		var secondSlow = ProcessIndicator(_secondTrendSlowMa, closePrice, currentTime);
		var bullCross = ProcessIndicator(_bullCrossMa, closePrice, currentTime);
		var firstBearFast = ProcessIndicator(_firstTrendBearFastMa, closePrice, currentTime);
		var firstBearSlow = ProcessIndicator(_firstTrendBearSlowMa, closePrice, currentTime);
		var secondBearFast = ProcessIndicator(_secondTrendBearFastMa, closePrice, currentTime);
		var secondBearSlow = ProcessIndicator(_secondTrendBearSlowMa, closePrice, currentTime);
		var bearCross = ProcessIndicator(_bearCrossMa, closePrice, currentTime);

		var canCheckLong = _prevFirstTrendFast.HasValue && _prevFirstTrendSlow.HasValue &&
		_prevSecondTrendFast.HasValue && _prevSecondTrendSlow.HasValue &&
		_prevBullCross.HasValue && _prevPrevBullCross.HasValue &&
		_prevClose.HasValue && _prevPrevClose.HasValue;

		var canCheckShort = _prevFirstTrendBearFast.HasValue && _prevFirstTrendBearSlow.HasValue &&
		_prevSecondTrendBearFast.HasValue && _prevSecondTrendBearSlow.HasValue &&
		_prevBearCross.HasValue && _prevPrevBearCross.HasValue &&
		_prevClose.HasValue && _prevPrevClose.HasValue;

		var bullishTrend = canCheckLong &&
		_prevFirstTrendFast!.Value > _prevFirstTrendSlow!.Value &&
		_prevSecondTrendFast!.Value > _prevSecondTrendSlow!.Value;

		var bullishCross = canCheckLong &&
		_prevPrevClose!.Value <= _prevPrevBullCross!.Value &&
		_prevClose!.Value > _prevBullCross!.Value;

		var bearishTrend = canCheckShort &&
		_prevFirstTrendBearFast!.Value < _prevFirstTrendBearSlow!.Value &&
		_prevSecondTrendBearFast!.Value < _prevSecondTrendBearSlow!.Value;

		var bearishCross = canCheckShort &&
		_prevPrevClose!.Value >= _prevPrevBearCross!.Value &&
		_prevClose!.Value < _prevBearCross!.Value;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			bullishTrend = bearishTrend = false;
			bullishCross = bearishCross = false;
		}

		if (Position == 0m)
		{
			if (bullishTrend && bullishCross)
			{
				EnterPosition(true, closePrice);
			}
			else if (bearishTrend && bearishCross)
			{
				EnterPosition(false, closePrice);
			}
		}

		UpdateHistory(ref _prevPrevFirstTrendFast, ref _prevFirstTrendFast, firstFast);
		UpdateHistory(ref _prevPrevFirstTrendSlow, ref _prevFirstTrendSlow, firstSlow);
		UpdateHistory(ref _prevPrevSecondTrendFast, ref _prevSecondTrendFast, secondFast);
		UpdateHistory(ref _prevPrevSecondTrendSlow, ref _prevSecondTrendSlow, secondSlow);
		UpdateHistory(ref _prevPrevBullCross, ref _prevBullCross, bullCross);
		UpdateHistory(ref _prevPrevFirstTrendBearFast, ref _prevFirstTrendBearFast, firstBearFast);
		UpdateHistory(ref _prevPrevFirstTrendBearSlow, ref _prevFirstTrendBearSlow, firstBearSlow);
		UpdateHistory(ref _prevPrevSecondTrendBearFast, ref _prevSecondTrendBearFast, secondBearFast);
		UpdateHistory(ref _prevPrevSecondTrendBearSlow, ref _prevSecondTrendBearSlow, secondBearSlow);
		UpdateHistory(ref _prevPrevBearCross, ref _prevBearCross, bearCross);
		UpdateHistory(ref _prevPrevClose, ref _prevClose, closePrice);
	}

	private void EnterPosition(bool isLong, decimal entryPrice)
	{
		var volume = isLong ? BuyVolume : SellVolume;
		if (volume <= 0m)
		return;

		CancelProtection(ref _stopOrder);
		CancelProtection(ref _takeProfitOrder);

		if (isLong)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		var stopDistance = GetStopDistance(isLong);
		var takeDistance = GetTakeDistance(isLong, stopDistance);
		var normalizedEntry = NormalizePrice(entryPrice);

		if (stopDistance > 0m)
		{
			var stopPrice = NormalizePrice(isLong ? normalizedEntry - stopDistance : normalizedEntry + stopDistance);
			_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
		}

		if (takeDistance > 0m)
		{
			var takePrice = NormalizePrice(isLong ? normalizedEntry + takeDistance : normalizedEntry - takeDistance);
			_takeProfitOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
		}
	}

	private decimal GetStopDistance(bool isLong)
	{
		var pips = isLong ? BuyStopLossPips : SellStopLossPips;
		return pips > 0m && _actualPipSize > 0m ? pips * _actualPipSize : 0m;
	}

	private decimal GetTakeDistance(bool isLong, decimal stopDistance)
	{
		if (stopDistance <= 0m)
		return 0m;

		var percent = isLong ? BuyTakeProfitPercent : SellTakeProfitPercent;
		return percent > 0m ? stopDistance * (percent / 100m) : 0m;
	}

	private static decimal? ProcessIndicator(LengthIndicator<decimal>? indicator, decimal input, DateTimeOffset time)
	{
		if (indicator == null)
		return null;

		var value = indicator.Process(input, time, true);
		return value.IsFinal ? value.ToDecimal() : (decimal?)null;
	}

	private static void UpdateHistory(ref decimal? previousPrevious, ref decimal? previous, decimal? current)
	{
		if (current.HasValue)
		{
			previousPrevious = previous;
			previous = current;
		}
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private void CancelProtection(ref Order? order)
	{
		if (order == null)
		return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.Security != Security)
		return;

		if (order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			if (_stopOrder == order)
			_stopOrder = null;

			if (_takeProfitOrder == order)
			_takeProfitOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtection(ref _stopOrder);
			CancelProtection(ref _takeProfitOrder);
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelProtection(ref _stopOrder);
		CancelProtection(ref _takeProfitOrder);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethodOption method, int period)
	{
		return method switch
		{
			MaMethodOption.Simple => new SimpleMovingAverage { Length = period },
			MaMethodOption.Exponential => new ExponentialMovingAverage { Length = period },
			MaMethodOption.Smoothed => new SmoothedMovingAverage { Length = period },
			MaMethodOption.LinearWeighted => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
		};
	}

	/// <summary>
	/// Moving average smoothing methods supported by the strategy.
	/// </summary>
	public enum MaMethodOption
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		LinearWeighted
	}

}
