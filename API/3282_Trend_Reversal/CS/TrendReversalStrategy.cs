using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that looks for momentum supported reversals when a fast LWMA crosses above or below a slow LWMA.
/// The logic was ported from the Trend Reversal MetaTrader expert and simplified for the StockSharp high-level API.
/// </summary>
public class TrendReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maxPositions;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<decimal> _momentumDeviations = new(3);
	private decimal? _fastValue;
	private decimal? _slowValue;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private DateTimeOffset? _lastSignalTime;
	private ICandleMessage _previousCandle;
	private ICandleMessage _previousPreviousCandle;

	/// <summary>
	/// Initializes parameters of the strategy.
	/// </summary>
	public TrendReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for signal calculation", "General");

		_fastLength = Param(nameof(FastLength), 6)
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_slowLength = Param(nameof(SlowLength), 85)
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(50, 120, 5);

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetDisplay("Momentum Length", "Period of the momentum oscillator", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetDisplay("Buy Momentum Threshold", "Minimum deviation of momentum from 100 for long setups", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetDisplay("Sell Momentum Threshold", "Minimum deviation of momentum from 100 for short setups", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast", "Fast EMA period for the MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 1);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow", "Slow EMA period for the MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 32, 1);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal", "Signal EMA period for the MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 12, 1);

		_takeProfit = Param(nameof(TakeProfit), 50m)
			.SetDisplay("Take Profit", "Distance of the take profit in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_stopLoss = Param(nameof(StopLoss), 20m)
			.SetDisplay("Stop Loss", "Distance of the stop loss in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 80m, 5m);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order volume expressed in lots", "Execution");

		_maxPositions = Param(nameof(MaxPositions), 1)
			.SetDisplay("Max Positions", "Maximum number of net position units to hold", "Execution")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Trading timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Momentum oscillator period.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation required for long setups.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation required for short setups.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Volume of each trade expressed in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of net position units (multiples of trade volume) allowed at once.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
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
		_momentumDeviations.Clear();
		_fastValue = null;
		_slowValue = null;
		_macdMain = null;
		_macdSignal = null;
		_lastSignalTime = null;
		_previousCandle = null;
		_previousPreviousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_fastMa = new LinearWeightedMovingAverage
		{
			Length = FastLength,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new LinearWeightedMovingAverage
		{
			Length = SlowLength,
			CandlePrice = CandlePrice.Typical
		};

		_momentum = new Momentum
		{
			Length = MomentumLength,
			CandlePrice = CandlePrice.Typical
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength, CandlePrice = CandlePrice.Typical },
				LongMa = { Length = MacdSlowLength, CandlePrice = CandlePrice.Typical }
			},
			SignalMa = { Length = MacdSignalLength, CandlePrice = CandlePrice.Typical }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _momentum, ProcessTrendIndicators)
			.BindEx(_macd, ProcessMacd)
			.Start();

		if (TakeProfit > 0m || StopLoss > 0m)
		{
			var takeProfit = TakeProfit > 0m ? new Unit(TakeProfit, UnitTypes.Price) : null;
			var stopLoss = StopLoss > 0m ? new Unit(StopLoss, UnitTypes.Price) : null;
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrendIndicators(ICandleMessage candle, decimal fast, decimal slow, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentum.IsFormed)
			return;

		_fastValue = fast;
		_slowValue = slow;

		var deviation = Math.Abs(100m - momentumValue);
		UpdateMomentum(deviation);

		TryGenerateSignal(candle);
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

	if (indicatorValue is not MovingAverageConvergenceDivergenceSignalValue macdValue)
			return;

	if (macdValue.Macd is not decimal macdMain || macdValue.Signal is not decimal macdSignal)
			return;

		_macdMain = macdMain;
		_macdSignal = macdSignal;

		TryGenerateSignal(candle);
	}

	private void TryGenerateSignal(ICandleMessage candle)
	{
		if (_lastSignalTime == candle.OpenTime)
			return;

		if (_fastValue is not decimal fast || _slowValue is not decimal slow)
			return;

		if (_macdMain is not decimal macdMain || _macdSignal is not decimal macdSignal)
			return;

		if (_previousCandle is null || _previousPreviousCandle is null)
		{
			UpdateHistoricalCandles(candle);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_lastSignalTime = candle.OpenTime;

		var momentumSupportsLong = HasMomentumAbove(MomentumBuyThreshold);
		var momentumSupportsShort = HasMomentumAbove(MomentumSellThreshold);
		var priceOverlap = _previousPreviousCandle.LowPrice < _previousCandle.HighPrice;

		if (priceOverlap)
		{
			var maxExposure = TradeVolume * MaxPositions;
			if (maxExposure > 0m)
			{
				if (momentumSupportsLong && fast > slow && macdMain > macdSignal && Position < maxExposure)
				{
					// Enter or increase a long position when bullish conditions align.
					BuyMarket();
				}
				else if (momentumSupportsShort && fast < slow && macdMain < macdSignal && Position > -maxExposure)
				{
					// Enter or increase a short position when bearish conditions align.
					SellMarket();
				}
			}
		}

		UpdateHistoricalCandles(candle);
	}

	private bool HasMomentumAbove(decimal threshold)
	{
		foreach (var value in _momentumDeviations)
		{
			if (value >= threshold)
				return true;
		}

		return false;
	}

	private void UpdateMomentum(decimal deviation)
	{
		_momentumDeviations.Enqueue(deviation);
		while (_momentumDeviations.Count > 3)
			_momentumDeviations.Dequeue();
	}

	private void UpdateHistoricalCandles(ICandleMessage candle)
	{
		_previousPreviousCandle = _previousCandle;
		_previousCandle = candle;
	}
}
