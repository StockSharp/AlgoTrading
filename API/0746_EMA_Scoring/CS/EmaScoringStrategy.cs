using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA scoring strategy.
/// Buys when the score crosses above threshold and sells when it crosses below.
/// </summary>
public class EmaScoringStrategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _mediumEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;
	private readonly StrategyParam<int> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevScore;

	/// <summary>
	/// Period for short EMA.
	/// </summary>
	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for medium EMA.
	/// </summary>
	public int MediumEmaPeriod
	{
		get => _mediumEmaPeriod.Value;
		set => _mediumEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for long EMA.
	/// </summary>
	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	/// <summary>
	/// Score threshold for signals.
	/// </summary>
	public int Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EmaScoringStrategy"/>.
	/// </summary>
	public EmaScoringStrategy()
	{
		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA Period", "Short EMA period", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_mediumEmaPeriod = Param(nameof(MediumEmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA Period", "Medium EMA period", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(30, 100, 5);

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA Period", "Long EMA period", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_threshold = Param(nameof(Threshold), 4)
			.SetDisplay("Score Threshold", "Score threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 6, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevScore = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var emaShort = new EMA { Length = ShortEmaPeriod };
		var emaMedium = new EMA { Length = MediumEmaPeriod };
		var emaLong = new EMA { Length = LongEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaMedium, emaLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaMedium);
			DrawIndicator(area, emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShort, decimal emaMedium, decimal emaLong)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var score = 0;
		score += candle.ClosePrice > emaShort ? 1 : -1;
		score += candle.ClosePrice > emaMedium ? 1 : -1;
		score += candle.ClosePrice > emaLong ? 1 : -1;
		score += emaShort > emaMedium ? 1 : -1;
		score += emaMedium > emaLong ? 1 : -1;
		score += emaShort > emaLong ? 1 : -1;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevScore = score;
			return;
		}

		if (_prevScore < Threshold && score >= Threshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (_prevScore > -Threshold && score <= -Threshold && Position >= 0)
		{
			SellMarket();
		}

		_prevScore = score;
	}
}
