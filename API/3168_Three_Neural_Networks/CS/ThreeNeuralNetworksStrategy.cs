using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe strategy converted from the MetaTrader "Three neural networks" expert advisor.
/// Combines smoothed moving averages from H1, H4, and D1 charts to derive directional signals.
/// </summary>
public class ThreeNeuralNetworksStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<MoneyManagementMode> _moneyManagementMode;
	private readonly StrategyParam<decimal> _volumeOrRisk;
	private readonly StrategyParam<int> _h1Period;
	private readonly StrategyParam<int> _h1Shift;
	private readonly StrategyParam<int> _h4Period;
	private readonly StrategyParam<int> _h4Shift;
	private readonly StrategyParam<int> _d1Period;
	private readonly StrategyParam<int> _d1Shift;
	private readonly StrategyParam<decimal> _p1;
	private readonly StrategyParam<decimal> _p2;
	private readonly StrategyParam<decimal> _p3;
	private readonly StrategyParam<decimal> _q1;
	private readonly StrategyParam<decimal> _q2;
	private readonly StrategyParam<decimal> _q3;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<decimal> _k2;
	private readonly StrategyParam<decimal> _k3;
	private readonly StrategyParam<bool> _enableDetailedLog;

	private readonly List<decimal> _h1History = new();
	private readonly List<decimal> _h4History = new();
	private readonly List<decimal> _d1History = new();

	private SmoothedMovingAverage _h1Ma = null!;
	private SmoothedMovingAverage _h4Ma = null!;
	private SmoothedMovingAverage _d1Ma = null!;

	private decimal _pipSize;
	private decimal _previousPosition;
	private decimal _lastSignalPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Specifies how trade volume is calculated.
	/// </summary>
	public enum MoneyManagementMode
	{
		/// <summary>
		/// Uses the configured value as a fixed lot size.
		/// </summary>
		FixedLot,

		/// <summary>
		/// Interprets the configured value as a risk percentage of the portfolio.
		/// </summary>
		RiskPercent,
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ThreeNeuralNetworksStrategy"/>.
	/// </summary>
	public ThreeNeuralNetworksStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetRange(0m, 10000m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetRange(0m, 10000m)
		.SetDisplay("Take Profit (pips)", "Target profit distance in pips", "Risk")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
		.SetRange(0m, 10000m)
		.SetDisplay("Trailing Stop (pips)", "Distance between price and trailing stop", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetRange(0m, 10000m)
		.SetDisplay("Trailing Step (pips)", "Minimum improvement before the trailing stop is moved", "Risk");

		_moneyManagementMode = Param(nameof(ManagementMode), MoneyManagementMode.RiskPercent)
		.SetDisplay("Money Management", "Select between fixed volume or risk-percentage sizing", "Money Management");

		_volumeOrRisk = Param(nameof(VolumeOrRisk), 1m)
		.SetRange(0m, 1000m)
		.SetDisplay("Lot or Risk", "Lot size for FixedLot or risk percent for RiskPercent", "Money Management")
		.SetCanOptimize(true);

		_h1Period = Param(nameof(H1Period), 2)
		.SetRange(1, 500)
		.SetDisplay("H1 MA Period", "Smoothed moving average period on the H1 timeframe", "Indicators")
		.SetCanOptimize(true);

		_h1Shift = Param(nameof(H1Shift), 5)
		.SetRange(0, 100)
		.SetDisplay("H1 MA Shift", "Horizontal shift applied to the H1 moving average", "Indicators");

		_h4Period = Param(nameof(H4Period), 2)
		.SetRange(1, 500)
		.SetDisplay("H4 MA Period", "Smoothed moving average period on the H4 timeframe", "Indicators")
		.SetCanOptimize(true);

		_h4Shift = Param(nameof(H4Shift), 5)
		.SetRange(0, 100)
		.SetDisplay("H4 MA Shift", "Horizontal shift applied to the H4 moving average", "Indicators");

		_d1Period = Param(nameof(D1Period), 2)
		.SetRange(1, 500)
		.SetDisplay("D1 MA Period", "Smoothed moving average period on the D1 timeframe", "Indicators")
		.SetCanOptimize(true);

		_d1Shift = Param(nameof(D1Shift), 5)
		.SetRange(0, 100)
		.SetDisplay("D1 MA Shift", "Horizontal shift applied to the D1 moving average", "Indicators");

		_p1 = Param(nameof(P1), 0.1m)
		.SetDisplay("p1 Weight", "Weight of the first H1 neural component", "Weights");

		_p2 = Param(nameof(P2), 0.1m)
		.SetDisplay("p2 Weight", "Weight of the second H1 neural component", "Weights");

		_p3 = Param(nameof(P3), 0.1m)
		.SetDisplay("p3 Weight", "Weight of the third H1 neural component", "Weights");

		_q1 = Param(nameof(Q1), 0.1m)
		.SetDisplay("q1 Weight", "Weight of the first H4 neural component", "Weights");

		_q2 = Param(nameof(Q2), 0.1m)
		.SetDisplay("q2 Weight", "Weight of the second H4 neural component", "Weights");

		_q3 = Param(nameof(Q3), 0.1m)
		.SetDisplay("q3 Weight", "Weight of the third H4 neural component", "Weights");

		_k1 = Param(nameof(K1), 0.1m)
		.SetDisplay("k1 Weight", "Weight of the first D1 neural component", "Weights");

		_k2 = Param(nameof(K2), 0.1m)
		.SetDisplay("k2 Weight", "Weight of the second D1 neural component", "Weights");

		_k3 = Param(nameof(K3), 0.1m)
		.SetDisplay("k3 Weight", "Weight of the third D1 neural component", "Weights");

		_enableDetailedLog = Param(nameof(EnableDetailedLog), false)
		.SetDisplay("Enable Debug Log", "Write verbose diagnostic messages", "Diagnostics");
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Selected money management mode.
	/// </summary>
	public MoneyManagementMode ManagementMode
	{
		get => _moneyManagementMode.Value;
		set => _moneyManagementMode.Value = value;
	}

	/// <summary>
	/// Lot size or risk percentage depending on <see cref="ManagementMode"/>.
	/// </summary>
	public decimal VolumeOrRisk
	{
		get => _volumeOrRisk.Value;
		set => _volumeOrRisk.Value = value;
	}

	/// <summary>
	/// Smoothed moving average period on the H1 timeframe.
	/// </summary>
	public int H1Period
	{
		get => _h1Period.Value;
		set => _h1Period.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to the H1 moving average.
	/// </summary>
	public int H1Shift
	{
		get => _h1Shift.Value;
		set => _h1Shift.Value = value;
	}

	/// <summary>
	/// Smoothed moving average period on the H4 timeframe.
	/// </summary>
	public int H4Period
	{
		get => _h4Period.Value;
		set => _h4Period.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to the H4 moving average.
	/// </summary>
	public int H4Shift
	{
		get => _h4Shift.Value;
		set => _h4Shift.Value = value;
	}

	/// <summary>
	/// Smoothed moving average period on the D1 timeframe.
	/// </summary>
	public int D1Period
	{
		get => _d1Period.Value;
		set => _d1Period.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to the D1 moving average.
	/// </summary>
	public int D1Shift
	{
		get => _d1Shift.Value;
		set => _d1Shift.Value = value;
	}

	/// <summary>
	/// Weight of the first H1 neural component.
	/// </summary>
	public decimal P1
	{
		get => _p1.Value;
		set => _p1.Value = value;
	}

	/// <summary>
	/// Weight of the second H1 neural component.
	/// </summary>
	public decimal P2
	{
		get => _p2.Value;
		set => _p2.Value = value;
	}

	/// <summary>
	/// Weight of the third H1 neural component.
	/// </summary>
	public decimal P3
	{
		get => _p3.Value;
		set => _p3.Value = value;
	}

	/// <summary>
	/// Weight of the first H4 neural component.
	/// </summary>
	public decimal Q1
	{
		get => _q1.Value;
		set => _q1.Value = value;
	}

	/// <summary>
	/// Weight of the second H4 neural component.
	/// </summary>
	public decimal Q2
	{
		get => _q2.Value;
		set => _q2.Value = value;
	}

	/// <summary>
	/// Weight of the third H4 neural component.
	/// </summary>
	public decimal Q3
	{
		get => _q3.Value;
		set => _q3.Value = value;
	}

	/// <summary>
	/// Weight of the first D1 neural component.
	/// </summary>
	public decimal K1
	{
		get => _k1.Value;
		set => _k1.Value = value;
	}

	/// <summary>
	/// Weight of the second D1 neural component.
	/// </summary>
	public decimal K2
	{
		get => _k2.Value;
		set => _k2.Value = value;
	}

	/// <summary>
	/// Weight of the third D1 neural component.
	/// </summary>
	public decimal K3
	{
		get => _k3.Value;
		set => _k3.Value = value;
	}

	/// <summary>
	/// Enables verbose diagnostic logging.
	/// </summary>
	public bool EnableDetailedLog
	{
		get => _enableDetailedLog.Value;
		set => _enableDetailedLog.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security is null)
		yield break;

		yield return (security, TimeSpan.FromHours(1).TimeFrame());
		yield return (security, TimeSpan.FromHours(4).TimeFrame());
		yield return (security, TimeSpan.FromDays(1).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_h1History.Clear();
		_h4History.Clear();
		_d1History.Clear();

		_pipSize = 0m;
		_previousPosition = 0m;
		_lastSignalPrice = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		_previousPosition = Position;

		_h1Ma = new SmoothedMovingAverage
		{
			Length = Math.Max(1, H1Period),
			CandlePrice = CandlePrice.Median,
		};

		_h4Ma = new SmoothedMovingAverage
		{
			Length = Math.Max(1, H4Period),
			CandlePrice = CandlePrice.Median,
		};

		_d1Ma = new SmoothedMovingAverage
		{
			Length = Math.Max(1, D1Period),
			CandlePrice = CandlePrice.Median,
		};

		SubscribeCandles(TimeSpan.FromHours(1).TimeFrame())
		.Bind(_h1Ma, ProcessH1Candle)
		.Start();

		SubscribeCandles(TimeSpan.FromHours(4).TimeFrame())
		.Bind(_h4Ma, ProcessH4Candle)
		.Start();

		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
		.Bind(_d1Ma, ProcessD1Candle)
		.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (_previousPosition <= 0m && Position > 0m)
		{
			var entryPrice = PositionPrice ?? _lastSignalPrice;
			_longStop = StopLossPips > 0m ? entryPrice - ToAbsoluteUnit(StopLossPips) : null;
			_longTake = TakeProfitPips > 0m ? entryPrice + ToAbsoluteUnit(TakeProfitPips) : null;
			_shortStop = null;
			_shortTake = null;
		}
		else if (_previousPosition >= 0m && Position < 0m)
		{
			var entryPrice = PositionPrice ?? _lastSignalPrice;
			_shortStop = StopLossPips > 0m ? entryPrice + ToAbsoluteUnit(StopLossPips) : null;
			_shortTake = TakeProfitPips > 0m ? entryPrice - ToAbsoluteUnit(TakeProfitPips) : null;
			_longStop = null;
			_longTake = null;
		}
		else if (Position == 0m && _previousPosition != 0m)
		{
			_longStop = null;
			_longTake = null;
			_shortStop = null;
			_shortTake = null;
			_lastSignalPrice = 0m;
		}

		_previousPosition = Position;
	}

	private void ProcessH1Candle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateTrailing(candle);
		if (HandleRiskManagement(candle))
		return;

		if (!_h1Ma.IsFormed)
		return;

		AddIndicatorValue(_h1History, maValue, H1Shift);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!TryCalculateNeurons(out var n1, out var n2, out var n3))
		return;

		if (EnableDetailedLog)
		{
			LogInfo($"Neural outputs H1={n1}, H4={n2}, D1={n3} on {candle.OpenTime:O}. Close={candle.ClosePrice:0.#####}");
		}

		if (n1 > 0m && n2 > 0m && n3 > 0m)
		{
			TryOpenLong(candle);
		}
		else if (n1 > 0m && n2 < 0m && n3 < 0m)
		{
			TryOpenShort(candle);
		}
	}

	private void ProcessH4Candle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_h4Ma.IsFormed)
		return;

		AddIndicatorValue(_h4History, maValue, H4Shift);
	}

	private void ProcessD1Candle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_d1Ma.IsFormed)
		return;

		AddIndicatorValue(_d1History, maValue, D1Shift);
	}

	private void TryOpenLong(ICandleMessage candle)
	{
		if (Position < 0m)
		{
			ClosePosition();
			return;
		}

		if (Position > 0m)
		return;

		var price = candle.ClosePrice;
		var volume = GetOrderVolume(price);
		if (volume <= 0m)
		return;

		_lastSignalPrice = price;
		var order = BuyMarket(volume);
		if (order != null && EnableDetailedLog)
		{
			LogInfo($"Submitted long order. Price={price:0.#####}, Volume={volume:0.####}");
		}
	}

	private void TryOpenShort(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			ClosePosition();
			return;
		}

		if (Position < 0m)
		return;

		var price = candle.ClosePrice;
		var volume = GetOrderVolume(price);
		if (volume <= 0m)
		return;

		_lastSignalPrice = price;
		var order = SellMarket(volume);
		if (order != null && EnableDetailedLog)
		{
			LogInfo($"Submitted short order. Price={price:0.#####}, Volume={volume:0.####}");
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _pipSize <= 0m)
		return;

		var trailingDistance = ToAbsoluteUnit(TrailingStopPips);
		var trailingStep = TrailingStepPips > 0m ? ToAbsoluteUnit(TrailingStepPips) : 0m;

		if (Position > 0m && PositionPrice is decimal entry)
		{
			var priceAdvance = candle.ClosePrice - entry;
			if (priceAdvance >= trailingDistance + trailingStep)
			{
				var newStop = candle.ClosePrice - trailingDistance;
				if (_longStop is not decimal currentStop || newStop - currentStop >= trailingStep)
				{
					_longStop = newStop;
					if (EnableDetailedLog)
					{
						LogInfo($"Trailing long stop to {newStop:0.#####}");
					}
				}
			}
		}
		else if (Position < 0m && PositionPrice is decimal entryPrice)
		{
			var priceAdvance = entryPrice - candle.ClosePrice;
			if (priceAdvance >= trailingDistance + trailingStep)
			{
				var newStop = candle.ClosePrice + trailingDistance;
				if (_shortStop is not decimal currentStop || currentStop - newStop >= trailingStep)
				{
					_shortStop = newStop;
					if (EnableDetailedLog)
					{
						LogInfo($"Trailing short stop to {newStop:0.#####}");
					}
				}
			}
		}
	}

	private bool HandleRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				if (EnableDetailedLog)
				{
					LogInfo($"Long stop-loss hit at {stop:0.#####}");
				}
				ClosePosition();
				return true;
			}

			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				if (EnableDetailedLog)
				{
					LogInfo($"Long take-profit reached at {take:0.#####}");
				}
				ClosePosition();
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				if (EnableDetailedLog)
				{
					LogInfo($"Short stop-loss hit at {stop:0.#####}");
				}
				ClosePosition();
				return true;
			}

			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				if (EnableDetailedLog)
				{
					LogInfo($"Short take-profit reached at {take:0.#####}");
				}
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private bool TryCalculateNeurons(out decimal n1, out decimal n2, out decimal n3)
	{
		var h1Ready = TryCalculateNeuron(_h1History, H1Shift, P1, P2, P3, out n1);
		var h4Ready = TryCalculateNeuron(_h4History, H4Shift, Q1, Q2, Q3, out n2);
		var d1Ready = TryCalculateNeuron(_d1History, D1Shift, K1, K2, K3, out n3);
		return h1Ready && h4Ready && d1Ready;
	}

	private static void AddIndicatorValue(List<decimal> history, decimal value, int shift)
	{
		history.Add(value);
		var maxCount = Math.Max(shift + 8, 8);
		if (history.Count > maxCount)
		{
			history.RemoveRange(0, history.Count - maxCount);
		}
	}

	private static bool TryCalculateNeuron(List<decimal> history, int shift, decimal w1, decimal w2, decimal w3, out decimal result)
	{
		result = 0m;
		var baseIndex = history.Count - 1 - shift;
		if (baseIndex < 4)
		return false;

		var v1 = history[baseIndex - 1];
		var v2 = history[baseIndex - 2];
		var v3 = history[baseIndex - 3];
		var v4 = history[baseIndex - 4];

		if (v1 == 0m || v2 == 0m || v3 == 0m || v4 == 0m)
		return false;

		var term1 = ((v1 - v2) / v1) * w1;
		var term2 = ((v2 - v3) / v3) * w2;
		var term3 = ((v3 - v4) / v4) * w3;
		result = Math.Round((term1 + term2 + term3) * 10000m);
		return true;
	}

	private decimal GetOrderVolume(decimal price)
	{
		var volume = VolumeOrRisk;

		if (ManagementMode == MoneyManagementMode.RiskPercent && Portfolio is { CurrentValue: > 0m } portfolio && price > 0m)
		{
			var riskAmount = portfolio.CurrentValue * VolumeOrRisk / 100m;
			if (riskAmount > 0m)
			{
				volume = riskAmount / price;
			}
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security is null)
		return volume;

		if (security.VolumeStep is decimal step && step > 0m)
		{
			volume = Math.Floor(volume / step) * step;
		}

		if (security.VolumeMin is decimal min && min > 0m && volume < min)
		{
			volume = min;
		}

		if (security.VolumeMax is decimal max && max > 0m && volume > max)
		{
			volume = max;
		}

		return volume;
	}

	private decimal ToAbsoluteUnit(decimal pips)
	{
		return _pipSize > 0m ? pips * _pipSize : 0m;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security is null)
		return 0.0001m;

		if (security.PriceStep is decimal step && step > 0m)
		{
			var decimals = security.Decimals;
			var adjust = decimals is 3 or 5 ? 10m : 1m;
			return step * adjust;
		}

		if (security.Decimals is int digits && digits > 0)
		{
			var adjust = digits is 3 or 5 ? 10m : 1m;
			return (decimal)Math.Pow(10, -digits) * adjust;
		}

		return 0.0001m;
	}
}
