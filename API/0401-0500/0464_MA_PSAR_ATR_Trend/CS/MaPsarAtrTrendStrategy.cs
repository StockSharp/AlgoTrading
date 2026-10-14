namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MA PSAR ATR Trend Strategy.
/// A long opens when Fast MA > Slow MA, the close is above the fast MA and the low is above the Parabolic SAR of the last
/// finished daily candle; a short mirrors this. UsePsarFilter switches the daily SAR condition off. Each entry gets a stop
/// AtrMultiplierLong or AtrMultiplierShort ATRs away; a position closes when the stop is hit or the fast MA crosses back
/// over the slow MA.
/// </summary>
public class MaPsarAtrTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplierLong;
	private readonly StrategyParam<decimal> _atrMultiplierShort;
	private readonly StrategyParam<bool> _usePsarFilter;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _dailySar;
	private decimal? _stopPrice;

	/// <summary>
	/// Fast MA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow MA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMaxStep
	{
		get => _sarMaxStep.Value;
		set => _sarMaxStep.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the long stop.
	/// </summary>
	public decimal AtrMultiplierLong
	{
		get => _atrMultiplierLong.Value;
		set => _atrMultiplierLong.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the short stop.
	/// </summary>
	public decimal AtrMultiplierShort
	{
		get => _atrMultiplierShort.Value;
		set => _atrMultiplierShort.Value = value;
	}

	/// <summary>
	/// Require the daily Parabolic SAR to agree.
	/// </summary>
	public bool UsePsarFilter
	{
		get => _usePsarFilter.Value;
		set => _usePsarFilter.Value = value;
	}

	/// <summary>
	/// Candle type of the signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MaPsarAtrTrendStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast MA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 160)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow MA period", "Indicators");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Parabolic SAR acceleration step", "PSAR");

		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Step", "Parabolic SAR maximum acceleration", "PSAR");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_atrMultiplierLong = Param(nameof(AtrMultiplierLong), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier Long", "ATR multiplier of the long stop", "Risk");

		_atrMultiplierShort = Param(nameof(AtrMultiplierShort), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier Short", "ATR multiplier of the short stop", "Risk");

		_usePsarFilter = Param(nameof(UsePsarFilter), true)
			.SetDisplay("Use PSAR Filter", "Require the daily Parabolic SAR to agree", "PSAR");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type of the signals", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_dailySar = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_dailySar = null;
		_stopPrice = null;

		var fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var psar = new ParabolicSar { AccelerationStep = SarStep, AccelerationMax = SarMaxStep };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastMa, slowMa, atr, ProcessCandle)
			.Start();

		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
			.BindEx(psar, ProcessDailyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, IIndicatorValue psarValue)
	{
		if (candle.State != CandleStates.Finished || !psarValue.IsFormed)
			return;

		_dailySar = psarValue.GetValue<decimal>();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed || !atrValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (fast < slow || (_stopPrice is decimal stop && candle.LowPrice <= stop))
			{
				SellMarket(Position);
				_stopPrice = null;
			}

			return;
		}

		if (Position < 0)
		{
			if (fast > slow || (_stopPrice is decimal stop && candle.HighPrice >= stop))
			{
				BuyMarket(-Position);
				_stopPrice = null;
			}

			return;
		}

		if (UsePsarFilter && _dailySar is null)
			return;

		var longSar = !UsePsarFilter || candle.LowPrice > _dailySar;
		var shortSar = !UsePsarFilter || candle.HighPrice < _dailySar;

		if (fast > slow && close > fast && longSar)
		{
			BuyMarket(Volume);
			_stopPrice = close - atr * AtrMultiplierLong;
		}
		else if (fast < slow && close < fast && shortSar)
		{
			SellMarket(Volume);
			_stopPrice = close + atr * AtrMultiplierShort;
		}
	}
}
