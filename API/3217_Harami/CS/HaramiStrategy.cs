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
/// Harami pattern strategy with momentum and MACD filters across multiple timeframes.
/// </summary>
public class HaramiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private LinearWeightedMovingAverage _fastMa = null!;
	private ExponentialMovingAverage _slowMa = null!;
	private Momentum _higherMomentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private (decimal Open, decimal Close)? _higherPrevious;
	private (decimal Open, decimal Close)? _higherPrevious2;

	private decimal? _momentumRecent;
	private decimal? _momentumPrevious;
	private decimal? _momentumOld;

	private decimal? _macdValue;
	private decimal? _macdSignal;

	/// <summary>
	/// Initializes a new instance of the <see cref="HaramiStrategy"/> class.
	/// </summary>
	public HaramiStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Base TF", "Primary timeframe for entries", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Higher TF", "Higher timeframe for Harami and momentum", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD TF", "Timeframe for MACD filter", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted MA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow exponential MA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(30, 120, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum period on the higher timeframe", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Bull Threshold", "Minimum momentum deviation for longs", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Bear Threshold", "Minimum momentum deviation for shorts", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Protective stop in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target in points", "Risk");
	}

	/// <summary>
	/// Primary candle type used for entry calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for Harami and momentum confirmation.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to calculate the MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow exponential moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum lookback on the higher timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation required to confirm long entries.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation required to confirm short entries.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss size expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit size expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_higherPrevious = null;
		_higherPrevious2 = null;
		_momentumRecent = null;
		_momentumPrevious = null;
		_momentumOld = null;
		_macdValue = null;
		_macdSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaLength };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaLength };
		_higherMomentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_fastMa, _slowMa, ProcessMainCandle)
			.Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription
			.Bind(_higherMomentum, ProcessHigherCandle)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.BindEx(_macd, ProcessMacdCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawOwnTrades(area);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
		}

		Unit takeProfit = null;
		if (TakeProfitPoints > 0m)
			takeProfit = new Unit(TakeProfitPoints, UnitTypes.Point);

		Unit stopLoss = null;
		if (StopLossPoints > 0m)
			stopLoss = new Unit(StopLossPoints, UnitTypes.Point);

		StartProtection(takeProfit: takeProfit, stopLoss: stopLoss, useMarketOrders: true);
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_higherPrevious is null || _higherPrevious2 is null)
			return;

		if (_momentumRecent is null || _momentumPrevious is null || _momentumOld is null)
			return;

		if (_macdValue is null || _macdSignal is null)
			return;

		var higherPrev = _higherPrevious.Value;
		var higherPrev2 = _higherPrevious2.Value;

		var bodyPrev = higherPrev.Open - higherPrev.Close;
		var bodyPrev2 = higherPrev2.Open - higherPrev2.Close;

		var bullishHarami = bodyPrev2 > 0m && bodyPrev < 0m && Math.Abs(bodyPrev) < Math.Abs(bodyPrev2);
		var bearishHarami = bodyPrev2 < 0m && bodyPrev > 0m && Math.Abs(bodyPrev) < Math.Abs(bodyPrev2);

		var bullishMomentum = _momentumRecent.Value > MomentumBuyThreshold ||
			_momentumPrevious.Value > MomentumBuyThreshold ||
			_momentumOld.Value > MomentumBuyThreshold;

		var bearishMomentum = _momentumRecent.Value > MomentumSellThreshold ||
			_momentumPrevious.Value > MomentumSellThreshold ||
			_momentumOld.Value > MomentumSellThreshold;

		var macdAbove = _macdValue.Value > _macdSignal.Value;
		var macdBelow = _macdValue.Value < _macdSignal.Value;

		var longTrend = slowMa > fastMa;
		var shortTrend = slowMa < fastMa;

		if (bullishHarami && bullishMomentum && macdAbove && longTrend && Position <= 0m)
		{
			BuyMarket(GetVolumeToOpenLong());
		}
		else if (bearishHarami && bearishMomentum && macdBelow && shortTrend && Position >= 0m)
		{
			SellMarket(GetVolumeToOpenShort());
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherPrevious2 = _higherPrevious;
		_higherPrevious = (candle.OpenPrice, candle.ClosePrice);

		if (!_higherMomentum.IsFormed)
			return;

		var deviation = Math.Abs(momentumValue - 100m);

		_momentumOld = _momentumPrevious;
		_momentumPrevious = _momentumRecent;
		_momentumRecent = deviation;
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		_macdValue = macdLine;
		_macdSignal = signalLine;
	}

	private decimal GetVolumeToOpenLong()
	{
		var current = Position;
		if (current < 0m)
			return Volume + Math.Abs(current);

		return Volume;
	}

	private decimal GetVolumeToOpenShort()
	{
		var current = Position;
		if (current > 0m)
			return Volume + current;

		return Volume;
	}
}

