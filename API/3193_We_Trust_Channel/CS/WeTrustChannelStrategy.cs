namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Mean-reversion strategy converted from the MetaTrader expert "WE TRUST".
/// It trades a moving-average channel with standard deviation bands and optional signal reversal.
/// </summary>
public class WeTrustChannelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<int> _stdDevShift;
	private readonly StrategyParam<int> _signalBarOffset;
	private readonly StrategyParam<decimal> _channelIndentPips;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<CandlePrice> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private LinearWeightedMovingAverage _movingAverage;
	private StandardDeviation? _standardDeviation;
	private readonly List<decimal> _maHistory = new();
	private readonly List<decimal> _stdDevHistory = new();
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="WeTrustChannelStrategy"/> class.
	/// </summary>
	public WeTrustChannelStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Base volume for market entries.", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetNotNegative()
			.SetDisplay("Stop loss (pips)", "Initial stop-loss distance in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 60m)
			.SetNotNegative()
			.SetDisplay("Take profit (pips)", "Take-profit distance in pips.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetNotNegative()
			.SetDisplay("Trailing stop (pips)", "Trailing stop distance measured in pips.", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 10m)
			.SetNotNegative()
			.SetDisplay("Trailing step (pips)", "Step between trailing adjustments in pips.", "Risk");

		_maPeriod = Param(nameof(MaPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("MA period", "Length of the linear weighted moving average.", "Indicators");

		_maShift = Param(nameof(MaShift), 0)
			.SetNotNegative()
			.SetDisplay("MA shift", "Bars to shift the moving average when evaluating signals.", "Indicators");

		_stdDevPeriod = Param(nameof(StdDevPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("StdDev period", "Length of the standard deviation window.", "Indicators");

		_stdDevShift = Param(nameof(StdDevShift), 0)
			.SetNotNegative()
			.SetDisplay("StdDev shift", "Bars to shift the deviation value when evaluating signals.", "Indicators");

		_signalBarOffset = Param(nameof(SignalBarOffset), 1)
			.SetNotNegative()
			.SetDisplay("Signal bar offset", "Number of completed bars to look back for the channel check.", "Trading");

		_channelIndentPips = Param(nameof(ChannelIndentPips), 1m)
			.SetNotNegative()
			.SetDisplay("Channel indent (pips)", "Extra buffer added outside the deviation bands.", "Trading");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse signals", "Invert the direction of channel breakout signals.", "Trading");

		_closeOpposite = Param(nameof(CloseOpposite), false)
			.SetDisplay("Close opposite", "Close an opposing position before entering a new trade.", "Trading");

		_appliedPrice = Param(nameof(AppliedPrice), CandlePrice.Weighted)
			.SetDisplay("Applied price", "Price source fed into the indicators.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for signals.", "General");
	}

	/// <summary>
	/// Trade volume for new market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Step between trailing stop updates in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Bars to shift the moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Standard deviation window length.
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// Bars to shift the deviation value.
	/// </summary>
	public int StdDevShift
	{
		get => _stdDevShift.Value;
		set => _stdDevShift.Value = value;
	}

	/// <summary>
	/// Bars to look back when checking the channel breakout.
	/// </summary>
	public int SignalBarOffset
	{
		get => _signalBarOffset.Value;
		set => _signalBarOffset.Value = value;
	}

	/// <summary>
	/// Extra buffer outside the channel measured in pips.
	/// </summary>
	public decimal ChannelIndentPips
	{
		get => _channelIndentPips.Value;
		set => _channelIndentPips.Value = value;
	}

	/// <summary>
	/// Enables signal reversal when set to true.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Enables closing of opposite positions before opening new trades.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Price type supplied to the indicators.
	/// </summary>
	public CandlePrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Candle type requested from the data subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_movingAverage = null;
		_standardDeviation = null;
		_maHistory.Clear();
		_stdDevHistory.Clear();
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_movingAverage = new LinearWeightedMovingAverage { Length = MaPeriod };
		_standardDeviation = new StandardDeviation { Length = StdDevPeriod };

		_maHistory.Clear();
		_stdDevHistory.Clear();

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_movingAverage, _standardDeviation, ProcessCandle)
			.Start();

		var chartArea = CreateChartArea();
		if (chartArea != null)
		{
			DrawCandles(chartArea, subscription);
			DrawIndicator(chartArea, _movingAverage);
			DrawIndicator(chartArea, _standardDeviation);
			DrawOwnTrades(chartArea);
		}

		StartProtection(
			takeProfit: TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null,
			stopLoss: StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null,
			trailingStop: TrailingStopPips > 0m ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null,
			trailingStep: TrailingStopPips > 0m && TrailingStepPips > 0m ? new Unit(TrailingStepPips * _pipSize, UnitTypes.Absolute) : null,
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdDevValue)
	{
		if (_movingAverage == null || _standardDeviation == null)
			return;

		if (Volume != OrderVolume)
			Volume = OrderVolume;

		if (_movingAverage.Length != MaPeriod)
			_movingAverage.Length = MaPeriod;
		if (_standardDeviation.Length != StdDevPeriod)
			_standardDeviation.Length = StdDevPeriod;

		if (candle.State != CandleStates.Finished)
			return;

		_maHistory.Add(maValue);
		_stdDevHistory.Add(stdDevValue);

		TrimHistory(_maHistory);
		TrimHistory(_stdDevHistory);

		var maShiftIndex = MaShift + SignalBarOffset;
		var stdShiftIndex = StdDevShift + SignalBarOffset;

		var maForSignal = GetShiftedValue(_maHistory, maShiftIndex);
		var stdForSignal = GetShiftedValue(_stdDevHistory, stdShiftIndex);

		if (maForSignal is null || stdForSignal is null)
			return;

		var indent = ChannelIndentPips * _pipSize;
		var upper = maForSignal.Value + stdForSignal.Value + indent;
		var lower = maForSignal.Value - stdForSignal.Value - indent;

		var price = GetPrice(candle, AppliedPrice);

		var wantBuy = price < lower;
		var wantSell = price > upper;

		if (ReverseSignals)
		{
			(wantBuy, wantSell) = (wantSell, wantBuy);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (wantBuy)
		{
			if (CloseOpposite && Position < 0)
			{
				ClosePosition();
				return;
			}

			if (Position <= 0)
				BuyMarket(OrderVolume);
		}
		else if (wantSell)
		{
			if (CloseOpposite && Position > 0)
			{
				ClosePosition();
				return;
			}

			if (Position >= 0)
				SellMarket(OrderVolume);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? Security?.Step ?? 0m;
		if (step <= 0m)
			step = 1m;

		var decimals = Security?.Decimals ?? 0;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;

		return step * multiplier;
	}

	private void TrimHistory(List<decimal> history)
	{
		var capacity = Math.Max(Math.Max(MaShift, StdDevShift) + SignalBarOffset + 5, 10);
		if (history.Count <= capacity)
			return;

		history.RemoveRange(0, history.Count - capacity);
	}

	private static decimal? GetShiftedValue(List<decimal> history, int shift)
	{
		var index = history.Count - 1 - shift;
		if (index < 0 || index >= history.Count)
			return null;

		return history[index];
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePrice priceType)
	{
		return priceType switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}
}
