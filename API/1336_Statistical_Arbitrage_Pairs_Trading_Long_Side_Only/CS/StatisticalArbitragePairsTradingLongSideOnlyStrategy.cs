using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Statistical arbitrage pairs trading strategy (long only).
/// Compares z-score of main security and paired security.
/// Buys when spread falls below threshold and closes when it returns above zero.
/// </summary>
public class StatisticalArbitragePairsTradingLongSideOnlyStrategy : Strategy
{
	private readonly StrategyParam<int> _zScoreLength;
	private readonly StrategyParam<decimal> _extremeLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _pairedSecurity;

	private decimal? _mainZScore;
	private decimal? _pairedZScore;

	/// <summary>
	/// Period length for z-score calculation.
	/// </summary>
	public int ZScoreLength
	{
		get => _zScoreLength.Value;
		set => _zScoreLength.Value = value;
	}

	/// <summary>
	/// Threshold for long entry.
	/// </summary>
	public decimal ExtremeLevel
	{
		get => _extremeLevel.Value;
		set => _extremeLevel.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Second instrument for pairs trading.
	/// </summary>
	public Security PairedSecurity
	{
		get => _pairedSecurity.Value;
		set => _pairedSecurity.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public StatisticalArbitragePairsTradingLongSideOnlyStrategy()
	{
		_zScoreLength = Param(nameof(ZScoreLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Length", "Period for z-score calculation", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_extremeLevel = Param(nameof(ExtremeLevel), -1m)
			.SetDisplay("Long Entry Threshold", "Entry threshold for spread z-score", "General")
			.SetCanOptimize(true)
			.SetOptimize(-2m, -0.5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_pairedSecurity = Param(nameof(PairedSecurity), default(Security))
			.SetDisplay("Paired Security", "Second instrument for pairs trading", "General")
			.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(PairedSecurity, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_mainZScore = null;
		_pairedZScore = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var mainSma = new SimpleMovingAverage { Length = ZScoreLength };
		var mainStd = new StandardDeviation { Length = ZScoreLength };
		var pairSma = new SimpleMovingAverage { Length = ZScoreLength };
		var pairStd = new StandardDeviation { Length = ZScoreLength };

		var mainSubscription = SubscribeCandles(CandleType);
		var pairSubscription = SubscribeCandles(CandleType, security: PairedSecurity);

		mainSubscription
			.Bind(mainSma, mainStd, ProcessMainCandle)
			.Start();

		pairSubscription
			.Bind(pairSma, pairStd, ProcessPairedCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, mainSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPairedCandle(ICandleMessage candle, decimal sma, decimal std)
	{
		if (candle.State != CandleStates.Finished || std == 0)
			return;

		_pairedZScore = (candle.ClosePrice - sma) / std;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal sma, decimal std)
	{
		if (candle.State != CandleStates.Finished || std == 0)
			return;

		_mainZScore = (candle.ClosePrice - sma) / std;

		if (_pairedZScore is null)
			return;

		var spread = _mainZScore.Value - _pairedZScore.Value;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (spread < ExtremeLevel)
				BuyMarket();
		}
		else if (spread >= 0)
		{
			SellMarket(Position);
		}
	}
}
