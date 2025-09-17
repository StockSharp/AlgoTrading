namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Intraday trend strategy converted from the MetaTrader "DayTrading" expert advisor.
/// Combines Parabolic SAR, MACD, Stochastic and Momentum filters with trailing exits.
/// </summary>
public class DayTradingStrategy : Strategy
{
	private const decimal MomentumNeutralLevel = 100m;
	private const decimal StochasticBuyThreshold = 35m;
	private const decimal StochasticSellThreshold = 60m;

	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _slippagePoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticSignal;
	private readonly StrategyParam<int> _stochasticSlow;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _sarAcceleration;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;

	private ParabolicSar _parabolicSar = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private StochasticOscillator _stochastic = null!;
	private Momentum _momentum = null!;

	private decimal? _previousSar;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortTakeProfit;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _pointSize;

	/// <summary>
	/// Initializes a new instance of <see cref="DayTradingStrategy"/>.
	/// </summary>
	public DayTradingStrategy()
	{
		_lotSize = Param(nameof(LotSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 15m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (points)", "Distance used to trail profitable positions", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Fixed profit target measured in points", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Protective stop distance measured in points", "Risk")
			.SetCanOptimize(true);

		_slippagePoints = Param(nameof(SlippagePoints), 3m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Slippage (points)", "Maximum acceptable execution slippage", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for indicator calculations", "Data");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Length of the fast EMA in MACD", "Indicators")
			.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Length of the slow EMA in MACD", "Indicators")
			.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Length of the MACD signal EMA", "Indicators")
			.SetCanOptimize(true);

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Period of the %K line", "Indicators")
			.SetCanOptimize(true);

		_stochasticSignal = Param(nameof(StochasticSignal), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Period of the %D smoothing", "Indicators")
			.SetCanOptimize(true);

		_stochasticSlow = Param(nameof(StochasticSlow), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Slowing", "Final smoothing applied to %K", "Indicators")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Number of candles used for Momentum", "Indicators")
			.SetCanOptimize(true);

		_sarAcceleration = Param(nameof(SarAcceleration), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Acceleration", "Initial acceleration factor of Parabolic SAR", "Indicators")
			.SetCanOptimize(true);

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Increment applied to the acceleration factor", "Indicators")
			.SetCanOptimize(true);

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Maximum", "Maximum acceleration factor of Parabolic SAR", "Indicators")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Trade volume used for each market entry.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Distance used to trail profitable positions.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Fixed profit target measured in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Protective stop distance measured in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Maximum acceptable execution slippage.
	/// </summary>
	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Time frame used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the fast EMA in MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA in MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Length of the MACD signal EMA.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Period of the %K line.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Period of the %D smoothing.
	/// </summary>
	public int StochasticSignal
	{
		get => _stochasticSignal.Value;
		set => _stochasticSignal.Value = value;
	}

	/// <summary>
	/// Final smoothing applied to %K.
	/// </summary>
	public int StochasticSlow
	{
		get => _stochasticSlow.Value;
		set => _stochasticSlow.Value = value;
	}

	/// <summary>
	/// Number of candles used for Momentum.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Initial acceleration factor of Parabolic SAR.
	/// </summary>
	public decimal SarAcceleration
	{
		get => _sarAcceleration.Value;
		set => _sarAcceleration.Value = value;
	}

	/// <summary>
	/// Increment applied to the acceleration factor.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor of Parabolic SAR.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
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

		_previousSar = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = LotSize;
		_pointSize = CalculatePointSize();

		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarAcceleration,
			AccelerationStep = SarStep,
			AccelerationMax = SarMaximum,
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod },
			},
			SignalMa = { Length = MacdSignalPeriod },
		};

		_stochastic = new StochasticOscillator
		{
			KPeriod = StochasticLength,
			DPeriod = StochasticSignal,
			Slowing = StochasticSlow,
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_parabolicSar, _macd, _stochastic, _momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue sarValue,
		IIndicatorValue macdValue,
		IIndicatorValue stochasticValue,
		IIndicatorValue momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!sarValue.IsFinal || !macdValue.IsFinal || !stochasticValue.IsFinal || !momentumValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macd)
			return;

		if (stochasticValue is not StochasticOscillatorValue stochastic)
			return;

		var sar = sarValue.ToDecimal();
		var previousSar = _previousSar;
		_previousSar = sar;

		if (previousSar is null)
			return;

		var momentum = momentumValue.ToDecimal();
		var ask = GetAskPrice(candle);
		var bid = GetBidPrice(candle);

		var buySignal = sar <= ask && previousSar.Value > sar && momentum < MomentumNeutralLevel &&
			macd.Macd < macd.Signal && stochastic.K < StochasticBuyThreshold;
		var sellSignal = sar >= bid && previousSar.Value < sar && momentum > MomentumNeutralLevel &&
			macd.Macd > macd.Signal && stochastic.K > StochasticSellThreshold;

		var closedPosition = false;

		if (Position > 0)
		{
			if (sellSignal)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				closedPosition = true;
			}
			else if (HandleLongRisk(candle))
			{
				closedPosition = true;
			}
		}
		else if (Position < 0)
		{
			if (buySignal)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				closedPosition = true;
			}
			else if (HandleShortRisk(candle))
			{
				closedPosition = true;
			}
		}

		if (closedPosition)
			return;

		if (Position == 0)
		{
			if (buySignal)
			{
				var entryPrice = ask;
				BuyMarket(Volume);
				_longEntryPrice = entryPrice;
				_longStopPrice = StopLossPoints > 0m ? entryPrice - ConvertPoints(StopLossPoints) : null;
				_longTakeProfit = TakeProfitPoints > 0m ? entryPrice + ConvertPoints(TakeProfitPoints) : null;
			}
			else if (sellSignal)
			{
				var entryPrice = bid;
				SellMarket(Volume);
				_shortEntryPrice = entryPrice;
				_shortStopPrice = StopLossPoints > 0m ? entryPrice + ConvertPoints(StopLossPoints) : null;
				_shortTakeProfit = TakeProfitPoints > 0m ? entryPrice - ConvertPoints(TakeProfitPoints) : null;
			}
		}
	}

	private bool HandleLongRisk(ICandleMessage candle)
	{
		if (Math.Abs(Position) <= 0m)
			return false;

		if (_longTakeProfit is decimal takeProfit && candle.HighPrice >= takeProfit)
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
			return true;
		}

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
			return true;
		}

		var trailingDistance = ConvertPoints(TrailingStopPoints);
		if (trailingDistance > 0m && _longEntryPrice is decimal entry)
		{
			var progressed = candle.HighPrice - entry;
			if (progressed >= trailingDistance)
			{
				var candidate = candle.ClosePrice - trailingDistance;
				if (!_longStopPrice.HasValue || candidate > _longStopPrice.Value)
					_longStopPrice = candidate;
			}
		}

		return false;
	}

	private bool HandleShortRisk(ICandleMessage candle)
	{
		if (Math.Abs(Position) <= 0m)
			return false;

		if (_shortTakeProfit is decimal takeProfit && candle.LowPrice <= takeProfit)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return true;
		}

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return true;
		}

		var trailingDistance = ConvertPoints(TrailingStopPoints);
		if (trailingDistance > 0m && _shortEntryPrice is decimal entry)
		{
			var progressed = entry - candle.LowPrice;
			if (progressed >= trailingDistance)
			{
				var candidate = candle.ClosePrice + trailingDistance;
				if (!_shortStopPrice.HasValue || candidate < _shortStopPrice.Value)
					_shortStopPrice = candidate;
			}
		}

		return false;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfit = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	private decimal GetBidPrice(ICandleMessage candle)
	{
		if (Security?.BestBid?.Price is decimal bid && bid > 0m)
			return bid;

		if (Security?.LastPrice is decimal last && last > 0m)
			return last;

		return candle.ClosePrice;
	}

	private decimal GetAskPrice(ICandleMessage candle)
	{
		if (Security?.BestAsk?.Price is decimal ask && ask > 0m)
			return ask;

		if (Security?.LastPrice is decimal last && last > 0m)
			return last;

		return candle.ClosePrice;
	}

	private decimal ConvertPoints(decimal points)
	{
		if (points <= 0m)
			return 0m;

		if (_pointSize > 0m)
			return points * _pointSize;

		var step = Security?.Step ?? Security?.PriceStep ?? 0m;
		return step > 0m ? points * step : points;
	}

	private decimal CalculatePointSize()
	{
		var step = Security?.Step ?? Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}
}
