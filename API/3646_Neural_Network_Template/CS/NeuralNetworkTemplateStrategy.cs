using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy inspired by a neural-network driven template from MQL5.
/// </summary>
public class NeuralNetworkTemplateStrategy : Strategy
{
	private readonly StrategyParam<int> _barsToPattern;
	private readonly StrategyParam<int> _maxTakeProfitPoints;
	private readonly StrategyParam<int> _minTargetPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _profitMultiply;
	private readonly StrategyParam<decimal> _tradeLevel;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<decimal> _rsiHistory = new();
	private readonly Queue<decimal> _macdHistory = new();

	private decimal _rsiSum;
	private decimal _macdSum;
	private decimal? _targetPrice;
	private decimal? _stopPrice;
	private int _positionDirection;

	/// <summary>
	/// Number of candles used for pattern recognition.
	/// </summary>
	public int BarsToPattern
	{
		get => _barsToPattern.Value;
		set => _barsToPattern.Value = value;
	}

	/// <summary>
	/// Upper bound for calculated take-profit in points.
	/// </summary>
	public int MaxTakeProfitPoints
	{
		get => _maxTakeProfitPoints.Value;
		set => _maxTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum projected move required to open a trade.
	/// </summary>
	public int MinTargetPoints
	{
		get => _minTargetPoints.Value;
		set => _minTargetPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the projected move returned by the scoring model.
	/// </summary>
	public decimal ProfitMultiply
	{
		get => _profitMultiply.Value;
		set => _profitMultiply.Value = value;
	}

	/// <summary>
	/// Required confidence level before opening a new position.
	/// </summary>
	public decimal TradeLevel
	{
		get => _tradeLevel.Value;
		set => _tradeLevel.Value = value;
	}

	/// <summary>
	/// Trading volume for every market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NeuralNetworkTemplateStrategy"/>.
	/// </summary>
	public NeuralNetworkTemplateStrategy()
	{
		_barsToPattern = Param(nameof(BarsToPattern), 3)
			.SetGreaterThanZero()
			.SetDisplay("Bars", "Candles analysed", "Model")
			.SetCanOptimize(true);

		_maxTakeProfitPoints = Param(nameof(MaxTakeProfitPoints), 500)
			.SetGreaterThanZero()
			.SetDisplay("Max TP", "Maximum take-profit in points", "Risk");

		_minTargetPoints = Param(nameof(MinTargetPoints), 100)
			.SetGreaterThanZero()
			.SetDisplay("Min Target", "Minimum projected move in points", "Model");

		_stopLossPoints = Param(nameof(StopLossPoints), 300)
			.SetGreaterThanZero()
			.SetDisplay("Stop-Loss", "Stop-loss distance in points", "Risk");

		_profitMultiply = Param(nameof(ProfitMultiply), 0.8m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Profit Mult", "Take-profit multiplier", "Model");

		_tradeLevel = Param(nameof(TradeLevel), 0.9m)
			.SetBetween(0m, 1m)
			.SetDisplay("Trade Level", "Required confidence", "Model");

		_volume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("TF", "Working timeframe", "General");
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

		_rsi = null!;
		_macd = null!;
		_rsiHistory.Clear();
		_macdHistory.Clear();
		_rsiSum = 0m;
		_macdSum = 0m;
		_targetPrice = null;
		_stopPrice = null;
		_positionDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = 12 };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 48 }
			},
			SignalMa = { Length = 12 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_rsi, _macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageOpenPosition(candle);

		if (!_rsi.IsFormed || !_macd.IsFormed)
			return;

		var rsiDecimal = rsiValue.ToDecimal();

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdComponents)
			return;

		if (macdComponents.Macd is not decimal macdLine ||
			macdComponents.Signal is not decimal signalLine)
			return;

		UpdateHistory(rsiDecimal, macdLine - signalLine);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		EvaluateEntry(candle, rsiDecimal, macdLine, signalLine);
	}

	private void UpdateHistory(decimal rsiValue, decimal macdHistogram)
	{
		_rsiHistory.Enqueue(rsiValue);
		_rsiSum += rsiValue;
		if (_rsiHistory.Count > BarsToPattern)
			_rsiSum -= _rsiHistory.Dequeue();

		_macdHistory.Enqueue(macdHistogram);
		_macdSum += macdHistogram;
		if (_macdHistory.Count > BarsToPattern)
			_macdSum -= _macdHistory.Dequeue();
	}

	private void EvaluateEntry(ICandleMessage candle, decimal rsiValue, decimal macdLine, decimal signalLine)
	{
		if (_rsiHistory.Count < BarsToPattern || _macdHistory.Count < BarsToPattern)
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var normalizedRsi = Clamp((rsiValue - 50m) / 50m, -1m, 1m);
		var macdHistogram = macdLine - signalLine;
		var macdAverage = _macdHistory.Count == 0 ? 0m : _macdSum / _macdHistory.Count;
		var macdDeviation = macdHistogram - macdAverage;
		var normalizedMomentum = (decimal)Math.Tanh((double)(macdDeviation * 5m));

		var combinedScore = normalizedRsi * 0.6m + normalizedMomentum * 0.4m;
		var confidence = Math.Min(1m, Math.Abs(combinedScore));
		var projectedMove = macdDeviation * BarsToPattern;
		var projectedPoints = projectedMove / priceStep;

		if (combinedScore > 0m)
		{
			if (confidence < TradeLevel)
				return;

			if (projectedPoints < MinTargetPoints)
				return;

			var takeProfit = candle.ClosePrice + Math.Min(projectedMove * ProfitMultiply, MaxTakeProfitPoints * priceStep);
			var stopLoss = candle.ClosePrice - StopLossPoints * priceStep;

			if (takeProfit <= candle.ClosePrice)
				return;

			BuyMarket(TradeVolume);
			_targetPrice = takeProfit;
			_stopPrice = stopLoss;
			_positionDirection = 1;
		}
		else if (combinedScore < 0m)
		{
			if (confidence < TradeLevel)
				return;

			if (projectedPoints > -MinTargetPoints)
				return;

			var takeProfit = candle.ClosePrice + Math.Max(projectedMove * ProfitMultiply, -MaxTakeProfitPoints * priceStep);
			var stopLoss = candle.ClosePrice + StopLossPoints * priceStep;

			if (takeProfit >= candle.ClosePrice)
				return;

			SellMarket(TradeVolume);
			_targetPrice = takeProfit;
			_stopPrice = stopLoss;
			_positionDirection = -1;
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTargets();
				return;
			}

			if (_targetPrice.HasValue && candle.HighPrice >= _targetPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTargets();
			}
		}
		else if (Position < 0m)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
				return;
			}

			if (_targetPrice.HasValue && candle.LowPrice <= _targetPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
			}
		}
		else if (_positionDirection != 0)
		{
			ResetTargets();
		}
	}

	private void ResetTargets()
	{
		_targetPrice = null;
		_stopPrice = null;
		_positionDirection = 0;
	}

	private static decimal Clamp(decimal value, decimal min, decimal max)
	{
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}
}
