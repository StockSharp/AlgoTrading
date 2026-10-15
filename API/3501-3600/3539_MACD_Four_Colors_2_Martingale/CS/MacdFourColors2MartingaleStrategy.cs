using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD histogram crossover with martingale volume scaling.
/// Based on the MACD Four Colors 2 Martingale expert advisor.
/// </summary>
public class MacdFourColors2MartingaleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<int> _maxMartingale;

	private MovingAverageConvergenceDivergence _macd;
	private readonly System.Collections.Generic.Queue<decimal> _macdHistory = new();
	private decimal? _prevHistogram;
	private decimal _currentVolume;
	private int _consecutiveLosses;
	private decimal _entryPrice;

	/// <summary>
	/// Type of candles used for MACD analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the fast EMA inside MACD.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow EMA inside MACD.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the signal line.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied after a losing trade.
	/// </summary>
	public decimal LotCoefficient
	{
		get => _lotCoefficient.Value;
		set => _lotCoefficient.Value = value;
	}

	/// <summary>
	/// Maximum number of martingale steps before resetting.
	/// </summary>
	public int MaxMartingale
	{
		get => _maxMartingale.Value;
		set => _maxMartingale.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public MacdFourColors2MartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for MACD analysis", "General");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 20)
			.SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
			.SetGreaterThanZero();

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 50)
			.SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators")
			.SetGreaterThanZero();

		_signalPeriod = Param(nameof(SignalPeriod), 12)
			.SetDisplay("Signal Period", "Signal line smoothing period", "Indicators")
			.SetGreaterThanZero();

		_lotCoefficient = Param(nameof(LotCoefficient), 1.5m)
			.SetDisplay("Lot Coefficient", "Multiplier after a loss", "Money Management")
			.SetGreaterThanZero();

		_maxMartingale = Param(nameof(MaxMartingale), 5)
			.SetDisplay("Max Martingale", "Maximum consecutive doublings", "Money Management")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHistogram = null;
		_currentVolume = Volume > 0 ? Volume : 1;
		_consecutiveLosses = 0;
		_entryPrice = 0;
		_macdHistory.Clear();

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = FastEmaPeriod },
			LongMa = { Length = SlowEmaPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macdHistory.Enqueue(macdValue);
		while (_macdHistory.Count > SignalPeriod)
			_macdHistory.Dequeue();

		if (!_macd.IsFormed || _macdHistory.Count < SignalPeriod)
		{
			_prevHistogram = null;
			return;
		}

		// Calculate signal line (SMA of MACD)
		var sum = 0m;
		var history = _macdHistory.ToArray();
		foreach (var v in history)
			sum += v;
		var signalValue = sum / history.Length;

		var histogram = macdValue - signalValue;

		if (_prevHistogram is null)
		{
			_prevHistogram = histogram;
			return;
		}

		var crossUp = _prevHistogram < 0 && histogram >= 0;
		var crossDown = _prevHistogram >= 0 && histogram < 0;

		if (crossUp)
		{
			// Check for loss on closing short
			if (Position < 0)
			{
				var pnl = _entryPrice - candle.ClosePrice;
				if (pnl < 0)
				{
					_consecutiveLosses++;
					if (_consecutiveLosses <= MaxMartingale)
						_currentVolume *= LotCoefficient;
				}
				else
				{
					_consecutiveLosses = 0;
					_currentVolume = Volume > 0 ? Volume : 1;
				}

				BuyMarket(Math.Abs(Position) + _currentVolume);
				_entryPrice = candle.ClosePrice;
			}
			else if (Position == 0)
			{
				BuyMarket(_currentVolume);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (crossDown)
		{
			// Check for loss on closing long
			if (Position > 0)
			{
				var pnl = candle.ClosePrice - _entryPrice;
				if (pnl < 0)
				{
					_consecutiveLosses++;
					if (_consecutiveLosses <= MaxMartingale)
						_currentVolume *= LotCoefficient;
				}
				else
				{
					_consecutiveLosses = 0;
					_currentVolume = Volume > 0 ? Volume : 1;
				}

				SellMarket(Math.Abs(Position) + _currentVolume);
				_entryPrice = candle.ClosePrice;
			}
			else if (Position == 0)
			{
				SellMarket(_currentVolume);
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevHistogram = histogram;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_macd = null;
		_prevHistogram = null;
		_currentVolume = 0;
		_consecutiveLosses = 0;
		_entryPrice = 0;
		_macdHistory.Clear();

		base.OnReseted();
	}
}
