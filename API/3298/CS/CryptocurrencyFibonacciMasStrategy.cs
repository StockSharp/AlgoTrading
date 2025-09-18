using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Cryptocurrency Fibonacci MAs" MetaTrader expert advisor.
/// Combines a Fibonacci EMA stack with higher timeframe momentum and a monthly MACD filter.
/// Opens long positions when all EMAs are aligned upward, momentum shows strength and MACD is bullish.
/// Opens short positions in the mirrored situation.
/// Risk management relies on classic stop-loss/take-profit expressed in pips.
/// </summary>
public class CryptocurrencyFibonacciMasStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema8;
	private ExponentialMovingAverage _ema13;
	private ExponentialMovingAverage _ema21;
	private ExponentialMovingAverage _ema55;
	private Momentum _momentum;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private readonly Queue<decimal> _momentumAbsValues = new();
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private decimal _pipSize;
	private decimal _priceStep;

	/// <summary>
	/// Initializes a new instance of the <see cref="CryptocurrencyFibonacciMasStrategy"/> class.
	/// </summary>
	public CryptocurrencyFibonacciMasStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Order volume in lots", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 60m, 5m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 120m, 10m);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Momentum Buy Threshold", "Minimal absolute momentum distance from 100 for buys", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Momentum Sell Threshold", "Minimal absolute momentum distance from 100 for sells", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum number of stacked positions per direction", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for EMA calculations", "General");
	}

	/// <summary>
	/// Order volume used for entries and exits.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Required absolute momentum deviation from the neutral 100 level to allow long entries.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Required absolute momentum deviation from the neutral 100 level to allow short entries.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of positions allowed in the same direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Primary candle data type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the momentum confirmation.
	/// </summary>
	public DataType MomentumCandleType => MapMomentumTimeFrame(CandleType);

	/// <summary>
	/// Monthly timeframe used by the MACD confirmation filter.
	/// </summary>
	public DataType MacdCandleType => TimeSpan.FromDays(30).TimeFrame();

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		yield return (Security, CandleType);

		var momentumType = MomentumCandleType;
		if (!Equals(momentumType, CandleType))
		yield return (Security, momentumType);

		var macdType = MacdCandleType;
		if (!Equals(macdType, CandleType) && !Equals(macdType, momentumType))
		yield return (Security, macdType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentumAbsValues.Clear();
		_macdMain = null;
		_macdSignal = null;
		_pipSize = 0m;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema8 = new ExponentialMovingAverage { Length = 8 };
		_ema13 = new ExponentialMovingAverage { Length = 13 };
		_ema21 = new ExponentialMovingAverage { Length = 21 };
		_ema55 = new ExponentialMovingAverage { Length = 55 };
		_momentum = new Momentum { Length = 14 };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
		Macd =
		{
		ShortMa = { Length = 12 },
		LongMa = { Length = 26 }
		},
		SignalMa = { Length = 9 }
		};

		_pipSize = GetPipSize();
		_priceStep = Security?.PriceStep ?? 0m;

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(_ema8, _ema13, _ema21, _ema55, ProcessMainCandle)
		.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
		.Bind(_momentum, ProcessMomentum)
		.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
		.BindEx(_macd, ProcessMacd)
		.Start();

		StartProtection(
		takeProfit: TakeProfitPips > 0m ? new Unit(PipsToSteps(TakeProfitPips), UnitTypes.Step) : null,
		stopLoss: StopLossPips > 0m ? new Unit(PipsToSteps(StopLossPips), UnitTypes.Step) : null);
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_momentum.IsFormed)
		return;

		var distance = Math.Abs(momentum - 100m);
		_momentumAbsValues.Enqueue(distance);

		while (_momentumAbsValues.Count > 3)
		{
		_momentumAbsValues.Dequeue();
		}
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!value.IsFinal)
		return;

		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)value;

		if (macdValue.Macd is not decimal macd || macdValue.Signal is not decimal signal)
		return;

		_macdMain = macd;
		_macdSignal = signal;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal ema8, decimal ema13, decimal ema21, decimal ema55)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_ema55.IsFormed || _momentumAbsValues.Count < 3 || _macdMain is null || _macdSignal is null)
		return;

		var momentumForBuy = _momentumAbsValues.Any(v => v >= MomentumBuyThreshold);
		var momentumForSell = _momentumAbsValues.Any(v => v >= MomentumSellThreshold);

		var macdMain = _macdMain.Value;
		var macdSignal = _macdSignal.Value;

		if (Position <= 0 && CanOpenPosition(Sides.Buy) && momentumForBuy && ema8 > ema13 && ema13 > ema21 && ema21 > ema55 && macdMain > macdSignal)
		{
		var volume = CalculateVolumeForEntry(Sides.Buy);
		if (volume > 0)
		{
		BuyMarket(volume);
		LogInfo($"Long entry at {candle.ClosePrice:F5}. EMA8={ema8:F5}, EMA13={ema13:F5}, EMA21={ema21:F5}, EMA55={ema55:F5}, MomentumΔ={_momentumAbsValues.Last():F3}, MACD={macdMain:F4}>{macdSignal:F4}");
		}
		}
		else if (Position >= 0 && CanOpenPosition(Sides.Sell) && momentumForSell && ema8 < ema13 && ema13 < ema21 && ema21 < ema55 && macdMain < macdSignal)
		{
		var volume = CalculateVolumeForEntry(Sides.Sell);
		if (volume > 0)
		{
		SellMarket(volume);
		LogInfo($"Short entry at {candle.ClosePrice:F5}. EMA8={ema8:F5}, EMA13={ema13:F5}, EMA21={ema21:F5}, EMA55={ema55:F5}, MomentumΔ={_momentumAbsValues.Last():F3}, MACD={macdMain:F4}<{macdSignal:F4}");
		}
		}
	}

	private bool CanOpenPosition(Sides side)
	{
		if (MaxPositions <= 0)
		return false;

		var baseVolume = TradeVolume > 0 ? TradeVolume : 1m;
		if (baseVolume <= 0)
		return false;

		var maxVolume = MaxPositions * baseVolume;

		if (side == Sides.Buy)
		return Position <= 0 && Math.Abs(Position) < maxVolume;

		return Position >= 0 && Math.Abs(Position) < maxVolume;
	}

	private decimal CalculateVolumeForEntry(Sides side)
	{
		var baseVolume = TradeVolume > 0 ? TradeVolume : 1m;

		if (side == Sides.Buy && Position < 0)
		baseVolume += Math.Abs(Position);
		else if (side == Sides.Sell && Position > 0)
		baseVolume += Math.Abs(Position);

		return baseVolume;
	}

	private DataType MapMomentumTimeFrame(DataType source)
	{
		var span = source.Arg as TimeSpan?;
		if (span is null)
		return source;

		TimeSpan target = span.Value;
		var minutes = span.Value.TotalMinutes;

		if (minutes <= 1)
		{
		target = TimeSpan.FromMinutes(15);
		}
		else if (Math.Abs(minutes - 5) < 0.001)
		{
		target = TimeSpan.FromMinutes(30);
		}
		else if (Math.Abs(minutes - 15) < 0.001)
		{
		target = TimeSpan.FromHours(1);
		}
		else if (Math.Abs(minutes - 30) < 0.001)
		{
		target = TimeSpan.FromHours(4);
		}
		else if (Math.Abs(minutes - 60) < 0.001)
		{
		target = TimeSpan.FromDays(1);
		}
		else if (Math.Abs(minutes - 240) < 0.001)
		{
		target = TimeSpan.FromDays(7);
		}
		else if (Math.Abs(minutes - 1440) < 0.001)
		{
		target = TimeSpan.FromDays(30);
		}
		else if (Math.Abs(minutes - 10080) < 0.001)
		{
		target = TimeSpan.FromDays(30);
		}
		else if (Math.Abs(minutes - 43200) < 0.001)
		{
		target = TimeSpan.FromDays(30);
		}

		return target.TimeFrame();
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		return step;
	}

	private decimal PipsToSteps(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		if (_pipSize <= 0m || _priceStep <= 0m)
		return pips;

		var priceOffset = pips * _pipSize;
		return priceOffset / _priceStep;
	}
}
