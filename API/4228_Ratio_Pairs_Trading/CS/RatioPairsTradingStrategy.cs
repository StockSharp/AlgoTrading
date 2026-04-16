using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ratio-based pairs trading strategy.
/// Trades the price ratio (Asset1 / Asset2) of two correlated instruments.
/// Enters when the z-score of the ratio exceeds the entry threshold and exits
/// when the ratio reverts toward its rolling mean.
/// </summary>
public class RatioPairsTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryZScore;
	private readonly StrategyParam<decimal> _exitZScore;
	private readonly StrategyParam<decimal> _hedgeRatio;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _secondSecurity;

	private BollingerBands _ratioBands;

	private decimal _firstPrice;
	private decimal _secondPrice;
	private DateTimeOffset _firstTime;
	private DateTimeOffset _secondTime;

	/// <summary>
	/// Rolling window for mean and standard deviation of the ratio.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Z-score threshold to open a pair position.
	/// </summary>
	public decimal EntryZScore
	{
		get => _entryZScore.Value;
		set => _entryZScore.Value = value;
	}

	/// <summary>
	/// Z-score threshold to close the pair when the ratio reverts.
	/// </summary>
	public decimal ExitZScore
	{
		get => _exitZScore.Value;
		set => _exitZScore.Value = value;
	}

	/// <summary>
	/// Volume multiplier for the second leg (dollar-neutral hedge ratio).
	/// </summary>
	public decimal HedgeRatio
	{
		get => _hedgeRatio.Value;
		set => _hedgeRatio.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage protection.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type used for both securities.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Second security in the traded pair.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RatioPairsTradingStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Rolling window for ratio statistics", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_entryZScore = Param(nameof(EntryZScore), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Z-Score", "Z-score threshold to open a pair position", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 3.0m, 0.25m);

		_exitZScore = Param(nameof(ExitZScore), 0.5m)
			.SetNotNegative()
			.SetDisplay("Exit Z-Score", "Z-score threshold to close the pair", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.0m, 0.1m);

		_hedgeRatio = Param(nameof(HedgeRatio), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Hedge Ratio", "Volume multiplier for the second leg", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2.0m, 0.1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-loss %", "Protective stop per leg, in percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for both securities", "General");

		_secondSecurity = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Security", "Second security in the pair", "General")
			.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(SecondSecurity, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ratioBands = null;
		_firstPrice = 0m;
		_secondPrice = 0m;
		_firstTime = default;
		_secondTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (SecondSecurity == null)
			throw new InvalidOperationException("Second security is not specified.");

		_ratioBands = new BollingerBands
		{
			Length = LookbackPeriod,
			Width = EntryZScore
		};

		var firstSubscription = SubscribeCandles(CandleType);
		var secondSubscription = SubscribeCandles(CandleType, security: SecondSecurity);

		firstSubscription
			.Bind(ProcessFirstCandle)
			.Start();

		secondSubscription
			.Bind(ProcessSecondCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, firstSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessFirstCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_firstPrice = candle.ClosePrice;
		_firstTime = candle.OpenTime;

		TryProcessPair();
	}

	private void ProcessSecondCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_secondPrice = candle.ClosePrice;
		_secondTime = candle.OpenTime;

		TryProcessPair();
	}

	private void TryProcessPair()
	{
		if (_firstPrice <= 0m || _secondPrice <= 0m)
			return;

		// Wait until both legs align on the same bar to avoid stale pricing.
		if (_firstTime != _secondTime)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ratio = _firstPrice / _secondPrice;

		var value = _ratioBands.Process(new DecimalIndicatorValue(_ratioBands, ratio, _firstTime) { IsFinal = true });

		if (!_ratioBands.IsFormed || value.IsEmpty)
			return;

		var bands = (BollingerBandsValue)value;
		if (bands.MovingAverage is not decimal mean ||
			bands.UpBand is not decimal upper ||
			bands.LowBand is not decimal lower)
		{
			return;
		}

		// Half the band width equals one standard deviation times EntryZScore.
		var stdDev = (upper - lower) / (2m * EntryZScore);
		if (stdDev <= 0m)
			return;

		var zScore = (ratio - mean) / stdDev;

		var absZ = Math.Abs(zScore);

		// Exit zone: z-score inside the reversion band and pair position is open.
		if (absZ <= ExitZScore && Position != 0)
		{
			ClosePair();
			return;
		}

		// Ratio too high => first leg overpriced vs. second: short first, long second.
		if (zScore >= EntryZScore && Position >= 0)
		{
			EnterShortFirst();
		}
		// Ratio too low => first leg underpriced vs. second: long first, short second.
		else if (zScore <= -EntryZScore && Position <= 0)
		{
			EnterLongFirst();
		}
	}

	private decimal GetSecondPosition()
	{
		return GetPositionValue(SecondSecurity, Portfolio) ?? 0m;
	}

	private void EnterLongFirst()
	{
		// Flip first leg from any short / flat to long of size Volume.
		var firstVolume = Volume + Math.Abs(Position);
		BuyMarket(firstVolume);

		// Flip second leg to short of size Volume * HedgeRatio.
		var targetSecond = Volume * HedgeRatio;
		var secondVolume = targetSecond + Math.Max(0m, GetSecondPosition());
		if (secondVolume > 0m)
			SellMarket(secondVolume, SecondSecurity);

		LogInfo($"Long pair: +{Volume} {Security?.Id} / -{targetSecond} {SecondSecurity?.Id}");
	}

	private void EnterShortFirst()
	{
		// Flip first leg from any long / flat to short of size Volume.
		var firstVolume = Volume + Math.Abs(Position);
		SellMarket(firstVolume);

		// Flip second leg to long of size Volume * HedgeRatio.
		var targetSecond = Volume * HedgeRatio;
		var secondVolume = targetSecond + Math.Max(0m, -GetSecondPosition());
		if (secondVolume > 0m)
			BuyMarket(secondVolume, SecondSecurity);

		LogInfo($"Short pair: -{Volume} {Security?.Id} / +{targetSecond} {SecondSecurity?.Id}");
	}

	private void ClosePair()
	{
		var firstPos = Position;
		if (firstPos > 0)
			SellMarket(firstPos);
		else if (firstPos < 0)
			BuyMarket(-firstPos);

		var secondPos = GetSecondPosition();
		if (secondPos > 0)
			SellMarket(secondPos, SecondSecurity);
		else if (secondPos < 0)
			BuyMarket(-secondPos, SecondSecurity);

		LogInfo("Pair closed: ratio returned to mean.");
	}
}
