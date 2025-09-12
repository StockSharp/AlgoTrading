using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA crossover with ATR-based trailing take profit.
/// </summary>
public class TrailingTakeProfitExampleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _isFirst;

	public TrailingTakeProfitExampleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 14)
			.SetDisplay("Fast SMA", "Fast moving average length.", "Indicators");

		_slowLength = Param(nameof(SlowLength), 28)
			.SetDisplay("Slow SMA", "Slow moving average length.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 7)
			.SetDisplay("ATR Length", "ATR period for trailing exit.", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for trailing exit.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation.", "General");
	}

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing exit.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0m;
		_prevSlow = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastSma = new SimpleMovingAverage { Length = FastLength };
		var slowSma = new SimpleMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var diffPrev = _prevFast - _prevSlow;
		var diffCurr = fast - slow;

		if (!_isFirst)
		{
			var longCondition = diffPrev <= 0m && diffCurr > 0m;
			var shortCondition = diffPrev >= 0m && diffCurr < 0m;

			if (longCondition && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (shortCondition && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}

		var atrMultiplied = AtrMultiplier * atr;

		if (Position > 0)
		{
			var target = _prevHigh + atrMultiplied;
			if (candle.HighPrice >= target)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var target = _prevLow - atrMultiplied;
			if (candle.LowPrice <= target)
				BuyMarket(Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_isFirst = false;
	}
}
