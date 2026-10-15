using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX for BTC strategy.
/// Long only: buys when ADX(14) crosses above EntryLevel while, with SmaFilter enabled, the close is above SMA(SmaLength), and closes
/// the long when ADX crosses below ExitLevel.
/// </summary>
public class AdxForBtcStrategy : Strategy
{
	private const int _adxLength = 14;

	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<bool> _smaFilter;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevAdx;

	/// <summary>
	/// ADX level whose upward cross opens a long.
	/// </summary>
	public decimal EntryLevel
	{
		get => _entryLevel.Value;
		set => _entryLevel.Value = value;
	}

	/// <summary>
	/// ADX level whose downward cross closes the long.
	/// </summary>
	public decimal ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
	}

	/// <summary>
	/// Require the close above the SMA.
	/// </summary>
	public bool SmaFilter
	{
		get => _smaFilter.Value;
		set => _smaFilter.Value = value;
	}

	/// <summary>
	/// SMA period of the trend filter.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
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
	/// Constructor.
	/// </summary>
	public AdxForBtcStrategy()
	{
		_entryLevel = Param(nameof(EntryLevel), 14m)
			.SetDisplay("Entry Level", "ADX level whose upward cross opens a long", "ADX");

		_exitLevel = Param(nameof(ExitLevel), 45m)
			.SetDisplay("Exit Level", "ADX level whose downward cross closes the long", "ADX");

		_smaFilter = Param(nameof(SmaFilter), true)
			.SetDisplay("SMA Filter", "Require the close above the SMA", "Filters");

		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period of the trend filter", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevAdx = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAdx = null;

		var adx = new AverageDirectionalIndex { Length = _adxLength };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, adx);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFormed || ((IAverageDirectionalIndexValue)adxValue).MovingAverage is not decimal adx)
			return;

		var prevAdx = _prevAdx;
		_prevAdx = adx;

		if (!smaValue.IsFormed || prevAdx is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trendOk = !SmaFilter || candle.ClosePrice > smaValue.ToDecimal();

		if (Position == 0 && prev <= EntryLevel && adx > EntryLevel && trendOk)
			BuyMarket(Volume);
		else if (Position > 0 && prev >= ExitLevel && adx < ExitLevel)
			SellMarket(Position);
	}
}
