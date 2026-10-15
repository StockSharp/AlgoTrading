using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBVious MA strategy.
/// On-balance volume is compared with four simple moving averages of itself. A long opens when OBV crosses above the long entry
/// average and closes when it crosses below the long exit average. A short opens when OBV crosses below the short entry average
/// and closes when it crosses above the short exit average. TradeDirection ("Long", "Short" or "Both") limits the entries.
/// </summary>
public class ObviousMaStrategy : Strategy
{
	private readonly StrategyParam<int> _longEntryLength;
	private readonly StrategyParam<int> _longExitLength;
	private readonly StrategyParam<int> _shortEntryLength;
	private readonly StrategyParam<int> _shortExitLength;
	private readonly StrategyParam<string> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _longEntryMa;
	private SimpleMovingAverage _longExitMa;
	private SimpleMovingAverage _shortEntryMa;
	private SimpleMovingAverage _shortExitMa;

	private decimal? _prevObv;
	private decimal? _prevLongEntry;
	private decimal? _prevLongExit;
	private decimal? _prevShortEntry;
	private decimal? _prevShortExit;

	/// <summary>
	/// Length of the OBV average for long entries.
	/// </summary>
	public int LongEntryLength
	{
		get => _longEntryLength.Value;
		set => _longEntryLength.Value = value;
	}

	/// <summary>
	/// Length of the OBV average for long exits.
	/// </summary>
	public int LongExitLength
	{
		get => _longExitLength.Value;
		set => _longExitLength.Value = value;
	}

	/// <summary>
	/// Length of the OBV average for short entries.
	/// </summary>
	public int ShortEntryLength
	{
		get => _shortEntryLength.Value;
		set => _shortEntryLength.Value = value;
	}

	/// <summary>
	/// Length of the OBV average for short exits.
	/// </summary>
	public int ShortExitLength
	{
		get => _shortExitLength.Value;
		set => _shortExitLength.Value = value;
	}

	/// <summary>
	/// Allowed entries: "Long", "Short" or "Both".
	/// </summary>
	public string TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
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
	public ObviousMaStrategy()
	{
		_longEntryLength = Param(nameof(LongEntryLength), 190)
			.SetGreaterThanZero()
			.SetDisplay("Long Entry Length", "Length of the OBV average for long entries", "Indicators");

		_longExitLength = Param(nameof(LongExitLength), 202)
			.SetGreaterThanZero()
			.SetDisplay("Long Exit Length", "Length of the OBV average for long exits", "Indicators");

		_shortEntryLength = Param(nameof(ShortEntryLength), 395)
			.SetGreaterThanZero()
			.SetDisplay("Short Entry Length", "Length of the OBV average for short entries", "Indicators");

		_shortExitLength = Param(nameof(ShortExitLength), 300)
			.SetGreaterThanZero()
			.SetDisplay("Short Exit Length", "Length of the OBV average for short exits", "Indicators");

		_tradeDirection = Param(nameof(TradeDirection), "Long")
			.SetDisplay("Trade Direction", "Allowed entries: Long, Short or Both", "General");

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
		ResetState();
	}

	private void ResetState()
	{
		_prevObv = null;
		_prevLongEntry = null;
		_prevLongExit = null;
		_prevShortEntry = null;
		_prevShortExit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var obv = new OnBalanceVolume();
		_longEntryMa = new SimpleMovingAverage { Length = LongEntryLength };
		_longExitMa = new SimpleMovingAverage { Length = LongExitLength };
		_shortEntryMa = new SimpleMovingAverage { Length = ShortEntryLength };
		_shortExitMa = new SimpleMovingAverage { Length = ShortExitLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(obv, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, obv);
				DrawIndicator(oscillators, _longEntryMa);
				DrawIndicator(oscillators, _longExitMa);
				DrawIndicator(oscillators, _shortEntryMa);
				DrawIndicator(oscillators, _shortExitMa);
			}
		}
	}

	private static decimal? ProcessAverage(SimpleMovingAverage ma, decimal obv, DateTime time)
	{
		var value = ma.Process(new DecimalIndicatorValue(ma, obv, time) { IsFinal = true });
		return ma.IsFormed ? value.GetValue<decimal>() : null;
	}

	private void ProcessCandle(ICandleMessage candle, decimal obv)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The averages are built on OBV itself, not on price.
		var longEntry = ProcessAverage(_longEntryMa, obv, candle.OpenTime);
		var longExit = ProcessAverage(_longExitMa, obv, candle.OpenTime);
		var shortEntry = ProcessAverage(_shortEntryMa, obv, candle.OpenTime);
		var shortExit = ProcessAverage(_shortExitMa, obv, candle.OpenTime);

		var prevObv = _prevObv;
		var prevLongEntry = _prevLongEntry;
		var prevLongExit = _prevLongExit;
		var prevShortEntry = _prevShortEntry;
		var prevShortExit = _prevShortExit;

		_prevObv = obv;
		_prevLongEntry = longEntry;
		_prevLongExit = longExit;
		_prevShortEntry = shortEntry;
		_prevShortExit = shortExit;

		if (prevObv is not decimal prev || !IsFormedAndOnlineAndAllowTrading())
			return;

		var allowLong = !TradeDirection.EqualsIgnoreCase("Short");
		var allowShort = !TradeDirection.EqualsIgnoreCase("Long");

		var crossUpLongEntry = longEntry is decimal le && prevLongEntry is decimal ple && prev <= ple && obv > le;
		var crossDownLongExit = longExit is decimal lx && prevLongExit is decimal plx && prev >= plx && obv < lx;
		var crossDownShortEntry = shortEntry is decimal se && prevShortEntry is decimal pse && prev >= pse && obv < se;
		var crossUpShortExit = shortExit is decimal sx && prevShortExit is decimal psx && prev <= psx && obv > sx;

		if (crossUpLongEntry && allowLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDownShortEntry && allowShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && crossDownLongExit)
			SellMarket(Position);
		else if (Position < 0 && crossUpShortExit)
			BuyMarket(-Position);
	}
}
