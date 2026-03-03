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
/// Long Term Profitable Swing strategy based on EMA crossover with RSI filter and ATR exits.
/// </summary>
public class LongTermProfitableSwingAbbasStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<decimal> _atrStopMult;
	private readonly StrategyParam<decimal> _atrTpMult;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;

	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// ATR calculation length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// RSI bullish threshold.
	/// </summary>
	public decimal RsiThreshold
	{
		get => _rsiThreshold.Value;
		set => _rsiThreshold.Value = value;
	}

	/// <summary>
	/// ATR stop loss multiplier.
	/// </summary>
	public decimal AtrStopMult
	{
		get => _atrStopMult.Value;
		set => _atrStopMult.Value = value;
	}

	/// <summary>
	/// ATR take profit multiplier.
	/// </summary>
	public decimal AtrTpMult
	{
		get => _atrTpMult.Value;
		set => _atrTpMult.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LongTermProfitableSwingAbbasStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 16)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
			
			.SetOptimize(5, 50, 1);

		_slowEmaLength = Param(nameof(SlowEmaLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
			.SetOptimize(20, 100, 5);

		_rsiLength = Param(nameof(RsiLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Indicators")
			
			.SetOptimize(5, 30, 1);

		_atrLength = Param(nameof(AtrLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation length", "Indicators")
			
			.SetOptimize(5, 40, 1);

		_rsiThreshold = Param(nameof(RsiThreshold), 50m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Threshold", "RSI bullish threshold", "Indicators")
			
			.SetOptimize(40m, 60m, 1m);

		_atrStopMult = Param(nameof(AtrStopMult), 15m)
			.SetRange(0.1m, 30m)
			.SetDisplay("ATR Stop Mult", "ATR stop loss multiplier", "Risk")
			.SetOptimize(1m, 15m, 0.5m);

		_atrTpMult = Param(nameof(AtrTpMult), 20m)
			.SetRange(0.1m, 30m)
			.SetDisplay("ATR TP Mult", "ATR take profit multiplier", "Risk")
			.SetOptimize(1m, 20m, 0.5m);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastEma = null;
		_slowEma = null;
		_rsi = null;
		_atr = null;
		_prevFast = 0;
		_prevSlow = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _rsi, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_rsi.IsFormed || !_atr.IsFormed)
			return;

		if (_prevFast == 0 || _prevSlow == 0)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		_prevFast = fast;
		_prevSlow = slow;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
