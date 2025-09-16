using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BrainTrend1 signal strategy converted from the ASCV MetaTrader expert.
/// Uses ATR, Stochastic Oscillator, and Jurik moving average to detect trend flips.
/// Applies optional stop loss, take profit, and trailing stop based on pip distances.
/// </summary>
public class AscvBrainTrendSignalStrategy : Strategy
{
	private const decimal AtrRangeDivisor = 2.3m;
	private const decimal UpperThreshold = 53m;
	private const decimal LowerThreshold = 47m;

	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private StochasticOscillator _stochastic;
	private JurikMovingAverage _jmaClose;

	private decimal? _prevJmaClose;
	private decimal? _prevPrevJmaClose;
	private int _patternState;
	private int _trendDirection;
	private bool _prevBuySignal;
	private bool _prevSellSignal;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="AscvBrainTrendSignalStrategy"/> class.
	/// </summary>
	public AscvBrainTrendSignalStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Length used for Average True Range", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_stochasticPeriod = Param(nameof(StochasticPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Base period for Stochastic oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 30, 6);

		_jmaLength = Param(nameof(JmaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Length of Jurik moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_stopLossPips = Param(nameof(StopLossPips), 15)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 46)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 50, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimum price move to adjust trailing stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert buy and sell logic", "Trading Rules");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe for the strategy", "General");

		Volume = 1m;
	}

	/// <summary>
	/// ATR averaging period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic base period.
	/// </summary>
	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// Jurik moving average length.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing adjustment step in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Reverse trading signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_atr = null;
		_stochastic = null;
		_jmaClose = null;

		_prevJmaClose = null;
		_prevPrevJmaClose = null;
		_patternState = 0;
		_trendDirection = 0;
		_prevBuySignal = false;
		_prevSellSignal = false;

		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_stochastic = new StochasticOscillator { Length = StochasticPeriod };
		_jmaClose = new JurikMovingAverage { Length = JmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx([_atr, _stochastic], ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _jmaClose);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closedThisBar = ManageOpenPosition(candle);

		if (!TryGetIndicatorData(candle, values, out var data))
			return;

		var shouldBuy = (!ReverseSignals && _prevBuySignal) || (ReverseSignals && _prevSellSignal);
		var shouldSell = (!ReverseSignals && _prevSellSignal) || (ReverseSignals && _prevBuySignal);

		if (!closedThisBar && IsFormedAndOnlineAndAllowTrading())
		{
			if (shouldBuy && Position <= 0)
				TryEnterLong(candle);
			else if (shouldSell && Position >= 0)
				TryEnterShort(candle);
		}

		UpdateSignalState(data);
	}

	private bool TryGetIndicatorData(ICandleMessage candle, IIndicatorValue[] values, out IndicatorData data)
	{
		data = default;

		if (values.Length < 2)
			return false;

		var atrValue = values[0];
		if (!atrValue.IsFinal)
			return false;

		var atr = atrValue.GetValue<decimal>();

		var stochValue = (StochasticOscillatorValue)values[1];
		if (!stochValue.IsFinal)
			return false;

		if (stochValue.K is not decimal stochK)
			return false;

		var jmaValue = _jmaClose.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		if (!jmaValue.IsFinal)
			return false;

		var jmaClose = jmaValue.GetValue<decimal>();

		data = new IndicatorData(atr, stochK, jmaClose);
		return true;
	}

	private void UpdateSignalState(IndicatorData data)
	{
		if (_prevJmaClose is null)
		{
			_prevJmaClose = data.JmaClose;
			_prevBuySignal = false;
			_prevSellSignal = false;
			return;
		}

		if (_prevPrevJmaClose is null)
		{
			_prevPrevJmaClose = _prevJmaClose;
			_prevJmaClose = data.JmaClose;
			_prevBuySignal = false;
			_prevSellSignal = false;
			return;
		}

		var val3 = Math.Abs(data.JmaClose - _prevPrevJmaClose.Value);
		var range = data.Atr / AtrRangeDivisor;

		if (data.Stochastic < LowerThreshold && val3 > range)
			_patternState = 1;
		else if (data.Stochastic > UpperThreshold && val3 > range)
			_patternState = 2;

		if (val3 <= range)
		{
			_prevBuySignal = false;
			_prevSellSignal = false;
			_prevPrevJmaClose = _prevJmaClose;
			_prevJmaClose = data.JmaClose;
			return;
		}

		var newTrend = _trendDirection;
		var currentBuySignal = false;
		var currentSellSignal = false;

		if (data.Stochastic < LowerThreshold && (_patternState == 1 || _patternState == 0))
		{
			if (_trendDirection > 0)
				currentSellSignal = true;

			newTrend = -1;
		}
		else if (data.Stochastic > UpperThreshold && (_patternState == 2 || _patternState == 0))
		{
			if (_trendDirection < 0)
				currentBuySignal = true;

			newTrend = 1;
		}

		_trendDirection = newTrend;
		_prevBuySignal = currentBuySignal;
		_prevSellSignal = currentSellSignal;

		_prevPrevJmaClose = _prevJmaClose;
		_prevJmaClose = data.JmaClose;
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				LogInfo($"Stop loss hit at {stop}");
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				LogInfo($"Take profit hit at {take}");
				ResetPositionState();
				return true;
			}

			UpdateTrailingStopForLong(candle);
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				LogInfo($"Stop loss hit at {stop}");
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volume);
				LogInfo($"Take profit hit at {take}");
				ResetPositionState();
				return true;
			}

			UpdateTrailingStopForShort(candle);
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private void UpdateTrailingStopForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return;

		if (_entryPrice is not decimal entry)
			return;

		var step = GetPriceStep();
		var trailingDistance = step * TrailingStopPips;
		var trailingStep = step * TrailingStepPips;

		if (trailingDistance <= 0)
			return;

		if (candle.ClosePrice - entry < trailingDistance + trailingStep)
			return;

		var candidate = candle.ClosePrice - trailingDistance;

		if (_stopPrice is not decimal existing || candidate > existing)
		{
			_stopPrice = candidate;
			LogInfo($"Trailing stop moved to {candidate}");
		}
	}

	private void UpdateTrailingStopForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return;

		if (_entryPrice is not decimal entry)
			return;

		var step = GetPriceStep();
		var trailingDistance = step * TrailingStopPips;
		var trailingStep = step * TrailingStepPips;

		if (trailingDistance <= 0)
			return;

		if (entry - candle.ClosePrice < trailingDistance + trailingStep)
			return;

		var candidate = candle.ClosePrice + trailingDistance;

		if (_stopPrice is not decimal existing || candidate < existing)
		{
			_stopPrice = candidate;
			LogInfo($"Trailing stop moved to {candidate}");
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var step = GetPriceStep();
		var volume = Volume + Math.Abs(Position);
		var stop = StopLossPips > 0 ? candle.ClosePrice - step * StopLossPips : (decimal?)null;
		var take = TakeProfitPips > 0 ? candle.ClosePrice + step * TakeProfitPips : (decimal?)null;

		if (stop is decimal stopPrice && stopPrice >= candle.ClosePrice)
			return;

		if (take is decimal takePrice && takePrice <= candle.ClosePrice)
			take = null;

		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = stop;
		_takeProfitPrice = take;

		LogInfo($"Opened long at {_entryPrice} (stop={_stopPrice}, take={_takeProfitPrice})");
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var step = GetPriceStep();
		var volume = Volume + Math.Abs(Position);
		var stop = StopLossPips > 0 ? candle.ClosePrice + step * StopLossPips : (decimal?)null;
		var take = TakeProfitPips > 0 ? candle.ClosePrice - step * TakeProfitPips : (decimal?)null;

		if (stop is decimal stopPrice && stopPrice <= candle.ClosePrice)
			return;

		if (take is decimal takePrice && takePrice >= candle.ClosePrice)
			take = null;

		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = stop;
		_takeProfitPrice = take;

		LogInfo($"Opened short at {_entryPrice} (stop={_stopPrice}, take={_takeProfitPrice})");
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.MinPriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private readonly struct IndicatorData
	{
		public IndicatorData(decimal atr, decimal stochastic, decimal jmaClose)
		{
			Atr = atr;
			Stochastic = stochastic;
			JmaClose = jmaClose;
		}

		public decimal Atr { get; }
		public decimal Stochastic { get; }
		public decimal JmaClose { get; }
	}
}
