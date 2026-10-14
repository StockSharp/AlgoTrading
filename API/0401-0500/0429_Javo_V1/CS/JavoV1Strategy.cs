using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Javo v1 strategy.
/// Builds Heikin Ashi candles and runs a fast and a slow EMA on the Heikin Ashi close. Goes long when the Heikin Ashi
/// candle is bullish and the fast EMA is above the slow one, short when the candle is bearish and the fast EMA is below
/// the slow one. The opposite signal reverses the position.
/// </summary>
public class JavoV1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private decimal? _prevHaOpen;
	private decimal? _prevHaClose;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public JavoV1Strategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period on the Heikin Ashi close", "Moving Averages");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period on the Heikin Ashi close", "Moving Averages");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastEma = null;
		_slowEma = null;
		_prevHaOpen = null;
		_prevHaClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHaOpen = null;
		_prevHaClose = null;
		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _prevHaOpen is decimal prevOpen && _prevHaClose is decimal prevClose
			? (prevOpen + prevClose) / 2m
			: (candle.OpenPrice + candle.ClosePrice) / 2m;

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;

		var fast = _fastEma.Process(haClose, candle.ServerTime, true).ToDecimal();
		var slow = _slowEma.Process(haClose, candle.ServerTime, true).ToDecimal();

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (haClose > haOpen && fast > slow && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (haClose < haOpen && fast < slow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
