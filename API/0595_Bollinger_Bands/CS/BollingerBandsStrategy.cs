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
/// Strategy trading Bollinger Bands breakouts.
/// </summary>
public class BollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private decimal _entryPrice;
	private decimal _stopPrice;

	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal BbDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	public BollingerBandsStrategy()
	{
		_bbLength = Param(nameof(BbLength), 120)
			.SetDisplay("BB Length", "Bollinger Bands length", "Parameters")
			
			.SetOptimize(20, 200, 20);

		_bbDeviation = Param(nameof(BbDeviation), 2m)
			.SetDisplay("Standard Deviation", "Bollinger Bands deviation", "Parameters")
			
			.SetOptimize(1m, 3m, 0.5m);

		_smaLength = Param(nameof(SmaLength), 110)
			.SetDisplay("SMA Length", "SMA exit length", "Parameters")
			
			.SetOptimize(20, 200, 20);

		_stopLossPercent = Param(nameof(StopLossPercent), 6m)
			.SetDisplay("Stop Loss (%)", "Max risk per trade in percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2017, 1, 1), TimeSpan.Zero))
			.SetDisplay("Start Date", "Backtest start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2024, 7, 1), TimeSpan.Zero))
			.SetDisplay("End Date", "Backtest end date", "General");
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

		_entryPrice = default;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BbLength,
			Width = BbDeviation
		};

		var sma = new SimpleMovingAverage
		{
			Length = SmaLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { bb, sma }, ProcessCandle, true)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (values.Any(v => v.IsEmpty))
			return;

		var bb = (BollingerBandsValue)values[0];
		var upper = bb.UpBand ?? 0m;
		var lower = bb.LowBand ?? 0m;
		var smaExit = values[1].ToDecimal();

		var time = candle.OpenTime;
		var inRange = time >= StartDate && time <= EndDate;

		if (!inRange)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
			return;
		}

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close < smaExit || candle.LowPrice <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (close > smaExit || candle.HighPrice >= _stopPrice)
				BuyMarket();
		}
		else
		{
			if (close > upper)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = _entryPrice * (1m - StopLossPercent / 100m);
			}
			else if (close < lower)
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = _entryPrice * (1m + StopLossPercent / 100m);
			}
		}
	}
}
