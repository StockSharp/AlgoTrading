using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI and ATR level crossover with dynamic thresholds.
/// The thresholds adapt to recent extremes and volatility to detect reversals.
/// </summary>
public class RsiAndAtrTrendReversalSlTpStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiMultiplier;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _minDifference;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _sinceCrossHigh;
	private decimal _sinceCrossLow;
	private int _direction;
	private decimal _prevHClose;
	private decimal _prevThresh;
	private bool _isFirst;
	private readonly Queue<bool> _buyQueue = new();
	private readonly Queue<bool> _sellQueue = new();

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Multiplier for RSI/ATR adjustment.
	/// </summary>
	public decimal RsiMultiplier
	{
		get => _rsiMultiplier.Value;
		set => _rsiMultiplier.Value = value;
	}

	/// <summary>
	/// Signal delay in bars.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Minimum difference parameter.
	/// </summary>
	public decimal MinDifference
	{
		get => _minDifference.Value;
		set => _minDifference.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public RsiAndAtrTrendReversalSlTpStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 8)
			.SetDisplay("RSI Length", "RSI calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_rsiMultiplier = Param(nameof(RsiMultiplier), 1.5m)
			.SetDisplay("RSI Multiplier", "Multiplier for RSI/ATR adjustment", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.25m);

		_lookback = Param(nameof(Lookback), 1)
			.SetDisplay("Lookback", "Signal delay in bars", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_minDifference = Param(nameof(MinDifference), 10m)
			.SetDisplay("Min Difference", "Minimum difference percentage", "General")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_sinceCrossHigh = 0;
		_sinceCrossLow = 0;
		_direction = 1;
		_prevHClose = 0;
		_prevThresh = 0;
		_isFirst = true;
		_buyQueue.Clear();
		_sellQueue.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var sl = (100m - MinDifference) / 100m;
		var tp = (100m + MinDifference) / 100m;
		var atrRel = atrValue / hClose;
		var rsilower = rsiValue;
		var rsiupper = Math.Abs(rsiValue - 100m);

		if (_isFirst)
		{
			_sinceCrossHigh = hClose;
			_sinceCrossLow = hClose;
			_prevHClose = hClose;
			_prevThresh = hClose;
			_isFirst = false;
			return;
		}

		var lowerBase = _sinceCrossHigh * (1 - (atrRel + (1 / rsilower * RsiMultiplier)));
		var lower = Math.Max(lowerBase, hClose * sl);
		var upperBase = _sinceCrossLow * (1 + (atrRel + (1 / rsiupper * RsiMultiplier)));
		var upper = Math.Min(upperBase, hClose * tp);

		var thresh = _direction == 1 ? lower : upper;

		var crossUp = _prevHClose <= _prevThresh && hClose > thresh;
		var crossDown = _prevHClose >= _prevThresh && hClose < thresh;

		if (crossUp)
		{
			_direction = 1;
			_sinceCrossHigh = hClose;
			_sinceCrossLow = hClose;
		}
		else if (crossDown)
		{
			_direction = -1;
			_sinceCrossHigh = hClose;
			_sinceCrossLow = hClose;
		}
		else
		{
			_sinceCrossHigh = Math.Max(_sinceCrossHigh, hClose);
			_sinceCrossLow = Math.Min(_sinceCrossLow, hClose);
		}

		_buyQueue.Enqueue(crossUp);
		_sellQueue.Enqueue(crossDown);

		_prevHClose = hClose;
		_prevThresh = thresh;

		if (_buyQueue.Count > Lookback)
		{
			var buySignal = _buyQueue.Dequeue();
			var sellSignal = _sellQueue.Dequeue();

			if (buySignal && Position <= 0)
				BuyMarket();
			else if (sellSignal && Position >= 0)
				SellMarket();
		}
	}
}

