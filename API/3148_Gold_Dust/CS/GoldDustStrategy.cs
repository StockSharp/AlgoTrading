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
/// Translation of the "Gold Dust" expert advisor that evaluates two perceptrons built on a weighted moving average.
/// Generates sell orders when the perceptron output is positive and buy orders when it is negative.
/// Implements stop-loss management and a candle-based trailing stop expressed in adjusted pips.
/// </summary>
public class GoldDustStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _passMode;
	private readonly StrategyParam<int> _x11;
	private readonly StrategyParam<int> _x21;
	private readonly StrategyParam<int> _x31;
	private readonly StrategyParam<int> _x41;
	private readonly StrategyParam<int> _x12;
	private readonly StrategyParam<int> _x22;
	private readonly StrategyParam<int> _x32;
	private readonly StrategyParam<int> _x42;

	private readonly List<decimal> _openHistory = new();
	private readonly List<decimal> _closeHistory = new();
	private readonly List<decimal> _maHistory = new();

	private decimal _pipValue;
	private decimal _stopLossOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;
	private int _requiredHistory;

	private decimal _lastPosition;
	private decimal _longStop;
	private decimal _shortStop;
	private bool _hasLongStop;
	private bool _hasShortStop;

	/// <summary>
	/// Volume used when opening a fresh position.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance in adjusted pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Distance maintained by the trailing stop in adjusted pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Extra favourable move required before the trailing stop advances.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Length of the weighted moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle series used for calculations and order timing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Perceptron mode: 1 uses the first weight set, 2 uses the second, 3 requires consensus.
	/// </summary>
	public int PassMode
	{
		get => _passMode.Value;
		set => _passMode.Value = value;
	}

	/// <summary>
	/// First weight of perceptron #1 (raw value before 100 offset is removed).
	/// </summary>
	public int X11
	{
		get => _x11.Value;
		set => _x11.Value = value;
	}

	/// <summary>
	/// Second weight of perceptron #1 (raw value before 100 offset is removed).
	/// </summary>
	public int X21
	{
		get => _x21.Value;
		set => _x21.Value = value;
	}

	/// <summary>
	/// Third weight of perceptron #1 (raw value before 100 offset is removed).
	/// </summary>
	public int X31
	{
		get => _x31.Value;
		set => _x31.Value = value;
	}

	/// <summary>
	/// Fourth weight of perceptron #1 (raw value before 100 offset is removed).
	/// </summary>
	public int X41
	{
		get => _x41.Value;
		set => _x41.Value = value;
	}

	/// <summary>
	/// First weight of perceptron #2 (raw value before 100 offset is removed).
	/// </summary>
	public int X12
	{
		get => _x12.Value;
		set => _x12.Value = value;
	}

	/// <summary>
	/// Second weight of perceptron #2 (raw value before 100 offset is removed).
	/// </summary>
	public int X22
	{
		get => _x22.Value;
		set => _x22.Value = value;
	}

	/// <summary>
	/// Third weight of perceptron #2 (raw value before 100 offset is removed).
	/// </summary>
	public int X32
	{
		get => _x32.Value;
		set => _x32.Value = value;
	}

	/// <summary>
	/// Fourth weight of perceptron #2 (raw value before 100 offset is removed).
	/// </summary>
	public int X42
	{
		get => _x42.Value;
		set => _x42.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters with defaults taken from the original expert advisor.
	/// </summary>
	public GoldDustStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 150m)
			.SetDisplay("Stop Loss (pips)", "Initial stop distance in adjusted pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 25m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in adjusted pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Extra pips required before the trailing stop moves", "Risk");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Weighted moving average length", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		_passMode = Param(nameof(PassMode), 1)
			.SetRange(1, 3)
			.SetDisplay("Pass Mode", "1=Perceptron 1, 2=Perceptron 2, 3=Consensus", "Logic");

		_x11 = Param(nameof(X11), 100)
			.SetDisplay("X11", "Weight 1 for perceptron #1", "Perceptron #1");

		_x21 = Param(nameof(X21), 100)
			.SetDisplay("X21", "Weight 2 for perceptron #1", "Perceptron #1");

		_x31 = Param(nameof(X31), 100)
			.SetDisplay("X31", "Weight 3 for perceptron #1", "Perceptron #1");

		_x41 = Param(nameof(X41), 100)
			.SetDisplay("X41", "Weight 4 for perceptron #1", "Perceptron #1");

		_x12 = Param(nameof(X12), 100)
			.SetDisplay("X12", "Weight 1 for perceptron #2", "Perceptron #2");

		_x22 = Param(nameof(X22), 100)
			.SetDisplay("X22", "Weight 2 for perceptron #2", "Perceptron #2");

		_x32 = Param(nameof(X32), 100)
			.SetDisplay("X32", "Weight 3 for perceptron #2", "Perceptron #2");

		_x42 = Param(nameof(X42), 100)
			.SetDisplay("X42", "Weight 4 for perceptron #2", "Perceptron #2");

		_requiredHistory = Math.Max(1, MaPeriod * 3 + 1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openHistory.Clear();
		_closeHistory.Clear();
		_maHistory.Clear();

		_pipValue = 0m;
		_stopLossOffset = 0m;
		_trailingStopOffset = 0m;
		_trailingStepOffset = 0m;
		_requiredHistory = Math.Max(1, MaPeriod * 3 + 1);

		_lastPosition = 0m;
		_longStop = 0m;
		_shortStop = 0m;
		_hasLongStop = false;
		_hasShortStop = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateOffsets();
		_requiredHistory = Math.Max(1, MaPeriod * 3 + 1);

		var ma = new WeightedMovingAverage
		{
			Length = MaPeriod,
			CandlePrice = CandlePrice.Weighted
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, (candle, maValue) => ProcessCandle(candle, ma, maValue))
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, WeightedMovingAverage ma, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		AppendHistory(candle, maValue);
		UpdateOffsets();
		UpdatePositionState();
		UpdateTrailingStops(candle);

		if (CheckStopLoss(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!ma.IsFormed)
			return;

		var signal = CalculateSupervisorSignal();

		if (signal < 0)
		{
			EnterLong();
		}
		else if (signal > 0)
		{
			EnterShort();
		}
		else
		{
			CloseProfitablePositions(candle);
		}
	}

	private void AppendHistory(ICandleMessage candle, decimal maValue)
	{
		_openHistory.Add(candle.OpenPrice);
		_closeHistory.Add(candle.ClosePrice);
		_maHistory.Add(maValue);

		var required = Math.Max(1, MaPeriod * 3 + 1);
		_requiredHistory = required;

		while (_openHistory.Count > required)
		{
			_openHistory.RemoveAt(0);
			_closeHistory.RemoveAt(0);
			_maHistory.RemoveAt(0);
		}
	}

	private void UpdateOffsets()
	{
		_pipValue = CalculatePipValue();
		_stopLossOffset = StopLossPips * _pipValue;
		_trailingStopOffset = TrailingStopPips * _pipValue;
		_trailingStepOffset = TrailingStepPips * _pipValue;
	}

	private void UpdatePositionState()
	{
		if (Position > 0m)
		{
			if (_lastPosition <= 0m)
			{
				_hasShortStop = false;
				_longStop = PositionPrice - _stopLossOffset;
				_hasLongStop = _stopLossOffset > 0m;
			}
		}
		else if (Position < 0m)
		{
			if (_lastPosition >= 0m)
			{
				_hasLongStop = false;
				_shortStop = PositionPrice + _stopLossOffset;
				_hasShortStop = _stopLossOffset > 0m;
			}
		}
		else
		{
			_hasLongStop = false;
			_hasShortStop = false;
		}

		_lastPosition = Position;
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m)
			return;

		if (Position > 0m)
		{
			var profit = candle.ClosePrice - PositionPrice;
			if (profit > _trailingStopOffset + _trailingStepOffset)
			{
				var candidate = candle.ClosePrice - _trailingStopOffset;
				if (!_hasLongStop || candidate > _longStop)
				{
					_longStop = candidate;
					_hasLongStop = true;
				}
			}
		}
		else if (Position < 0m)
		{
			var profit = PositionPrice - candle.ClosePrice;
			if (profit > _trailingStopOffset + _trailingStepOffset)
			{
				var candidate = candle.ClosePrice + _trailingStopOffset;
				if (!_hasShortStop || candidate < _shortStop)
				{
					_shortStop = candidate;
					_hasShortStop = true;
				}
			}
		}
	}

	private bool CheckStopLoss(ICandleMessage candle)
	{
		if (Position > 0m && _hasLongStop)
		{
			if (candle.LowPrice <= _longStop)
			{
				SellMarket(Position);
				_hasLongStop = false;
				return true;
			}
		}
		else if (Position < 0m && _hasShortStop)
		{
			if (candle.HighPrice >= _shortStop)
			{
				BuyMarket(-Position);
				_hasShortStop = false;
				return true;
			}
		}

		return false;
	}

	private void EnterLong()
	{
		if (Position > 0m)
			return;

		var volume = TradeVolume + Math.Max(0m, -Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
	}

	private void EnterShort()
	{
		if (Position < 0m)
			return;

		var volume = TradeVolume + Math.Max(0m, Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
	}

	private void CloseProfitablePositions(ICandleMessage candle)
	{
		if (Position == 0m)
			return;

		var openProfit = Position * (candle.ClosePrice - PositionPrice);
		if (openProfit <= 0m)
			return;

		if (Position > 0m)
		{
			SellMarket(Position);
			_hasLongStop = false;
		}
		else
		{
			BuyMarket(-Position);
			_hasShortStop = false;
		}
	}

	private int CalculateSupervisorSignal()
	{
		var period = Math.Max(1, MaPeriod);
		if (_maHistory.Count <= period * 3)
			return 0;

		var result1 = EvaluatePerceptron(X11, X21, X31, X41);
		var result2 = EvaluatePerceptron(X12, X22, X32, X42);

		return PassMode switch
		{
			1 => result1,
			2 => result2,
			3 => result1 != 0 && result1 == result2 ? result1 : 0,
			_ => 0,
		};
	}

	private int EvaluatePerceptron(int x1, int x2, int x3, int x4)
	{
		var period = Math.Max(1, MaPeriod);
		if (_maHistory.Count <= period * 3)
			return 0;

		var lastIndex = _maHistory.Count - 1;
		var index1 = lastIndex - period;
		var index2 = lastIndex - period * 2;
		var index3 = lastIndex - period * 3;

		if (index3 < 0)
			return 0;

		var w1 = x1 - 100m;
		var w2 = x2 - 100m;
		var w3 = x3 - 100m;
		var w4 = x4 - 100m;

		var a1 = _closeHistory[lastIndex] - _maHistory[lastIndex];
		var a2 = _openHistory[index1] - _maHistory[index1];
		var a3 = _openHistory[index2] - _maHistory[index2];
		var a4 = _openHistory[index3] - _maHistory[index3];

		var result = w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;

		if (result > 0m)
			return 1;
		if (result < 0m)
			return -1;
		return 0;
	}

	private decimal CalculatePipValue()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var scaled = step;
		var digits = 0;
		while (scaled < 1m && digits < 10)
		{
			scaled *= 10m;
			digits++;
		}

		var adjust = (digits == 3 || digits == 5) ? 10m : 1m;
		return step * adjust;
	}
}

