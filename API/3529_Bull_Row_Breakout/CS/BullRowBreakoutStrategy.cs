namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy converted from the "BULL row full EA" expert advisor.
/// </summary>
public class BullRowBreakoutStrategy : Strategy
{
	private readonly List<ICandleMessage> _candles = new();
	private readonly Queue<decimal> _stochasticHistory = new();
	private StochasticOscillator _stochastic = null!;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<TimeSpan> _candleTimeFrame;
	private readonly StrategyParam<int> _stopLossLookback;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _bearRowSize;
	private readonly StrategyParam<decimal> _bearMinBody;
	private readonly StrategyParam<RowSequenceMode> _bearRowMode;
	private readonly StrategyParam<int> _bearShift;
	private readonly StrategyParam<int> _bullRowSize;
	private readonly StrategyParam<decimal> _bullMinBody;
	private readonly StrategyParam<RowSequenceMode> _bullRowMode;
	private readonly StrategyParam<int> _bullShift;
	private readonly StrategyParam<int> _breakoutLookback;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<int> _stochasticRangePeriod;
	private readonly StrategyParam<decimal> _stochasticUpperLevel;
	private readonly StrategyParam<decimal> _stochasticLowerLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="BullRowBreakoutStrategy"/> class.
	/// </summary>
	public BullRowBreakoutStrategy()
	{
		_volume = Param(nameof(Volume), 0.01m)
		.SetDisplay("Volume", "Fixed trade volume", "Trading")
		.SetCanOptimize(true);

		_candleTimeFrame = Param(nameof(CandleTimeFrame), TimeSpan.FromHours(1))
		.SetDisplay("Timeframe", "Primary candle timeframe", "Market")
		.SetCanOptimize(false);

		_stopLossLookback = Param(nameof(StopLossLookback), 10)
		.SetDisplay("Stop loss bars", "Bars used to locate protective stop", "Risk")
		.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 100m)
		.SetDisplay("Take profit %", "Reward distance relative to stop", "Risk")
		.SetCanOptimize(true);

		_bearRowSize = Param(nameof(BearRowSize), 3)
		.SetDisplay("Bear row size", "Required consecutive bearish candles", "Pattern")
		.SetCanOptimize(true);

		_bearMinBody = Param(nameof(BearMinBody), 0m)
		.SetDisplay("Bear min body", "Minimum bearish candle body (price steps)", "Pattern")
		.SetCanOptimize(true);

		_bearRowMode = Param(nameof(BearRowMode), RowSequenceMode.Normal)
		.SetDisplay("Bear row mode", "Body size progression for bearish row", "Pattern")
		.SetCanOptimize(true);

		_bearShift = Param(nameof(BearShift), 3)
		.SetDisplay("Bear row shift", "How many bars back the bearish row starts", "Pattern")
		.SetCanOptimize(true);

		_bullRowSize = Param(nameof(BullRowSize), 2)
		.SetDisplay("Bull row size", "Required consecutive bullish candles", "Pattern")
		.SetCanOptimize(true);

		_bullMinBody = Param(nameof(BullMinBody), 0m)
		.SetDisplay("Bull min body", "Minimum bullish candle body (price steps)", "Pattern")
		.SetCanOptimize(true);

		_bullRowMode = Param(nameof(BullRowMode), RowSequenceMode.Normal)
		.SetDisplay("Bull row mode", "Body size progression for bullish row", "Pattern")
		.SetCanOptimize(true);

		_bullShift = Param(nameof(BullShift), 1)
		.SetDisplay("Bull row shift", "How many bars back the bullish row starts", "Pattern")
		.SetCanOptimize(true);

		_breakoutLookback = Param(nameof(BreakoutLookback), 10)
		.SetDisplay("Breakout lookback", "Bars checked for the breakout filter", "Pattern")
		.SetCanOptimize(true);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 40)
		.SetDisplay("Stochastic %K", "%K period", "Indicators")
		.SetCanOptimize(true);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 8)
		.SetDisplay("Stochastic %D", "%D period", "Indicators")
		.SetCanOptimize(true);

		_stochasticSlowing = Param(nameof(StochasticSlowing), 10)
		.SetDisplay("Stochastic slowing", "Smoothing applied to %K", "Indicators")
		.SetCanOptimize(true);

		_stochasticRangePeriod = Param(nameof(StochasticRangePeriod), 3)
		.SetDisplay("Stochastic bars", "Bars that must remain inside the oscillator channel", "Indicators")
		.SetCanOptimize(true);

		_stochasticUpperLevel = Param(nameof(StochasticUpperLevel), 70m)
		.SetDisplay("Stochastic upper", "Upper bound for the oscillator", "Indicators")
		.SetCanOptimize(true);

		_stochasticLowerLevel = Param(nameof(StochasticLowerLevel), 30m)
		.SetDisplay("Stochastic lower", "Lower bound for the oscillator", "Indicators")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Fixed trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Primary candle timeframe.
	/// </summary>
	public TimeSpan CandleTimeFrame
	{
		get => _candleTimeFrame.Value;
		set => _candleTimeFrame.Value = value;
	}

	/// <summary>
	/// Bars used to locate the stop price.
	/// </summary>
	public int StopLossLookback
	{
		get => _stopLossLookback.Value;
		set => _stopLossLookback.Value = value;
	}

	/// <summary>
	/// Take profit distance relative to the stop in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Bearish row length in candles.
	/// </summary>
	public int BearRowSize
	{
		get => _bearRowSize.Value;
		set => _bearRowSize.Value = value;
	}

	/// <summary>
	/// Minimum bearish body expressed in price steps.
	/// </summary>
	public decimal BearMinBody
	{
		get => _bearMinBody.Value;
		set => _bearMinBody.Value = value;
	}

	/// <summary>
	/// Bearish row body progression requirement.
	/// </summary>
	public RowSequenceMode BearRowMode
	{
		get => _bearRowMode.Value;
		set => _bearRowMode.Value = value;
	}

	/// <summary>
	/// Offset in bars where the bearish row starts.
	/// </summary>
	public int BearShift
	{
		get => _bearShift.Value;
		set => _bearShift.Value = value;
	}

	/// <summary>
	/// Bullish row length in candles.
	/// </summary>
	public int BullRowSize
	{
		get => _bullRowSize.Value;
		set => _bullRowSize.Value = value;
	}

	/// <summary>
	/// Minimum bullish body expressed in price steps.
	/// </summary>
	public decimal BullMinBody
	{
		get => _bullMinBody.Value;
		set => _bullMinBody.Value = value;
	}

	/// <summary>
	/// Bullish row body progression requirement.
	/// </summary>
	public RowSequenceMode BullRowMode
	{
		get => _bullRowMode.Value;
		set => _bullRowMode.Value = value;
	}

	/// <summary>
	/// Offset in bars where the bullish row starts.
	/// </summary>
	public int BullShift
	{
		get => _bullShift.Value;
		set => _bullShift.Value = value;
	}

	/// <summary>
	/// Lookback used to determine the breakout high.
	/// </summary>
	public int BreakoutLookback
	{
		get => _breakoutLookback.Value;
		set => _breakoutLookback.Value = value;
	}

	/// <summary>
	/// Stochastic %K length.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D length.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to %K.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Number of candles that must remain inside the Stochastic channel.
	/// </summary>
	public int StochasticRangePeriod
	{
		get => _stochasticRangePeriod.Value;
		set => _stochasticRangePeriod.Value = value;
	}

	/// <summary>
	/// Upper bound for the Stochastic filter.
	/// </summary>
	public decimal StochasticUpperLevel
	{
		get => _stochasticUpperLevel.Value;
		set => _stochasticUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower bound for the Stochastic filter.
	/// </summary>
	public decimal StochasticLowerLevel
	{
		get => _stochasticLowerLevel.Value;
		set => _stochasticLowerLevel.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			K = { Length = StochasticKPeriod },
			D = { Length = StochasticDPeriod },
			Smooth = StochasticSlowing
		};

		var series = new CandleSeries(typeof(TimeFrameCandleMessage), Security, CandleTimeFrame);
		var subscription = SubscribeCandles(series);
		subscription
		.BindEx(_stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (!stochasticValue.IsFinal || stoch.K is not decimal kValue || stoch.D is not decimal dValue)
		return;

		_candles.Add(candle);
		var maxNeeded = Math.Max(Math.Max(BearShift + BearRowSize - 1, BullShift + BullRowSize - 1), Math.Max(StopLossLookback, BreakoutLookback));
		if (_candles.Count > Math.Max(maxNeeded + 5, StochasticRangePeriod + 5))
		_candles.RemoveAt(0);

		_stochasticHistory.Enqueue(kValue);
		while (_stochasticHistory.Count > Math.Max(StochasticRangePeriod, 1))
		_stochasticHistory.Dequeue();

		ManageProtectiveLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position > 0m)
		return;

		if (!HasEnoughHistory())
		return;

		if (!HasBearRow())
		return;

		if (!HasBullRow())
		return;

		if (!HasBreakout())
		return;

		if (!HasStochasticCross(kValue, dValue))
		return;

		if (!IsStochasticContained())
		return;

		var volume = Volume;
		if (volume <= 0m)
		return;

		var stopPrice = CalculateStopPrice();
		if (stopPrice is null)
		return;

		var entryPrice = candle.ClosePrice;
		var risk = entryPrice - stopPrice.Value;
		if (risk <= 0m)
		return;

		var reward = risk * TakeProfitPercent / 100m;
		var takeProfitPrice = entryPrice + reward;

		if (BuyMarket(volume) is null)
		return;

		_stopPrice = stopPrice;
		_takeProfitPrice = takeProfitPrice;
	}

	private void ManageProtectiveLevels(ICandleMessage candle)
	{
		if (Position <= 0m)
		return;

		if (_stopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Math.Abs(Position));
			ResetProtection();
			return;
		}

		if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
		{
			SellMarket(Math.Abs(Position));
			ResetProtection();
			return;
		}
	}
	private void ResetProtection()
	{
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private bool HasEnoughHistory()
	{
		if (_candles.Count < Math.Max(BreakoutLookback, StopLossLookback))
		return false;

		var bearRequirement = BearShift + BearRowSize - 1;
		var bullRequirement = BullShift + BullRowSize - 1;
		var minCandles = Math.Max(bearRequirement, bullRequirement);
		return _candles.Count >= Math.Max(minCandles, 2);
	}

	private bool HasBearRow() => HasRow(BearRowSize, BearMinBody, BearRowMode, BearShift, isBullish: false);

	private bool HasBullRow() => HasRow(BullRowSize, BullMinBody, BullRowMode, BullShift, isBullish: true);

	private bool HasRow(int size, decimal minBody, RowSequenceMode mode, int shift, bool isBullish)
	{
		if (size <= 0 || shift <= 0)
		return false;

		var maxShift = shift + size - 1;
		if (_candles.Count < maxShift)
		return false;

		var bodyStep = Security?.PriceStep ?? 0m;
		if (bodyStep <= 0m)
		bodyStep = 1m;

		var minBodyValue = minBody * bodyStep;
		decimal previousBody = 0m;

		for (var i = 0; i < size; i++)
		{
			var candle = GetCandle(shift + i);
			var body = isBullish ? candle.ClosePrice - candle.OpenPrice : candle.OpenPrice - candle.ClosePrice;

			if (body <= 0m)
			return false;

			if (body < minBodyValue)
			return false;

			if (mode == RowSequenceMode.Bigger && previousBody > 0m && body <= previousBody)
			return false;

			if (mode == RowSequenceMode.Smaller && previousBody > 0m && body >= previousBody)
			return false;

			previousBody = body;
		}

		return true;
	}

	private bool HasBreakout()
	{
		if (BreakoutLookback <= 2)
		return false;

		var prevClose = GetCandle(1).ClosePrice;
		var highest = decimal.MinValue;

		for (var shift = 2; shift <= BreakoutLookback; shift++)
		{
			var candle = GetCandle(shift);
			highest = Math.Max(highest, candle.HighPrice);
		}

		return prevClose > highest;
	}

	private bool HasStochasticCross(decimal kValue, decimal dValue)
	{
		return kValue > dValue;
	}

	private bool IsStochasticContained()
	{
		if (StochasticRangePeriod <= 0)
		return true;

		if (_stochasticHistory.Count < StochasticRangePeriod)
		return false;

		return _stochasticHistory.All(v => v <= StochasticUpperLevel && v >= StochasticLowerLevel);
	}

	private decimal? CalculateStopPrice()
	{
		if (StopLossLookback <= 0)
		return null;

		var lowest = decimal.MaxValue;
		var bars = Math.Min(StopLossLookback, _candles.Count);
		for (var shift = 1; shift <= bars; shift++)
		{
			var candle = GetCandle(shift);
			lowest = Math.Min(lowest, candle.LowPrice);
		}

		return lowest == decimal.MaxValue ? null : lowest;
	}

	private ICandleMessage GetCandle(int shift)
	{
		return _candles[^shift];
	}
}

/// <summary>
/// Defines how candle bodies must evolve inside a row.
/// </summary>
public enum RowSequenceMode
{
	/// <summary>
	/// Only direction and minimum body size are checked.
	/// </summary>
	Normal,

	/// <summary>
	/// Each candle must have a larger body than the previous one.
	/// </summary>
	Bigger,

	/// <summary>
	/// Each candle must have a smaller body than the previous one.
	/// </summary>
	Smaller
}
