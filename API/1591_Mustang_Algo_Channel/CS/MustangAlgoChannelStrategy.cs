using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI smoothed with WMA to generate global sentiment signals.
/// </summary>
public class MustangAlgoChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<int> _medianPeriod;
	private readonly StrategyParam<decimal> _upperBound;
	private readonly StrategyParam<decimal> _lowerBound;
	private readonly StrategyParam<string> _tradeMode;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private WeightedMovingAverage _smooth = null!;
	private WeightedMovingAverage _median = null!;

	private decimal? _prevOscillator;
	private decimal? _prevMedian;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// WMA smoothing period.
	/// </summary>
	public int Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}

	/// <summary>
	/// Median WMA period.
	/// </summary>
	public int MedianPeriod
	{
		get => _medianPeriod.Value;
		set => _medianPeriod.Value = value;
	}

	/// <summary>
	/// Upper bound for short signals.
	/// </summary>
	public decimal UpperBound
	{
		get => _upperBound.Value;
		set => _upperBound.Value = value;
	}

	/// <summary>
	/// Lower bound for long signals.
	/// </summary>
	public decimal LowerBound
	{
		get => _lowerBound.Value;
		set => _lowerBound.Value = value;
	}

	/// <summary>
	/// Trading direction mode.
	/// </summary>
	public string TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public MustangAlgoChannelStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Period for RSI calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_smoothing = Param(nameof(Smoothing), 20)
			.SetDisplay("WMA Smoothing", "Smoothing period for sentiment oscillator", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);

		_medianPeriod = Param(nameof(MedianPeriod), 25)
			.SetDisplay("Moving Median", "Period for crossover reference line", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);

		_upperBound = Param(nameof(UpperBound), 55m)
			.SetDisplay("Upper Bound", "Overbought threshold for short signals", "Signals");

		_lowerBound = Param(nameof(LowerBound), 48m)
			.SetDisplay("Lower Bound", "Oversold threshold for long signals", "Signals");

		_tradeMode = Param(nameof(TradeMode), "Long & Short")
			.SetDisplay("Trade Mode", "Trading direction", "General")
			.SetOptions("Long & Short", "Long Only", "Short Only");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Enable Stop Loss", "Use stop loss", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Enable Take Profit", "Use take profit", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 4m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 12m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevOscillator = default;
		_prevMedian = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_smooth = new WeightedMovingAverage { Length = Smoothing };
		_median = new WeightedMovingAverage { Length = MedianPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_rsi, ProcessCandle)
			.Start();

		StartProtection(
			UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit());
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var smoothValue = _smooth.Process(rsiValue);
		if (!smoothValue.IsFinal)
			return;

		var medianValue = _median.Process(smoothValue);
		if (!medianValue.IsFinal)
			return;

		var osc = smoothValue.GetValue<decimal>();
		var med = medianValue.GetValue<decimal>();

		if (_prevOscillator == null || _prevMedian == null)
		{
			_prevOscillator = osc;
			_prevMedian = med;
			return;
		}

		var crossUp = _prevOscillator < _prevMedian && osc > med;
		var crossDown = _prevOscillator > _prevMedian && osc < med;

		var longEntry = crossUp && osc < LowerBound;
		var longExit = crossDown && osc > UpperBound;
		var shortEntry = crossDown && osc > UpperBound;
		var shortExit = crossUp && osc < LowerBound;

		var allowLong = TradeMode == "Long & Short" || TradeMode == "Long Only";
		var allowShort = TradeMode == "Long & Short" || TradeMode == "Short Only";

		if (allowLong)
		{
			if (longEntry && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (longExit && Position > 0)
			{
				SellMarket(Position);
			}
		}

		if (allowShort)
		{
			if (shortEntry && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (shortExit && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		_prevOscillator = osc;
		_prevMedian = med;
	}
}

