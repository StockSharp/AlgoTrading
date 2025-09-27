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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy converted from the MetaTrader "Trend Line" expert.
/// The algorithm relies on weighted moving averages, momentum spikes and MACD confirmation.
/// </summary>
public class TrendLineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _trailingTriggerSteps;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _momentum1;
	private decimal? _momentum2;
	private decimal? _momentum3;
	private decimal? _macdLine;
	private decimal? _macdSignal;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendLineStrategy"/> class.
	/// </summary>
	public TrendLineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Primary Candles", "Candle type used to calculate every indicator.", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetNotNegative()
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average.", "Indicators")
		.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetNotNegative()
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average.", "Indicators")
		.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetNotNegative()
		.SetDisplay("Momentum Period", "Number of candles used in the momentum calculation.", "Indicators")
		.SetCanOptimize(true);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetDisplay("Long Momentum", "Minimum momentum required before entering long positions.", "Filters")
		.SetCanOptimize(true);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), -0.3m)
		.SetDisplay("Short Momentum", "Maximum momentum allowed before opening short positions.", "Filters")
		.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetNotNegative()
		.SetDisplay("MACD Fast", "Fast EMA length of the MACD indicator.", "Indicators")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetNotNegative()
		.SetDisplay("MACD Slow", "Slow EMA length of the MACD indicator.", "Indicators")
		.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetNotNegative()
		.SetDisplay("MACD Signal", "Signal EMA length of the MACD indicator.", "Indicators")
		.SetCanOptimize(true);

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Protective stop distance expressed in price steps.", "Risk")
		.SetCanOptimize(true);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Protective take profit distance expressed in price steps.", "Risk")
		.SetCanOptimize(true);

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 40m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing stop distance expressed in price steps.", "Risk")
		.SetCanOptimize(true);

		_trailingTriggerSteps = Param(nameof(TrailingTriggerSteps), 40m)
		.SetNotNegative()
		.SetDisplay("Trailing Trigger", "Profit in steps required before the trailing stop activates.", "Risk")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used to calculate the indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast LWMA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow LWMA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum required to allow long positions.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Maximum (negative) momentum tolerated before opening short positions.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length of the MACD indicator.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length of the MACD indicator.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length of the MACD indicator.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in price steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Profit in price steps required before the trailing stop activates.
	/// </summary>
	public decimal TrailingTriggerSteps
	{
		get => _trailingTriggerSteps.Value;
		set => _trailingTriggerSteps.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
		yield break;

		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentum1 = null;
		_momentum2 = null;
		_momentum3 = null;
		_macdLine = null;
		_macdSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortMa = { Length = MacdFastLength },
			LongMa = { Length = MacdSlowLength },
			SignalMa = { Length = MacdSignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, _momentum, ProcessMainCandle);
		subscription.BindEx(_macd, ProcessMacd);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal)
		return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
		return;

		_macdLine = macd;
		_macdSignal = signal;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateMomentumHistory(momentumValue);
		UpdateTrailingStop(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentum.IsFormed)
		return;

		if (_macdLine is not decimal macd || _macdSignal is not decimal signal)
		return;

		TryEnterPosition(candle.ClosePrice, fastValue, slowValue, macd, signal);
	}

	private void UpdateMomentumHistory(decimal momentumValue)
	{
		_momentum3 = _momentum2;
		_momentum2 = _momentum1;
		_momentum1 = momentumValue;
	}

	private void UpdateTrailingStop(decimal closePrice)
	{
		if (Position == 0 || TrailingStopSteps <= 0m || TrailingTriggerSteps <= 0m)
		return;

		if (Security?.PriceStep is not decimal step || step <= 0m)
		return;

		if (PositionPrice is not decimal entryPrice)
		return;

		var triggerDistance = TrailingTriggerSteps * step;

		if (Position > 0)
		{
			if (closePrice - entryPrice >= triggerDistance)
			SetStopLoss(TrailingStopSteps, closePrice, Position);
		}
		else if (entryPrice - closePrice >= triggerDistance)
		{
			SetStopLoss(TrailingStopSteps, closePrice, Position);
		}
	}

	private void TryEnterPosition(decimal closePrice, decimal fastValue, decimal slowValue, decimal macd, decimal signal)
	{
		var longMomentum = HasMomentumForLong();
		var shortMomentum = HasMomentumForShort();

		if (fastValue > slowValue && macd > signal && longMomentum && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				BuyMarket(volume);
				ApplyProtection(closePrice, Position + volume);
			}
		}
		else if (fastValue < slowValue && macd < signal && shortMomentum && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				SellMarket(volume);
				ApplyProtection(closePrice, Position - volume);
			}
		}
	}

	private bool HasMomentumForLong()
	{
		var threshold = MomentumBuyThreshold;

		if (_momentum1 is decimal m1 && m1 >= threshold)
		return true;

		if (_momentum2 is decimal m2 && m2 >= threshold)
		return true;

		if (_momentum3 is decimal m3 && m3 >= threshold)
		return true;

		return false;
	}

	private bool HasMomentumForShort()
	{
		var threshold = MomentumSellThreshold;

		if (_momentum1 is decimal m1 && m1 <= threshold)
		return true;

		if (_momentum2 is decimal m2 && m2 <= threshold)
		return true;

		if (_momentum3 is decimal m3 && m3 <= threshold)
		return true;

		return false;
	}

	private void ApplyProtection(decimal referencePrice, decimal resultingPosition)
	{
		if (resultingPosition == 0)
		return;

		if (TakeProfitSteps > 0m)
		SetTakeProfit(TakeProfitSteps, referencePrice, resultingPosition);

		if (StopLossSteps > 0m)
		SetStopLoss(StopLossSteps, referencePrice, resultingPosition);
	}
}

