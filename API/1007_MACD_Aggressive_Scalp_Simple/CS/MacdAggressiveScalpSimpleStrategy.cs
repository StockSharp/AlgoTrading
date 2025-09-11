using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Aggressive Scalp Simple strategy.
/// Enters positions on MACD histogram crossovers filtered by EMA.
/// Exits positions when histogram momentum reverses.
/// </summary>
public class MacdAggressiveScalpSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private bool _hasPrevHist;
	private decimal _prevHist;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// EMA period used as trend filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Type of candles for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MacdAggressiveScalpSimpleStrategy"/>.
	/// </summary>
	public MacdAggressiveScalpSimpleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal EMA period", "MACD");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for trend filter", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_hasPrevHist = false;
		_prevHist = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastLength,
			LongPeriod = SlowLength,
			SignalPeriod = SignalLength
		};

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, ema, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal macdSignal, decimal macdHist, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevHist)
		{
			_prevHist = macdHist;
			_hasPrevHist = true;
			return;
		}

		var crossUp = _prevHist <= 0 && macdHist > 0;
		var crossDown = _prevHist >= 0 && macdHist < 0;

		if (crossUp && candle.ClosePrice >= ema && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && candle.ClosePrice <= ema && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (_prevHist > macdHist && Position > 0)
			SellMarket(Position);
		else if (_prevHist < macdHist && Position < 0)
			BuyMarket(Math.Abs(Position));

		_prevHist = macdHist;
	}
}

