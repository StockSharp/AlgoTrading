using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// High-level port of the "Eliot Wave I" MQL4 expert advisor.
/// Combines LWMA trend detection, multi-timeframe momentum confirmation,
/// and a monthly MACD filter to decide entries.
/// </summary>
public class EliotWaveStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _maxPosition;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<(decimal high, decimal low)> _recentCandles = new();
	private readonly Queue<decimal> _momentumDeviations = new();

	private decimal _fastValue;
	private decimal _slowValue;
	private decimal _macdMain;
	private decimal _macdSignal;
	private bool _hasMomentum;
	private bool _hasMacd;
	private decimal _entryPrice;

	/// <summary>
	/// Base timeframe candle type for the primary logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the momentum confirmation.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to evaluate the MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Fast LWMA length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow LWMA length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum lookback length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation above 100 required for bullish momentum.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum deviation above 100 required for bearish momentum.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trade size for a single entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum absolute net position allowed by the strategy.
	/// </summary>
	public decimal MaxPosition
	{
		get => _maxPosition.Value;
		set => _maxPosition.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults taken from the MQL expert.
	/// </summary>
	public EliotWaveStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Base Candle", "Primary timeframe used for LWMA logic", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum Candle", "Confirmation timeframe for the momentum filter", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candle", "Slow timeframe used for the MACD trend filter", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(40, 120, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Length of the momentum indicator on the higher timeframe", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Momentum Buy Threshold", "Deviation from 100 required to confirm a bullish setup", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.5m, 0.1m);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Momentum Sell Threshold", "Deviation from 100 required to confirm a bearish setup", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.5m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Stop Loss (pts)", "Protective stop-loss distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Take Profit (pts)", "Target distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 5m);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume used for each market order", "Risk");

		_maxPosition = Param(nameof(MaxPosition), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Max Position", "Maximum net position allowed (in lots)", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var types = new HashSet<DataType> { CandleType, MomentumCandleType, MacdCandleType };
		foreach (var type in types)
			yield return (Security, type);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_recentCandles.Clear();
		_momentumDeviations.Clear();
		_fastValue = 0m;
		_slowValue = 0m;
		_macdMain = 0m;
		_macdSignal = 0m;
		_hasMomentum = false;
		_hasMacd = false;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaLength };
		_slowMa = new WeightedMovingAverage { Length = SlowMaLength };
		_momentum = new Momentum { Length = MomentumPeriod };
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
			.Bind(ProcessMainCandle)
			.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
			.Bind(_momentum, ProcessMomentum)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.BindEx(_macd, ProcessMacd)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection(
			stopLoss: new Unit(StopLossPips, UnitTypes.Point),
			takeProfit: new Unit(TakeProfitPips, UnitTypes.Point)
		);

		Volume = TradeVolume;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		var isFinished = candle.State == CandleStates.Finished;
		var typical = GetTypicalPrice(candle);

		_fastValue = _fastMa.Process(typical, candle.OpenTime, isFinished).ToDecimal();
		_slowValue = _slowMa.Process(typical, candle.OpenTime, isFinished).ToDecimal();

		if (!isFinished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		UpdateHistory(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageOpenPosition(candle);

		if (!_hasMomentum || !_hasMacd)
			return;

		if (_recentCandles.Count < 3)
			return;

		TryEnter(candle);
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var deviation = Math.Abs(momentum - 100m);
		_momentumDeviations.Enqueue(deviation);
		if (_momentumDeviations.Count > 3)
			_momentumDeviations.Dequeue();

		_hasMomentum = _momentum.IsFormed && _momentumDeviations.Count == 3;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)value;
		if (macdValue.Macd is not decimal macd || macdValue.Signal is not decimal signal)
			return;

		_macdMain = macd;
		_macdSignal = signal;
		_hasMacd = true;
	}

	private void TryEnter(ICandleMessage candle)
	{
		var candles = _recentCandles.ToArray();
		var twoAgo = candles[^3];
		var oneAgo = candles[^2];

		var bullishMomentum = _momentumDeviations.Any(v => v >= MomentumBuyThreshold);
		var bearishMomentum = _momentumDeviations.Any(v => v >= MomentumSellThreshold);

		var canOpenLong = Position < MaxPosition;
		var canOpenShort = Position > -MaxPosition;

		if (bullishMomentum && _macdMain > _macdSignal && _fastValue > _slowValue && twoAgo.low < oneAgo.high && canOpenLong && Position <= 0m)
		{
			var volume = TradeVolume + Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
			return;
		}

		if (bearishMomentum && _macdMain < _macdSignal && _fastValue < _slowValue && oneAgo.low < twoAgo.high && canOpenShort && Position >= 0m)
		{
			var volume = TradeVolume + Math.Abs(Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_fastValue < _slowValue || _macdMain <= _macdSignal)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0m)
		{
			if (_fastValue > _slowValue || _macdMain >= _macdSignal)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_recentCandles.Enqueue((candle.HighPrice, candle.LowPrice));
		if (_recentCandles.Count > 3)
			_recentCandles.Dequeue();
	}

	private static decimal GetTypicalPrice(ICandleMessage candle)
	{
		return (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
	}
}
