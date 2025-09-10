using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA Pullback with ATR Exits Strategy.
/// Buys on pullbacks in uptrend and sells on pullbacks in downtrend.
/// Exits are based on ATR multiples.
/// </summary>
public class SmaPullbackAtrExitsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;
	private AverageTrueRange _atr;

	private decimal _entryPrice;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastSmaLength
	{
		get => _fastSmaLength.Value;
		set => _fastSmaLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowSmaLength
	{
		get => _slowSmaLength.Value;
		set => _slowSmaLength.Value = value;
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
	/// ATR stop-loss multiplier.
	/// </summary>
	public decimal AtrMultiplierSl
	{
		get => _atrMultiplierSl.Value;
		set => _atrMultiplierSl.Value = value;
	}

	/// <summary>
	/// ATR take-profit multiplier.
	/// </summary>
	public decimal AtrMultiplierTp
	{
		get => _atrMultiplierTp.Value;
		set => _atrMultiplierTp.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public SmaPullbackAtrExitsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastSmaLength = Param(nameof(FastSmaLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_slowSmaLength = Param(nameof(SlowSmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 5);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 2);

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1.2m)
			.SetRange(0.1m, 10m)
			.SetDisplay("ATR SL Mult", "ATR multiplier for stop-loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.1m);

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 2.0m)
			.SetRange(0.1m, 10m)
			.SetDisplay("ATR TP Mult", "ATR multiplier for take-profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);
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
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage { Length = FastSmaLength };
		_slowSma = new SimpleMovingAverage { Length = SlowSmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _slowSma, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastSmaValue, decimal slowSmaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastSma.IsFormed || !_slowSma.IsFormed || !_atr.IsFormed)
			return;

		var currentPrice = candle.ClosePrice;

		if (Position == 0)
		{
			if (currentPrice < fastSmaValue && fastSmaValue > slowSmaValue)
			{
				RegisterBuy();
				_entryPrice = currentPrice;
			}
			else if (currentPrice > fastSmaValue && fastSmaValue < slowSmaValue)
			{
				RegisterSell();
				_entryPrice = currentPrice;
			}
		}
		else if (Position > 0)
		{
			var stop = _entryPrice - atrValue * AtrMultiplierSl;
			var target = _entryPrice + atrValue * AtrMultiplierTp;

			if (candle.LowPrice <= stop || candle.HighPrice >= target)
			{
				RegisterSell(Position);
				_entryPrice = default;
			}
		}
		else if (Position < 0)
		{
			var stop = _entryPrice + atrValue * AtrMultiplierSl;
			var target = _entryPrice - atrValue * AtrMultiplierTp;

			if (candle.HighPrice >= stop || candle.LowPrice <= target)
			{
				RegisterBuy(Math.Abs(Position));
				_entryPrice = default;
			}
		}
	}
}
