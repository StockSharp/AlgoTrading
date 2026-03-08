using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// True Strength Index crossover strategy filtered by DeMarker.
/// </summary>
public class TSIDeMarkerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _tsiSpread;
	private readonly StrategyParam<decimal> _longDeMarkerLimit;
	private readonly StrategyParam<decimal> _shortDeMarkerLimit;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal? _prevTsi;
	private decimal? _prevSignal;
	private int _cooldownRemaining;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for DeMarker indicator.
	/// </summary>
	public int DemarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute spread between TSI and its signal line.
	/// </summary>
	public decimal TsiSpread
	{
		get => _tsiSpread.Value;
		set => _tsiSpread.Value = value;
	}

	/// <summary>
	/// Maximum DeMarker value allowed for long entries.
	/// </summary>
	public decimal LongDeMarkerLimit
	{
		get => _longDeMarkerLimit.Value;
		set => _longDeMarkerLimit.Value = value;
	}

	/// <summary>
	/// Minimum DeMarker value allowed for short entries.
	/// </summary>
	public decimal ShortDeMarkerLimit
	{
		get => _shortDeMarkerLimit.Value;
		set => _shortDeMarkerLimit.Value = value;
	}

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TSIDeMarkerStrategy"/> class.
	/// </summary>
	public TSIDeMarkerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");

		_demarkerPeriod = Param(nameof(DemarkerPeriod), 14)
			.SetDisplay("DeMarker Period", "Period for DeMarker", "Indicators");

		_tsiSpread = Param(nameof(TsiSpread), 2m)
			.SetDisplay("TSI Spread", "Minimum spread between TSI and its signal line", "Filters");

		_longDeMarkerLimit = Param(nameof(LongDeMarkerLimit), 0.55m)
			.SetDisplay("Long DeMarker", "Maximum DeMarker for long entries", "Filters");

		_shortDeMarkerLimit = Param(nameof(ShortDeMarkerLimit), 0.45m)
			.SetDisplay("Short DeMarker", "Minimum DeMarker for short entries", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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

		_prevTsi = null;
		_prevSignal = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var tsi = new TrueStrengthIndex();
		var demarker = new DeMarker { Length = DemarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tsi, demarker, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue tsiValue, IIndicatorValue demarkerValue)
	{
		if (candle.State != CandleStates.Finished || !tsiValue.IsFinal || !demarkerValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var tsiPair = (ITrueStrengthIndexValue)tsiValue;
		if (tsiPair.Tsi is not decimal tsi || tsiPair.Signal is not decimal signal)
			return;

		var demarker = demarkerValue.ToDecimal();
		if (_prevTsi is not decimal prevTsi || _prevSignal is not decimal prevSignal)
		{
			_prevTsi = tsi;
			_prevSignal = signal;
			return;
		}

		var crossUp = prevTsi <= prevSignal && tsi > signal && Math.Abs(tsi - signal) >= TsiSpread;
		var crossDown = prevTsi >= prevSignal && tsi < signal && Math.Abs(tsi - signal) >= TsiSpread;

		if (_cooldownRemaining == 0)
		{
			if (crossUp && demarker <= LongDeMarkerLimit && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (crossDown && demarker >= ShortDeMarkerLimit && Position >= 0)
			{
				if (Position > 0)
					SellMarket();

				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevTsi = tsi;
		_prevSignal = signal;
	}
}
