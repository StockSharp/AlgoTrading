using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands and EMA strategy.
/// Buys when price closes below the lower Bollinger Band and sells when price closes above the upper band.
/// Exits using a fixed stop based on a wider Bollinger Band and a profit target at EMA.
/// </summary>
public class BollingerEmaStatsStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _entryMultiplier;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// Bollinger Bands period length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for entry bands.
	/// </summary>
	public decimal EntryMultiplier
	{
		get => _entryMultiplier.Value;
		set => _entryMultiplier.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for stop bands.
	/// </summary>
	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <summary>
	/// EMA period for exit target.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
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
	/// Initializes a new instance of <see cref="BollingerEmaStatsStrategy"/>.
	/// </summary>
	public BollingerEmaStatsStrategy()
	{
		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
			
			.SetOptimize(10, 50, 5);

		_entryMultiplier = Param(nameof(EntryMultiplier), 2.0m)
			.SetDisplay("Entry StdDev Mult", "StdDev multiplier for entry bands", "Bollinger Bands")
			
			.SetOptimize(1.0m, 4.0m, 0.5m);

		_stopMultiplier = Param(nameof(StopMultiplier), 3.0m)
			.SetDisplay("Stop StdDev Mult", "StdDev multiplier for stop bands", "Bollinger Bands")
			
			.SetOptimize(2.0m, 5.0m, 0.5m);

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Exit Period", "EMA period for exit", "EMA")
			
			.SetOptimize(10, 50, 5);

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
		_stopPrice = 0m;
		_targetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		var bbEntry = new BollingerBands { Length = BbLength, Width = EntryMultiplier };
		var bbStop = new BollingerBands { Length = BbLength, Width = StopMultiplier };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { bbEntry, bbStop, ema }, ProcessCandle, true)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bbEntry);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (values[0].IsEmpty || values[1].IsEmpty || values[2].IsEmpty)
			return;

		var bbEntry = (BollingerBandsValue)values[0];
		var bbStop = (BollingerBandsValue)values[1];
		var emaValue = values[2].ToDecimal();

		if (bbEntry.UpBand is not decimal entryUpper || bbEntry.LowBand is not decimal entryLower)
			return;
		if (bbStop.UpBand is not decimal stopUpper || bbStop.LowBand is not decimal stopLower)
			return;

		if (Position == 0)
		{
			if (candle.ClosePrice < entryLower)
			{
				BuyMarket();
				_stopPrice = stopLower;
				_targetPrice = emaValue;
			}
			else if (candle.ClosePrice > entryUpper)
			{
				SellMarket();
				_stopPrice = stopUpper;
				_targetPrice = emaValue;
			}
		}
		else if (Position > 0)
		{
			if (_targetPrice != 0m &&
				(candle.ClosePrice >= _targetPrice || candle.ClosePrice <= _stopPrice))
			{
				SellMarket();
				_stopPrice = 0m;
				_targetPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (_targetPrice != 0m &&
				(candle.ClosePrice <= _targetPrice || candle.ClosePrice >= _stopPrice))
			{
				BuyMarket();
				_stopPrice = 0m;
				_targetPrice = 0m;
			}
		}
	}
}
