using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Relative asset class trend-following strategy that allocates to the primary instrument when its trend is stronger than the secondary benchmark.
/// </summary>
public class AssetClassTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _minTrendStrength;
	private readonly StrategyParam<decimal> _relativeStrengthThreshold;
	private readonly StrategyParam<int> _rebalanceIntervalBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _security2 = null!;
	private SimpleMovingAverage _primarySma = null!;
	private SimpleMovingAverage _secondarySma = null!;
	private decimal _latestPrimaryPrice;
	private decimal _latestSecondaryPrice;
	private decimal _latestPrimarySma;
	private decimal _latestSecondarySma;
	private bool _primaryUpdated;
	private bool _secondaryUpdated;
	private int _barsSinceRebalance;

	/// <summary>
	/// Secondary security identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Trend moving average length.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Minimum absolute trend strength required to hold the primary instrument.
	/// </summary>
	public decimal MinTrendStrength
	{
		get => _minTrendStrength.Value;
		set => _minTrendStrength.Value = value;
	}

	/// <summary>
	/// Minimum relative outperformance of the primary instrument versus the benchmark.
	/// </summary>
	public decimal RelativeStrengthThreshold
	{
		get => _relativeStrengthThreshold.Value;
		set => _relativeStrengthThreshold.Value = value;
	}

	/// <summary>
	/// Number of paired candles between rebalancing decisions.
	/// </summary>
	public int RebalanceIntervalBars
	{
		get => _rebalanceIntervalBars.Value;
		set => _rebalanceIntervalBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type used for both instruments.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AssetClassTrendFollowingStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Second Security Id", "Identifier of the secondary benchmark security", "General");

		_smaLength = Param(nameof(SmaLength), 36)
			.SetRange(10, 200)
			.SetDisplay("SMA Length", "Trend moving average length", "Indicators");

		_minTrendStrength = Param(nameof(MinTrendStrength), 0.004m)
			.SetRange(0.001m, 0.05m)
			.SetDisplay("Min Trend Strength", "Minimum absolute trend strength required to hold the primary instrument", "Signals");

		_relativeStrengthThreshold = Param(nameof(RelativeStrengthThreshold), 0.002m)
			.SetRange(0m, 0.05m)
			.SetDisplay("Relative Strength Threshold", "Minimum relative outperformance of the primary instrument", "Signals");

		_rebalanceIntervalBars = Param(nameof(RebalanceIntervalBars), 18)
			.SetRange(1, 200)
			.SetDisplay("Rebalance Bars", "Number of paired candles between rebalancing decisions", "General");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for both instruments", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (!Security2Id.IsEmpty())
			yield return (new Security { Id = Security2Id }, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_security2 = null!;
		_primarySma = null!;
		_secondarySma = null!;
		_latestPrimaryPrice = 0m;
		_latestSecondaryPrice = 0m;
		_latestPrimarySma = 0m;
		_latestSecondarySma = 0m;
		_primaryUpdated = false;
		_secondaryUpdated = false;
		_barsSinceRebalance = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Secondary security identifier is not specified.");

		_security2 = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };
		_primarySma = new SimpleMovingAverage { Length = SmaLength };
		_secondarySma = new SimpleMovingAverage { Length = SmaLength };
		_barsSinceRebalance = RebalanceIntervalBars;

		var primarySubscription = SubscribeCandles(CandleType, security: Security);
		var secondarySubscription = SubscribeCandles(CandleType, security: _security2);

		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		secondarySubscription
			.Bind(ProcessSecondaryCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawCandles(area, secondarySubscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent));
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrimaryPrice = candle.ClosePrice;
		_latestPrimarySma = _primarySma.Process(candle).ToDecimal();
		_primaryUpdated = true;
		TryRebalance();
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestSecondaryPrice = candle.ClosePrice;
		_latestSecondarySma = _secondarySma.Process(candle).ToDecimal();
		_secondaryUpdated = true;
		TryRebalance();
	}

	private void TryRebalance()
	{
		if (!_primaryUpdated || !_secondaryUpdated)
			return;

		_primaryUpdated = false;
		_secondaryUpdated = false;

		if (!_primarySma.IsFormed || !_secondarySma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_barsSinceRebalance < RebalanceIntervalBars)
		{
			_barsSinceRebalance++;
			return;
		}

		_barsSinceRebalance = 0;

		var primaryTrend = (_latestPrimaryPrice - _latestPrimarySma) / Math.Max(_latestPrimarySma, 1m);
		var secondaryTrend = (_latestSecondaryPrice - _latestSecondarySma) / Math.Max(_latestSecondarySma, 1m);
		var relativeStrength = primaryTrend - secondaryTrend;
		var shouldHoldLong = primaryTrend >= MinTrendStrength && relativeStrength >= RelativeStrengthThreshold;

		if (shouldHoldLong && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
		}
		else if (!shouldHoldLong && Position > 0)
		{
			SellMarket(Position);
		}
	}
}
