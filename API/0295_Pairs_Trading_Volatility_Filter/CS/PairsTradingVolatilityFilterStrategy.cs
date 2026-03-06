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
/// Pairs trading strategy with a volatility filter.
/// Trades the spread between two securities only when the primary leg volatility is below its recent average.
/// </summary>
public class PairsTradingVolatilityFilterStrategy : Strategy
{
	private enum SpreadState
	{
		Flat,
		LongSpread,
		ShortSpread,
	}

	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Security _security2;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _spreadAverage;
	private StandardDeviation _spreadStdDev;
	private SimpleMovingAverage _atrAverage;
	private decimal _latestPrice1;
	private decimal _latestPrice2;
	private decimal _hedgeRatio;
	private decimal _entrySpread;
	private decimal _secondaryVolume;
	private int _cooldown;
	private SpreadState _spreadState;

	/// <summary>
	/// Secondary security identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Lookback period for spread and volatility statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Entry threshold expressed in standard deviations.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Exit threshold expressed in standard deviations.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage applied to spread distance.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait between spread orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PairsTradingVolatilityFilterStrategy"/>.
	/// </summary>
	public PairsTradingVolatilityFilterStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Second Security Id", "Identifier of the second security", "General");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 40)
			.SetRange(10, 100)
			.SetDisplay("Lookback Period", "Lookback period for spread and volatility statistics", "Strategy Parameters");

		_entryThreshold = Param(nameof(EntryThreshold), 0.75m)
			.SetRange(1m, 5m)
			.SetDisplay("Entry Threshold", "Entry threshold in standard deviations", "Strategy Parameters");

		_exitThreshold = Param(nameof(ExitThreshold), 0.1m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Exit threshold in standard deviations", "Strategy Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage applied to spread distance", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between spread orders", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_atr = null;
		_spreadAverage = null;
		_spreadStdDev = null;
		_atrAverage = null;
		_latestPrice1 = default;
		_latestPrice2 = default;
		_hedgeRatio = default;
		_entrySpread = default;
		_secondaryVolume = default;
		_cooldown = default;
		_spreadState = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Second security identifier is not specified.");

		_security2 = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };
		_atr = new AverageTrueRange { Length = 14 };
		_spreadAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_spreadStdDev = new StandardDeviation { Length = LookbackPeriod };
		_atrAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_cooldown = 0;
		_spreadState = SpreadState.Flat;

		var primarySubscription = SubscribeCandles(CandleType);
		var secondarySubscription = SubscribeCandles(CandleType, security: _security2);

		primarySubscription
			.Bind(_atr, ProcessPrimaryCandle)
			.Start();

		secondarySubscription
			.Bind(ProcessSecondaryCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawCandles(area, secondarySubscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed)
			return;

		_latestPrice1 = candle.ClosePrice;

		var atrAverageValue = _atrAverage.Process(new DecimalIndicatorValue(_atrAverage, atrValue, candle.OpenTime)).ToDecimal();

		if (!_atrAverage.IsFormed || _latestPrice2 <= 0)
			return;

		if (_hedgeRatio <= 0)
			_hedgeRatio = _latestPrice1 / _latestPrice2;

		var spread = _latestPrice1 - (_latestPrice2 * _hedgeRatio);
		var spreadAverageValue = _spreadAverage.Process(new DecimalIndicatorValue(_spreadAverage, spread, candle.OpenTime)).ToDecimal();
		var spreadStdValue = _spreadStdDev.Process(new DecimalIndicatorValue(_spreadStdDev, spread, candle.OpenTime)).ToDecimal();

		if (!_spreadAverage.IsFormed || !_spreadStdDev.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (spreadStdValue <= 0)
			return;

		var zScore = (spread - spreadAverageValue) / spreadStdValue;
		var isLowVolatility = atrValue <= atrAverageValue * 10m;

		if (_spreadState == SpreadState.Flat)
		{
			if (!isLowVolatility)
				return;

			if (zScore <= -EntryThreshold)
			{
				OpenLongSpread(spread);
			}
			else if (zScore >= EntryThreshold)
			{
				OpenShortSpread(spread);
			}

			return;
		}

		if (_spreadState == SpreadState.LongSpread)
		{
			if (zScore >= ExitThreshold || IsStopLossHit(spread, isLongSpread: true))
				CloseSpread();
		}
		else if (_spreadState == SpreadState.ShortSpread)
		{
			if (zScore <= -ExitThreshold || IsStopLossHit(spread, isLongSpread: false))
				CloseSpread();
		}
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrice2 = candle.ClosePrice;
	}

	private void OpenLongSpread(decimal spread)
	{
		_entrySpread = spread;
		_secondaryVolume = Volume;
		_spreadState = SpreadState.LongSpread;

		BuyMarket(Volume, Security);
		SellMarket(_secondaryVolume, _security2);

		_cooldown = CooldownBars;
	}

	private void OpenShortSpread(decimal spread)
	{
		_entrySpread = spread;
		_secondaryVolume = Volume;
		_spreadState = SpreadState.ShortSpread;

		SellMarket(Volume, Security);
		BuyMarket(_secondaryVolume, _security2);

		_cooldown = CooldownBars;
	}

	private void CloseSpread()
	{
		if (_spreadState == SpreadState.LongSpread)
		{
			SellMarket(Math.Abs(Position), Security);
			BuyMarket(_secondaryVolume, _security2);
		}
		else if (_spreadState == SpreadState.ShortSpread)
		{
			BuyMarket(Math.Abs(Position), Security);
			SellMarket(_secondaryVolume, _security2);
		}

		_entrySpread = default;
		_secondaryVolume = default;
		_spreadState = SpreadState.Flat;
		_cooldown = CooldownBars;
	}

	private bool IsStopLossHit(decimal currentSpread, bool isLongSpread)
	{
		var baseDistance = Math.Max(Math.Abs(_entrySpread) * StopLossPercent / 100m, Security.PriceStep ?? 1m);

		return isLongSpread
			? currentSpread <= _entrySpread - baseDistance
			: currentSpread >= _entrySpread + baseDistance;
	}
}
