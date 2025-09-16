using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fuzzy logic strategy combining Bill Williams oscillators, RSI and DeMarker.
/// Opens short positions when the fuzzy score indicates exhaustion and
/// opens long positions during oversold momentum reversals.
/// </summary>
public class FuzzyLogicStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _deMarkerPeriod;

	private WilliamsR _williamsIndicator = null!;
	private RelativeStrengthIndex _rsiIndicator = null!;
	private readonly SmoothedMovingAverage _jaw = new() { Length = 13 };
	private readonly SmoothedMovingAverage _teeth = new() { Length = 8 };
	private readonly SmoothedMovingAverage _lips = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoFast = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlow = new() { Length = 34 };
	private readonly SimpleMovingAverage _acAverage = new() { Length = 5 };

	private readonly decimal?[] _jawBuffer = new decimal?[9];
	private readonly decimal?[] _teethBuffer = new decimal?[6];
	private readonly decimal?[] _lipsBuffer = new decimal?[4];
	private int _jawCount;
	private int _teethCount;
	private int _lipsCount;

	private readonly decimal[] _acHistory = new decimal[5];
	private int _acCount;

	private readonly Queue<decimal> _deMaxQueue = new();
	private readonly Queue<decimal> _deMinQueue = new();
	private decimal _deMaxSum;
	private decimal _deMinSum;
	private decimal? _previousHigh;
	private decimal? _previousLow;

	/// <summary>
	/// Candle series type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Decision value that triggers long entries.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Decision value that triggers short entries.
	/// </summary>
	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Williams %R lookback.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// RSI lookback.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// DeMarker oscillator lookback.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FuzzyLogicStrategy"/>.
	/// </summary>
	public FuzzyLogicStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to analyze", "General");

		_buyThreshold = Param(nameof(BuyThreshold), 0.25m)
		.SetDisplay("Buy Threshold", "Decision level for long entries", "Trading")
		.SetRange(0.1m, 0.5m)
		.SetCanOptimize(true);

		_sellThreshold = Param(nameof(SellThreshold), 0.75m)
		.SetDisplay("Sell Threshold", "Decision level for short entries", "Trading")
		.SetRange(0.5m, 0.9m)
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 60m)
		.SetDisplay("Stop Loss (points)", "Protective stop distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
		.SetDisplay("Take Profit (points)", "Target distance in price steps", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
		.SetDisplay("Trailing Stop (points)", "Trailing stop distance in price steps", "Risk");

		_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
		.SetDisplay("Williams %R Period", "Lookback for Williams %R", "Indicators")
		.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Lookback for RSI", "Indicators")
		.SetCanOptimize(true);

		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
		.SetDisplay("DeMarker Period", "Lookback for DeMarker", "Indicators")
		.SetCanOptimize(true);

		Volume = 1;
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

		Array.Clear(_jawBuffer);
		Array.Clear(_teethBuffer);
		Array.Clear(_lipsBuffer);
		_jawCount = 0;
		_teethCount = 0;
		_lipsCount = 0;

		Array.Clear(_acHistory);
		_acCount = 0;

		_deMaxQueue.Clear();
		_deMinQueue.Clear();
		_deMaxSum = 0m;
		_deMinSum = 0m;
		_previousHigh = null;
		_previousLow = null;

		_jaw.Reset();
		_teeth.Reset();
		_lips.Reset();
		_aoFast.Reset();
		_aoSlow.Reset();
		_acAverage.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_williamsIndicator = new WilliamsR { Length = WilliamsPeriod };
		_rsiIndicator = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_williamsIndicator, _rsiIndicator, ProcessCandle)
		.Start();

		var step = Security?.PriceStep ?? 1m;
		var stopDistance = TrailingStopPoints > 0m ? TrailingStopPoints : StopLossPoints;
		StartProtection(
		takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Absolute) : null,
		stopLoss: stopDistance > 0m ? new Unit(stopDistance * step, UnitTypes.Absolute) : null,
		isStopTrailing: TrailingStopPoints > 0m
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williamsIndicator);
			DrawIndicator(area, _rsiIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue wprValue, IIndicatorValue rsiValue)
	{
		// Work only with finished candles to avoid partial data.
		if (candle.State != CandleStates.Finished)
		return;

		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(hl2);
		var teethValue = _teeth.Process(hl2);
		var lipsValue = _lips.Process(hl2);
		var aoFastValue = _aoFast.Process(hl2);
		var aoSlowValue = _aoSlow.Process(hl2);

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal || !aoFastValue.IsFinal || !aoSlowValue.IsFinal)
		{
			UpdateDeMarker(candle);
			return;
		}

		var jawShifted = UpdateShiftBuffer(_jawBuffer, ref _jawCount, 8, jawValue.GetValue<decimal>());
		var teethShifted = UpdateShiftBuffer(_teethBuffer, ref _teethCount, 5, teethValue.GetValue<decimal>());
		var lipsShifted = UpdateShiftBuffer(_lipsBuffer, ref _lipsCount, 3, lipsValue.GetValue<decimal>());

		if (jawShifted is null || teethShifted is null || lipsShifted is null)
		{
			UpdateDeMarker(candle);
			return;
		}

		var ao = aoFastValue.GetValue<decimal>() - aoSlowValue.GetValue<decimal>();
		var acAverageValue = _acAverage.Process(ao);
		if (!acAverageValue.IsFinal)
		{
			UpdateDeMarker(candle);
			return;
		}

		var ac = ao - acAverageValue.GetValue<decimal>();
		var deMarker = UpdateDeMarker(candle);
		if (deMarker is null)
		{
			UpdateAcHistory(ac);
			return;
		}

		if (!wprValue.IsFinal || !rsiValue.IsFinal)
		{
			UpdateAcHistory(ac);
			return;
		}

		if (_acCount < _acHistory.Length)
		{
			UpdateAcHistory(ac);
			return;
		}

		var sumGator = Math.Abs(jawShifted.Value - teethShifted.Value) + Math.Abs(teethShifted.Value - lipsShifted.Value);
		var wpr = wprValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var decision = CalculateDecision(sumGator, wpr, deMarker.Value, rsi);

		if (IsFormedAndOnlineAndAllowTrading() && Position == 0)
		{
			var volume = Volume;
			if (decision > SellThreshold)
			{
				SellMarket(volume);
				LogInfo($"Fuzzy decision {decision:F2} triggered a short entry.");
			}
			else if (decision < BuyThreshold)
			{
				BuyMarket(volume);
				LogInfo($"Fuzzy decision {decision:F2} triggered a long entry.");
			}
		}

		UpdateAcHistory(ac);
	}

	private decimal? UpdateShiftBuffer(decimal?[] buffer, ref int filled, int shift, decimal value)
	{
		for (var i = 0; i < shift; i++)
		buffer[i] = buffer[i + 1];
		buffer[shift] = value;

		if (filled >= shift)
		return buffer[0];

		filled++;
		return null;
	}

	private decimal? UpdateDeMarker(ICandleMessage candle)
	{
		// Store previous extremes to compute DeMarker increments.
		if (_previousHigh is null || _previousLow is null)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return null;
		}

		var deMax = Math.Max(candle.HighPrice - _previousHigh.Value, 0m);
		var deMin = Math.Max(_previousLow.Value - candle.LowPrice, 0m);

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;

		if (_deMaxQueue.Count == DeMarkerPeriod)
		{
			_deMaxSum -= _deMaxQueue.Dequeue();
			_deMinSum -= _deMinQueue.Dequeue();
		}

		_deMaxQueue.Enqueue(deMax);
		_deMinQueue.Enqueue(deMin);
		_deMaxSum += deMax;
		_deMinSum += deMin;

		if (_deMaxQueue.Count < DeMarkerPeriod)
		return null;

		var denominator = _deMaxSum + _deMinSum;
		return denominator == 0m ? 0m : _deMaxSum / denominator;
	}

	private void UpdateAcHistory(decimal ac)
	{
		for (var i = _acHistory.Length - 1; i > 0; i--)
		_acHistory[i] = _acHistory[i - 1];
		_acHistory[0] = ac;

		if (_acCount < _acHistory.Length)
		_acCount++;
	}

	private decimal CalculateDecision(decimal sumGator, decimal wpr, decimal deMarker, decimal rsi)
	{
		var rang = new decimal[5, 5];
		var summary = new decimal[5];

		var gatorLevels = new[] { 0.010m, 0.020m, 0.030m, 0.040m, 0.040m, 0.030m, 0.020m, 0.010m };
		var wprLevels = new[] { -95m, -90m, -80m, -75m, -25m, -20m, -10m, -5m };
		var acLevels = new[] { 5m, 4m, 3m, 2m, 2m, 3m, 4m, 5m };
		var deMarkerLevels = new[] { 0.15m, 0.20m, 0.25m, 0.30m, 0.70m, 0.75m, 0.80m, 0.85m };
		var rsiLevels = new[] { 25m, 30m, 35m, 40m, 60m, 65m, 70m, 75m };
		var weights = new[] { 0.133m, 0.133m, 0.133m, 0.268m, 0.333m };

		// 1) Gator oscillator membership.
		if (sumGator < gatorLevels[0])
		{
			rang[0, 0] = 0.5m;
			rang[0, 4] = 0.5m;
		}
		if (sumGator >= gatorLevels[0] && sumGator < gatorLevels[1])
		{
			var part = (sumGator - gatorLevels[0]) / (gatorLevels[1] - gatorLevels[0]);
			rang[0, 0] = (1m - part) / 2m;
			rang[0, 1] = (1m - rang[0, 0] * 2m) / 2m;
			rang[0, 4] = rang[0, 0];
			rang[0, 3] = rang[0, 1];
		}
		if (sumGator >= gatorLevels[1] && sumGator < gatorLevels[2])
		{
			rang[0, 1] = 0.5m;
			rang[0, 3] = 0.5m;
		}
		if (sumGator >= gatorLevels[2] && sumGator < gatorLevels[3])
		{
			var part = (sumGator - gatorLevels[2]) / (gatorLevels[3] - gatorLevels[2]);
			rang[0, 1] = (1m - part) / 2m;
			rang[0, 2] = 1m - rang[0, 1] * 2m;
			rang[0, 3] = rang[0, 1];
		}
		if (sumGator >= gatorLevels[3])
		rang[0, 2] = 1m;

		// 2) Williams %R membership.
		if (wpr < wprLevels[0])
		rang[1, 0] = 1m;
		if (wpr >= wprLevels[0] && wpr < wprLevels[1])
		{
			var part = (wpr - wprLevels[0]) / (wprLevels[1] - wprLevels[0]);
			rang[1, 0] = 1m - part;
			rang[1, 1] = 1m - rang[1, 0];
		}
		if (wpr >= wprLevels[1] && wpr < wprLevels[2])
		rang[1, 1] = 1m;
		if (wpr >= wprLevels[2] && wpr < wprLevels[3])
		{
			var part = (wpr - wprLevels[2]) / (wprLevels[3] - wprLevels[2]);
			rang[1, 1] = 1m - part;
			rang[1, 2] = 1m - rang[1, 1];
		}
		if (wpr >= wprLevels[3] && wpr < wprLevels[4])
		rang[1, 2] = 1m;
		if (wpr >= wprLevels[4] && wpr < wprLevels[5])
		{
			var part = (wpr - wprLevels[4]) / (wprLevels[5] - wprLevels[4]);
			rang[1, 2] = 1m - part;
			rang[1, 3] = 1m - rang[1, 2];
		}
		if (wpr >= wprLevels[5] && wpr < wprLevels[6])
		rang[1, 3] = 1m;
		if (wpr >= wprLevels[6] && wpr < wprLevels[7])
		{
			var part = (wpr - wprLevels[6]) / (wprLevels[7] - wprLevels[6]);
			rang[1, 3] = 1m - part;
			rang[1, 4] = 1m - rang[1, 3];
		}
		if (wpr >= wprLevels[7])
		rang[1, 4] = 1m;

		// 3) Acceleration/Deceleration oscillator sequences.
		var tempAcBuy = 0m;
		if (_acHistory[0] < _acHistory[1] && _acHistory[0] < 0m && _acHistory[1] < 0m)
		tempAcBuy = 2m;
		if (_acHistory[0] < _acHistory[1] && _acHistory[1] < _acHistory[2] &&
		_acHistory[0] < 0m && _acHistory[1] < 0m && _acHistory[2] < 0m)
		tempAcBuy = 3m;
		if (_acHistory[0] < _acHistory[1] && _acHistory[1] < _acHistory[2] &&
		_acHistory[2] < _acHistory[3] && _acHistory[0] < 0m && _acHistory[1] < 0m &&
		_acHistory[2] < 0m && _acHistory[3] < 0m)
		tempAcBuy = 4m;
		if (_acHistory[0] < _acHistory[1] && _acHistory[1] < _acHistory[2] &&
		_acHistory[2] < _acHistory[3] && _acHistory[3] < _acHistory[4] &&
		_acHistory[0] < 0m && _acHistory[1] < 0m && _acHistory[2] < 0m &&
		_acHistory[3] < 0m && _acHistory[4] < 0m)
		tempAcBuy = 5m;

		var tempAcSell = 0m;
		if (_acHistory[0] > _acHistory[1] && _acHistory[0] > 0m && _acHistory[1] > 0m)
		tempAcSell = 2m;
		if (_acHistory[0] > _acHistory[1] && _acHistory[1] > _acHistory[2] &&
		_acHistory[0] > 0m && _acHistory[1] > 0m && _acHistory[2] > 0m)
		tempAcSell = 3m;
		if (_acHistory[0] > _acHistory[1] && _acHistory[1] > _acHistory[2] &&
		_acHistory[2] > _acHistory[3] && _acHistory[0] > 0m && _acHistory[1] > 0m &&
		_acHistory[2] > 0m && _acHistory[3] > 0m)
		tempAcSell = 4m;
		if (_acHistory[0] > _acHistory[1] && _acHistory[1] > _acHistory[2] &&
		_acHistory[2] > _acHistory[3] && _acHistory[3] > _acHistory[4] &&
		_acHistory[0] > 0m && _acHistory[1] > 0m && _acHistory[2] > 0m &&
		_acHistory[3] > 0m && _acHistory[4] > 0m)
		tempAcSell = 5m;

		if (tempAcBuy == acLevels[0] || tempAcBuy == acLevels[1])
		rang[2, 0] = 1m;
		if (tempAcBuy == acLevels[2] || tempAcBuy == acLevels[3])
		rang[2, 1] = 1m;

		if (tempAcSell == acLevels[4] || tempAcSell == acLevels[5])
		rang[2, 3] = 1m;
		if (tempAcSell == acLevels[6] || tempAcSell == acLevels[7])
		rang[2, 4] = 1m;

		if (rang[2, 0] == 0m && rang[2, 1] == 0m && rang[2, 3] == 0m && rang[2, 4] == 0m)
		rang[2, 2] = 1m;

		// 4) DeMarker membership.
		if (deMarker < deMarkerLevels[0])
		rang[3, 0] = 1m;
		if (deMarker >= deMarkerLevels[0] && deMarker < deMarkerLevels[1])
		{
			var part = (deMarker - deMarkerLevels[0]) / (deMarkerLevels[1] - deMarkerLevels[0]);
			rang[3, 0] = 1m - part;
			rang[3, 1] = 1m - rang[3, 0];
		}
		if (deMarker >= deMarkerLevels[1] && deMarker < deMarkerLevels[2])
		rang[3, 1] = 1m;
		if (deMarker >= deMarkerLevels[2] && deMarker < deMarkerLevels[3])
		{
			var part = (deMarker - deMarkerLevels[2]) / (deMarkerLevels[3] - deMarkerLevels[2]);
			rang[3, 1] = 1m - part;
			rang[3, 2] = 1m - rang[3, 1];
		}
		if (deMarker >= deMarkerLevels[3] && deMarker < deMarkerLevels[4])
		rang[3, 2] = 1m;
		if (deMarker >= deMarkerLevels[4] && deMarker < deMarkerLevels[5])
		{
			var part = (deMarker - deMarkerLevels[4]) / (deMarkerLevels[5] - deMarkerLevels[4]);
			rang[3, 2] = 1m - part;
			rang[3, 3] = 1m - rang[3, 2];
		}
		if (deMarker >= deMarkerLevels[5] && deMarker < deMarkerLevels[6])
		rang[3, 3] = 1m;
		if (deMarker >= deMarkerLevels[6] && deMarker < deMarkerLevels[7])
		{
			var part = (deMarker - deMarkerLevels[6]) / (deMarkerLevels[7] - deMarkerLevels[6]);
			rang[3, 3] = 1m - part;
			rang[3, 4] = 1m - rang[3, 3];
		}
		if (deMarker >= deMarkerLevels[7])
		rang[3, 4] = 1m;

		// 5) RSI membership.
		if (rsi < rsiLevels[0])
		rang[4, 0] = 1m;
		if (rsi >= rsiLevels[0] && rsi < rsiLevels[1])
		{
			var part = (rsi - rsiLevels[0]) / (rsiLevels[1] - rsiLevels[0]);
			rang[4, 0] = 1m - part;
			rang[4, 1] = 1m - rang[4, 0];
		}
		if (rsi >= rsiLevels[1] && rsi < rsiLevels[2])
		rang[4, 1] = 1m;
		if (rsi >= rsiLevels[2] && rsi < rsiLevels[3])
		{
			var part = (rsi - rsiLevels[2]) / (rsiLevels[3] - rsiLevels[2]);
			rang[4, 1] = 1m - part;
			rang[4, 2] = 1m - rang[4, 1];
		}
		if (rsi >= rsiLevels[3] && rsi < rsiLevels[4])
		rang[4, 2] = 1m;
		if (rsi >= rsiLevels[4] && rsi < rsiLevels[5])
		{
			var part = (rsi - rsiLevels[4]) / (rsiLevels[5] - rsiLevels[4]);
			rang[4, 2] = 1m - part;
			rang[4, 3] = 1m - rang[4, 2];
		}
		if (rsi >= rsiLevels[5] && rsi < rsiLevels[6])
		rang[4, 3] = 1m;
		if (rsi >= rsiLevels[6] && rsi < rsiLevels[7])
		{
			var part = (rsi - rsiLevels[6]) / (rsiLevels[7] - rsiLevels[6]);
			rang[4, 3] = 1m - part;
			rang[4, 4] = 1m - rang[4, 3];
		}
		if (rsi >= rsiLevels[7])
		rang[4, 4] = 1m;

		for (var x = 0; x < 4; x++)
		{
			for (var y = 0; y < 4; y++)
			summary[x] += rang[y, x] * weights[x];
		}

		var decision = 0m;
		for (var x = 0; x < 4; x++)
		decision += summary[x] * (0.2m * (x + 1) - 0.1m);

		return decision;
	}
}
