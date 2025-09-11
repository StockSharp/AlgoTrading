using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Random state machine strategy with moving average filter.
/// </summary>
public class RandomStateMachineStrategy : Strategy
{
	private readonly StrategyParam<int> _stateResetInterval;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<bool> _enableLongs;
	private readonly StrategyParam<bool> _enableShorts;
	private readonly StrategyParam<bool> _useTpSlLong;
	private readonly StrategyParam<bool> _useTpSlShort;
	private readonly StrategyParam<bool> _useTimedLong;
	private readonly StrategyParam<bool> _useTimedShort;
	private readonly StrategyParam<bool> _useMaCrossLong;
	private readonly StrategyParam<bool> _useMaCrossShort;
	private readonly StrategyParam<decimal> _rrRatioLong;
	private readonly StrategyParam<decimal> _rrRatioShort;
	private readonly StrategyParam<decimal> _riskPtsLong;
	private readonly StrategyParam<decimal> _riskPtsShort;
	private readonly StrategyParam<int> _barsHoldLong;
	private readonly StrategyParam<int> _barsHoldShort;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _maIndicator = null!;
	private int _barsSinceReset;
	private int _transitions;
	private int _barIndex;
	private int? _entryBar;
	private decimal _entryPrice;
	private decimal _tpPrice;
	private decimal _slPrice;
	private decimal _prevClose;
	private decimal _prevMa;
	private readonly Random _random = new();

	/// <summary>
	/// State reset interval in bars.
	/// </summary>
	public int StateResetInterval { get => _stateResetInterval.Value; set => _stateResetInterval.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaType MaTypeIndicator { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLongs { get => _enableLongs.Value; set => _enableLongs.Value = value; }

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShorts { get => _enableShorts.Value; set => _enableShorts.Value = value; }

	/// <summary>
	/// Use take-profit and stop-loss for long trades.
	/// </summary>
	public bool UseTpSlLong { get => _useTpSlLong.Value; set => _useTpSlLong.Value = value; }

	/// <summary>
	/// Use take-profit and stop-loss for short trades.
	/// </summary>
	public bool UseTpSlShort { get => _useTpSlShort.Value; set => _useTpSlShort.Value = value; }

	/// <summary>
	/// Use timed exit for long trades.
	/// </summary>
	public bool UseTimedLong { get => _useTimedLong.Value; set => _useTimedLong.Value = value; }

	/// <summary>
	/// Use timed exit for short trades.
	/// </summary>
	public bool UseTimedShort { get => _useTimedShort.Value; set => _useTimedShort.Value = value; }

	/// <summary>
	/// Close long position on MA cross.
	/// </summary>
	public bool UseMaCrossLong { get => _useMaCrossLong.Value; set => _useMaCrossLong.Value = value; }

	/// <summary>
	/// Close short position on MA cross.
	/// </summary>
	public bool UseMaCrossShort { get => _useMaCrossShort.Value; set => _useMaCrossShort.Value = value; }

	/// <summary>
	/// Risk/reward ratio for long trades.
	/// </summary>
	public decimal RrRatioLong { get => _rrRatioLong.Value; set => _rrRatioLong.Value = value; }

	/// <summary>
	/// Risk/reward ratio for short trades.
	/// </summary>
	public decimal RrRatioShort { get => _rrRatioShort.Value; set => _rrRatioShort.Value = value; }

	/// <summary>
	/// Risk per long trade in points.
	/// </summary>
	public decimal RiskPtsLong { get => _riskPtsLong.Value; set => _riskPtsLong.Value = value; }

	/// <summary>
	/// Risk per short trade in points.
	/// </summary>
	public decimal RiskPtsShort { get => _riskPtsShort.Value; set => _riskPtsShort.Value = value; }

	/// <summary>
	/// Bars to hold long position.
	/// </summary>
	public int BarsHoldLong { get => _barsHoldLong.Value; set => _barsHoldLong.Value = value; }

	/// <summary>
	/// Bars to hold short position.
	/// </summary>
	public int BarsHoldShort { get => _barsHoldShort.Value; set => _barsHoldShort.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public RandomStateMachineStrategy()
	{
		_stateResetInterval = Param(nameof(StateResetInterval), 100)
			.SetDisplay("State Reset", "Reset interval in bars", "General")
			.SetGreaterThanZero();

		_maLength = Param(nameof(MaLength), 14)
			.SetDisplay("MA Length", "Moving average length", "Indicators")
			.SetGreaterThanZero();

		_maType = Param(nameof(MaTypeIndicator), MaType.Ema)
			.SetDisplay("MA Type", "Moving average type", "Indicators");

		_enableLongs = Param(nameof(EnableLongs), true)
			.SetDisplay("Enable Longs", "Allow long trades", "Signals");

		_enableShorts = Param(nameof(EnableShorts), true)
			.SetDisplay("Enable Shorts", "Allow short trades", "Signals");

		_useTpSlLong = Param(nameof(UseTpSlLong), true)
			.SetDisplay("Use TP/SL Long", "Enable TP/SL for long", "Risk");

		_useTpSlShort = Param(nameof(UseTpSlShort), true)
			.SetDisplay("Use TP/SL Short", "Enable TP/SL for short", "Risk");

		_useTimedLong = Param(nameof(UseTimedLong), true)
			.SetDisplay("Timed Exit Long", "Use timed exit for long", "Risk");

		_useTimedShort = Param(nameof(UseTimedShort), true)
			.SetDisplay("Timed Exit Short", "Use timed exit for short", "Risk");

		_useMaCrossLong = Param(nameof(UseMaCrossLong), true)
			.SetDisplay("MA Cross Exit Long", "Close long on MA cross", "Risk");

		_useMaCrossShort = Param(nameof(UseMaCrossShort), true)
			.SetDisplay("MA Cross Exit Short", "Close short on MA cross", "Risk");

		_rrRatioLong = Param(nameof(RrRatioLong), 2m)
			.SetDisplay("R/R Long", "Risk/reward ratio for long", "Risk")
			.SetGreaterThanZero();

		_rrRatioShort = Param(nameof(RrRatioShort), 2m)
			.SetDisplay("R/R Short", "Risk/reward ratio for short", "Risk")
			.SetGreaterThanZero();

		_riskPtsLong = Param(nameof(RiskPtsLong), 1m)
			.SetDisplay("Risk Long", "Risk per long trade", "Risk")
			.SetGreaterThanZero();

		_riskPtsShort = Param(nameof(RiskPtsShort), 1m)
			.SetDisplay("Risk Short", "Risk per short trade", "Risk")
			.SetGreaterThanZero();

		_barsHoldLong = Param(nameof(BarsHoldLong), 10)
			.SetDisplay("Hold Bars Long", "Bars to hold long", "Risk")
			.SetGreaterThanZero();

		_barsHoldShort = Param(nameof(BarsHoldShort), 10)
			.SetDisplay("Hold Bars Short", "Bars to hold short", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of working candles", "General");
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
		_barsSinceReset = 0;
		_transitions = 0;
		_barIndex = 0;
		_entryBar = null;
		_entryPrice = 0m;
		_tpPrice = 0m;
		_slPrice = 0m;
		_prevClose = 0m;
		_prevMa = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_maIndicator = CreateMa(MaTypeIndicator, MaLength);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_maIndicator, ProcessCandle).Start();
	}

	private MovingAverage CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.Sma => new SimpleMovingAverage { Length = length },
			MaType.Ema => new ExponentialMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_maIndicator.IsFormed)
			return;

		_barIndex++;
		_barsSinceReset++;

		var stateChanged = false;
		if (_random.NextDouble() > 0.95)
		{
			stateChanged = true;
			_transitions++;
		}

		if (_barsSinceReset >= StateResetInterval)
		{
			if (_transitions >= 2)
				_transitions = 0;
			_barsSinceReset = 0;
		}

		var close = candle.ClosePrice;

		if (stateChanged)
		{
			if (EnableLongs && close > maValue && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_entryBar = _barIndex;
				_entryPrice = close;
				if (UseTpSlLong)
				{
					_tpPrice = _entryPrice + RiskPtsLong * RrRatioLong;
					_slPrice = _entryPrice - RiskPtsLong;
				}
			}
			else if (EnableShorts && close < maValue && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_entryBar = _barIndex;
				_entryPrice = close;
				if (UseTpSlShort)
				{
					_tpPrice = _entryPrice - RiskPtsShort * RrRatioShort;
					_slPrice = _entryPrice + RiskPtsShort;
				}
			}
		}

		if (Position > 0)
		{
			if (UseTpSlLong)
			{
				if (candle.HighPrice >= _tpPrice)
					SellMarket(Position);
				else if (candle.LowPrice <= _slPrice)
					SellMarket(Position);
			}

			if (UseTimedLong && _entryBar.HasValue && _barIndex - _entryBar.Value >= BarsHoldLong)
				SellMarket(Position);

			if (UseMaCrossLong && _prevClose >= _prevMa && close < maValue)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (UseTpSlShort)
			{
				if (candle.LowPrice <= _tpPrice)
					BuyMarket(Math.Abs(Position));
				else if (candle.HighPrice >= _slPrice)
					BuyMarket(Math.Abs(Position));
			}

			if (UseTimedShort && _entryBar.HasValue && _barIndex - _entryBar.Value >= BarsHoldShort)
				BuyMarket(Math.Abs(Position));

			if (UseMaCrossShort && _prevClose <= _prevMa && close > maValue)
				BuyMarket(Math.Abs(Position));
		}

		_prevClose = close;
		_prevMa = maValue;
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaType
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema
	}
}
