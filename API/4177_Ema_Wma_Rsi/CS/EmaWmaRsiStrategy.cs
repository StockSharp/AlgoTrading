using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy "EMA WMA RSI" created by cmillion.
/// Trades the crossover between EMA and WMA calculated on candle opens, filtered by RSI.
/// Includes optional opposite-position flattening, configurable point-based risk, and trailing stop choices.
/// </summary>
public class EmaWmaRsiStrategy : Strategy
{
	private const int HistoryLimit = 200;
	private const decimal TrailingBufferPoints = 5m;

	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<bool> _closeOppositePositions;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<TrailingSource> _trailingMode;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;
	private WeightedMovingAverage _wma = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal? _previousEma;
	private decimal? _previousWma;

	private decimal _pointSize;
	private decimal _stepPrice;

	private readonly List<ICandleMessage> _history = new();

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTake;
	private decimal? _shortTake;

	private enum TrailingSource
	{
		Fractals,
		CandleExtremes,
	}

	/// <summary>
	/// EMA period applied to candle open prices.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// WMA period applied to candle open prices.
	/// </summary>
	public int WmaPeriod
	{
		get => _wmaPeriod.Value;
		set => _wmaPeriod.Value = value;
	}

	/// <summary>
	/// RSI period used as a directional filter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in MetaTrader points. Zero activates fractal or candle trailing instead.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Closes opposite positions before opening a new trade.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOppositePositions.Value;
		set => _closeOppositePositions.Value = value;
	}

	/// <summary>
	/// Fixed trade volume. Set to zero to derive the size from <see cref="RiskPercent"/>.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Equity risk percentage used when <see cref="FixedVolume"/> equals zero.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Source used to build trailing stops when <see cref="TrailingStopPoints"/> is zero.
	/// </summary>
	public TrailingSource TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Candle type that drives indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EmaWmaRsiStrategy"/> class.
	/// </summary>
	public EmaWmaRsiStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for the exponential moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_wmaPeriod = Param(nameof(WmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Period for the weighted moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 30, 2);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for the RSI filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(8, 30, 2);

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss (points)", "Distance from entry expressed in MetaTrader points", "Risk")
			.SetRange(0m, 5000m)
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetDisplay("Take Profit (points)", "Profit target expressed in MetaTrader points", "Risk")
			.SetRange(0m, 5000m)
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 70m)
			.SetDisplay("Trailing Stop (points)", "Fixed trailing distance; zero enables adaptive trailing", "Risk")
			.SetRange(0m, 5000m)
			.SetCanOptimize(true);

		_closeOppositePositions = Param(nameof(CloseOppositePositions), false)
			.SetDisplay("Close Counter Trades", "Close the opposite position before entering", "Execution");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetDisplay("Fixed Volume", "Absolute volume used for market orders", "Execution")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetDisplay("Risk %", "Percentage of equity used when sizing trades dynamically", "Execution")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_trailingMode = Param(nameof(TrailingMode), TrailingSource.CandleExtremes)
			.SetDisplay("Trailing Source", "Price source used when adaptive trailing is enabled", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");
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

		_previousEma = null;
		_previousWma = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
		_history.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = Security?.Step ?? Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
		{
			_pointSize = 0.0001m;
		}

		_stepPrice = Security?.StepPrice ?? 0m;

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_wma = new WeightedMovingAverage { Length = WmaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _wma);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			DrawIndicator(rsiArea, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		_history.Add(candle);
		if (_history.Count > HistoryLimit)
		{
			_history.RemoveAt(0);
		}

		var emaValue = _ema.Process(candle.OpenPrice);
		var wmaValue = _wma.Process(candle.OpenPrice);
		var rsiValue = _rsi.Process(candle.OpenPrice);

		UpdateRiskManagement(candle);

		if (!_ema.IsFormed || !_wma.IsFormed || !_rsi.IsFormed)
		{
			_previousEma = emaValue;
			_previousWma = wmaValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousEma = emaValue;
			_previousWma = wmaValue;
			return;
		}

		if (_previousEma is decimal prevEma && _previousWma is decimal prevWma)
		{
			var buySignal = emaValue < wmaValue && prevEma > prevWma && rsiValue > 50m;
			var sellSignal = emaValue > wmaValue && prevEma < prevWma && rsiValue < 50m;

			if (buySignal)
			{
				TryEnterLong(candle, emaValue, wmaValue, rsiValue);
			}
			else if (sellSignal)
			{
				TryEnterShort(candle, emaValue, wmaValue, rsiValue);
			}
		}

		_previousEma = emaValue;
		_previousWma = wmaValue;
	}

	private void TryEnterLong(ICandleMessage candle, decimal emaValue, decimal wmaValue, decimal rsiValue)
	{
		if (Position > 0)
		{
			return;
		}

		if (Position < 0)
		{
			if (CloseOppositePositions)
			{
				var volumeToClose = Math.Abs(Position);
				if (volumeToClose > 0m)
				{
					BuyMarket(volumeToClose);
				}
				ResetShortState();
			}
			else
			{
				return;
			}
		}

		var stopDistance = GetStopDistance();
		var volume = GetTradeVolume(stopDistance);
		if (volume <= 0m)
		{
			return;
		}

		volume = NormalizeVolume(volume);
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);

		var entryPrice = candle.ClosePrice;
		_longEntryPrice = entryPrice;
		_longStop = stopDistance > 0m ? entryPrice - stopDistance : null;
		_longTake = GetTakeProfitDistance() is { } takeDist && takeDist > 0m ? entryPrice + takeDist : null;
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;

		LogInfo($"Long entry placed. EMA={emaValue:F5}, WMA={wmaValue:F5}, RSI={rsiValue:F2}.");
	}

	private void TryEnterShort(ICandleMessage candle, decimal emaValue, decimal wmaValue, decimal rsiValue)
	{
		if (Position < 0)
		{
			return;
		}

		if (Position > 0)
		{
			if (CloseOppositePositions)
			{
				var volumeToClose = Math.Abs(Position);
				if (volumeToClose > 0m)
				{
					SellMarket(volumeToClose);
				}
				ResetLongState();
			}
			else
			{
				return;
			}
		}

		var stopDistance = GetStopDistance();
		var volume = GetTradeVolume(stopDistance);
		if (volume <= 0m)
		{
			return;
		}

		volume = NormalizeVolume(volume);
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);

		var entryPrice = candle.ClosePrice;
		_shortEntryPrice = entryPrice;
		_shortStop = stopDistance > 0m ? entryPrice + stopDistance : null;
		_shortTake = GetTakeProfitDistance() is { } takeDist && takeDist > 0m ? entryPrice - takeDist : null;
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;

		LogInfo($"Short entry placed. EMA={emaValue:F5}, WMA={wmaValue:F5}, RSI={rsiValue:F2}.");
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position > 0 && _longEntryPrice is decimal longEntry)
		{
			var trailingCandidate = CalculateTrailingStop(true, candle.ClosePrice);
			if (trailingCandidate is decimal candidate && candidate > longEntry)
			{
				if (_longStop is not decimal current || candidate > current)
				{
					_longStop = candidate;
				}
			}

			if (_longTake is decimal takePrice && candle.HighPrice >= takePrice)
			{
				CloseLongPosition();
				return;
			}

			if (_longStop is decimal stopPrice && candle.LowPrice <= stopPrice)
			{
				CloseLongPosition();
				return;
			}
		}
		else
		{
			ResetLongState();
		}

		if (Position < 0 && _shortEntryPrice is decimal shortEntry)
		{
			var trailingCandidate = CalculateTrailingStop(false, candle.ClosePrice);
			if (trailingCandidate is decimal candidate && candidate < shortEntry)
			{
				if (_shortStop is not decimal current || candidate < current)
				{
					_shortStop = candidate;
				}
			}

			if (_shortTake is decimal takePrice && candle.LowPrice <= takePrice)
			{
				CloseShortPosition();
				return;
			}

			if (_shortStop is decimal stopPrice && candle.HighPrice >= stopPrice)
			{
				CloseShortPosition();
				return;
			}
		}
		else
		{
			ResetShortState();
		}
	}

	private void CloseLongPosition()
	{
		var volume = Math.Abs(Position);
		if (volume > 0m)
		{
			SellMarket(volume);
		}
		ResetLongState();
	}

	private void CloseShortPosition()
	{
		var volume = Math.Abs(Position);
		if (volume > 0m)
		{
			BuyMarket(volume);
		}
		ResetShortState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}

	private decimal? CalculateTrailingStop(bool isLong, decimal referencePrice)
	{
		var trailingDistance = GetTrailingDistance();
		if (trailingDistance > 0m)
		{
			return isLong ? referencePrice - trailingDistance : referencePrice + trailingDistance;
		}

		if (_history.Count < 5)
		{
			return null;
		}

		var buffer = TrailingBufferPoints * _pointSize;
		if (buffer < 0m)
		{
			buffer = 0m;
		}

		if (TrailingMode == TrailingSource.Fractals)
		{
			for (var i = _history.Count - 3; i >= 2; i--)
			{
				if (isLong)
				{
					if (IsFractalLow(i))
					{
						var candidate = _history[i].LowPrice;
						if (referencePrice - buffer > candidate)
						{
							return candidate;
						}
					}
				}
				else if (IsFractalHigh(i))
				{
					var candidate = _history[i].HighPrice;
					if (referencePrice + buffer < candidate)
					{
						return candidate;
					}
				}
			}
		}
		else
		{
			for (var i = _history.Count - 2; i >= 0; i--)
			{
				var candle = _history[i];
				if (isLong)
				{
					var candidate = candle.LowPrice;
					if (referencePrice - buffer > candidate)
					{
						return candidate;
					}
				}
				else
				{
					var candidate = candle.HighPrice;
					if (referencePrice + buffer < candidate)
					{
						return candidate;
					}
				}
			}
		}

		return null;
	}

	private bool IsFractalLow(int index)
	{
		var current = _history[index].LowPrice;
		return current <= _history[index - 1].LowPrice
			&& current <= _history[index - 2].LowPrice
			&& current <= _history[index + 1].LowPrice
			&& current <= _history[index + 2].LowPrice;
	}

	private bool IsFractalHigh(int index)
	{
		var current = _history[index].HighPrice;
		return current >= _history[index - 1].HighPrice
			&& current >= _history[index - 2].HighPrice
			&& current >= _history[index + 1].HighPrice
			&& current >= _history[index + 2].HighPrice;
	}

	private decimal GetStopDistance()
	{
		if (StopLossPoints <= 0m)
		{
			return 0m;
		}

		return StopLossPoints * _pointSize;
	}

	private decimal? GetTakeProfitDistance()
	{
		if (TakeProfitPoints <= 0m)
		{
			return null;
		}

		return TakeProfitPoints * _pointSize;
	}

	private decimal GetTrailingDistance()
	{
		if (TrailingStopPoints <= 0m)
		{
			return 0m;
		}

		return TrailingStopPoints * _pointSize;
	}

	private decimal GetTradeVolume(decimal stopDistance)
	{
		if (FixedVolume > 0m)
		{
			return FixedVolume;
		}

		var effectiveStop = stopDistance;
		if (effectiveStop <= 0m)
		{
			effectiveStop = GetTrailingDistance();
		}

		if (effectiveStop <= 0m)
		{
			return 0m;
		}

		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m || RiskPercent <= 0m)
		{
			return 0m;
		}

		decimal riskPerUnit;
		if (_stepPrice > 0m && _pointSize > 0m)
		{
			riskPerUnit = effectiveStop / _pointSize * _stepPrice;
		}
		else
		{
			riskPerUnit = effectiveStop;
		}

		if (riskPerUnit <= 0m)
		{
			return 0m;
		}

		var riskValue = equity * RiskPercent / 100m;
		return riskValue / riskPerUnit;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		{
			return 0m;
		}

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step));
			return steps * step;
		}

		return volume;
	}
}
