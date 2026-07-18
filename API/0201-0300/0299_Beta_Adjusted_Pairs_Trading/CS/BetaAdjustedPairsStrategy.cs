using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion strategy that trades the primary instrument when a beta-adjusted spread versus the secondary instrument becomes stretched.
/// </summary>
public class BetaAdjustedPairsStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<decimal> _betaAsset1;
	private readonly StrategyParam<decimal> _betaAsset2;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Security _security2;
	private SimpleMovingAverage _spreadAverage;
	private StandardDeviation _spreadStdDev;
	private decimal _latestPrice1;
	private decimal _latestPrice2;
	private decimal _entrySpread;
	private bool _primaryUpdated;
	private bool _secondaryUpdated;
	private int _cooldown;

	/// <summary>
	/// Secondary security identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Beta coefficient of the primary security.
	/// </summary>
	public decimal BetaAsset1
	{
		get => _betaAsset1.Value;
		set => _betaAsset1.Value = value;
	}

	/// <summary>
	/// Beta coefficient of the secondary security.
	/// </summary>
	public decimal BetaAsset2
	{
		get => _betaAsset2.Value;
		set => _betaAsset2.Value = value;
	}

	/// <summary>
	/// Lookback period for spread statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Entry threshold measured in spread standard deviations.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Exit threshold measured in spread standard deviations.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage applied to spread distance from entry.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	public BetaAdjustedPairsStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Second Security Id", "Identifier of the secondary security", "General");

		_betaAsset1 = Param(nameof(BetaAsset1), 1m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Primary Beta", "Beta coefficient of the primary security", "Spread");

		_betaAsset2 = Param(nameof(BetaAsset2), 1m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Secondary Beta", "Beta coefficient of the secondary security", "Spread");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 30)
			.SetRange(10, 150)
			.SetDisplay("Lookback Period", "Lookback period for spread statistics", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.1m)
			.SetRange(0.25m, 5m)
			.SetDisplay("Entry Threshold", "Entry threshold in spread standard deviations", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.15m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Exit threshold in spread standard deviations", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 120)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_security2 = null;
		_spreadAverage = null;
		_spreadStdDev = null;
		_latestPrice1 = 0m;
		_latestPrice2 = 0m;
		_entrySpread = 0m;
		_primaryUpdated = false;
		_secondaryUpdated = false;
		_cooldown = 0;
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
		_spreadAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_spreadStdDev = new StandardDeviation { Length = LookbackPeriod };
		_cooldown = 0;

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

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrice1 = candle.ClosePrice;
		_primaryUpdated = true;

		TryProcessSpread(candle.OpenTime);
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrice2 = candle.ClosePrice;
		_secondaryUpdated = true;

		TryProcessSpread(candle.OpenTime);
	}

	private void TryProcessSpread(DateTimeOffset time)
	{
		if (!_primaryUpdated || !_secondaryUpdated)
			return;

		_primaryUpdated = false;
		_secondaryUpdated = false;

		if (_latestPrice1 <= 0 || _latestPrice2 <= 0 || BetaAsset1 <= 0 || BetaAsset2 <= 0)
			return;

		var spread = (_latestPrice1 / BetaAsset1) - (_latestPrice2 / BetaAsset2);
		var averageSpread = _spreadAverage.Process(spread, time.UtcDateTime, true).ToDecimal();
		var spreadStdDev = _spreadStdDev.Process(spread, time.UtcDateTime, true).ToDecimal();

		if (!_spreadAverage.IsFormed || !_spreadStdDev.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (spreadStdDev <= 0)
			return;

		var zScore = (spread - averageSpread) / spreadStdDev;

		if (Position == 0)
		{
			if (zScore <= -EntryThreshold)
			{
				_entrySpread = spread;
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (zScore >= EntryThreshold)
			{
				_entrySpread = spread;
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		var stopDistance = Math.Max(Math.Abs(_entrySpread) * StopLossPercent / 100m, Security.PriceStep ?? 1m);
		var stopTriggered = Position > 0
			? spread <= _entrySpread - stopDistance
			: spread >= _entrySpread + stopDistance;

		if (Position > 0 && (zScore >= -ExitThreshold || stopTriggered))
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (zScore <= ExitThreshold || stopTriggered))
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
