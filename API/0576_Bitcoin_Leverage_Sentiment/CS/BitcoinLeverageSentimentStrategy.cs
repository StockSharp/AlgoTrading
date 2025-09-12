using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin leverage sentiment strategy using Z-Score of longs vs shorts.
/// </summary>
public class BitcoinLeverageSentimentStrategy : Strategy
{
	private readonly StrategyParam<Security> _longsSecurity;
	private readonly StrategyParam<Security> _shortsSecurity;
	private readonly StrategyParam<TradeDirection> _tradeDirection;
	private readonly StrategyParam<int> _zScoreLength;
	private readonly StrategyParam<decimal> _thresholdLongEntry;
	private readonly StrategyParam<decimal> _thresholdLongExit;
	private readonly StrategyParam<decimal> _thresholdShortEntry;
	private readonly StrategyParam<decimal> _thresholdShortExit;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private StandardDeviation _stdDev;
	private decimal _lastLong;
	private decimal _lastShort;
	private decimal _prevZ;

	public Security LongsSecurity { get => _longsSecurity.Value; set => _longsSecurity.Value = value; }
	public Security ShortsSecurity { get => _shortsSecurity.Value; set => _shortsSecurity.Value = value; }
	public TradeDirection Direction { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }
	public int ZScoreLength { get => _zScoreLength.Value; set => _zScoreLength.Value = value; }
	public decimal LongEntryThreshold { get => _thresholdLongEntry.Value; set => _thresholdLongEntry.Value = value; }
	public decimal LongExitThreshold { get => _thresholdLongExit.Value; set => _thresholdLongExit.Value = value; }
	public decimal ShortEntryThreshold { get => _thresholdShortEntry.Value; set => _thresholdShortEntry.Value = value; }
	public decimal ShortExitThreshold { get => _thresholdShortExit.Value; set => _thresholdShortExit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BitcoinLeverageSentimentStrategy()
	{
		_longsSecurity = Param<Security>(nameof(LongsSecurity), null)
			.SetDisplay("BTC Longs", "Security with BTC longs data", "Data");

		_shortsSecurity = Param<Security>(nameof(ShortsSecurity), null)
			.SetDisplay("BTC Shorts", "Security with BTC shorts data", "Data");

		_tradeDirection = Param(nameof(Direction), TradeDirection.Both)
			.SetDisplay("Trade Direction", "Allowed trade direction", "General");

		_zScoreLength = Param(nameof(ZScoreLength), 252)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Length", "Period for Z-Score calculation", "General");

		_thresholdLongEntry = Param(nameof(LongEntryThreshold), 1m)
			.SetDisplay("Long Entry", "Z-Score threshold for long entry", "Thresholds");

		_thresholdLongExit = Param(nameof(LongExitThreshold), -1.618m)
			.SetDisplay("Long Exit", "Z-Score threshold for long exit", "Thresholds");

		_thresholdShortEntry = Param(nameof(ShortEntryThreshold), -1.618m)
			.SetDisplay("Short Entry", "Z-Score threshold for short entry", "Thresholds");

		_thresholdShortExit = Param(nameof(ShortExitThreshold), 1m)
			.SetDisplay("Short Exit", "Z-Score threshold for short exit", "Thresholds");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for longs/shorts data", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (LongsSecurity == null || ShortsSecurity == null)
			throw new InvalidOperationException("Both longs and shorts securities must be set.");

		return [(Security, CandleType), (LongsSecurity, CandleType), (ShortsSecurity, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_lastLong = 0m;
		_lastShort = 0m;
		_prevZ = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		if (LongsSecurity == null || ShortsSecurity == null)
			throw new InvalidOperationException("Both longs and shorts securities must be set.");

		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = ZScoreLength };
		_stdDev = new StandardDeviation { Length = ZScoreLength };

		SubscribeCandles(CandleType, true, LongsSecurity)
			.Bind(ProcessLongs)
			.Start();

		SubscribeCandles(CandleType, true, ShortsSecurity)
			.Bind(ProcessShorts)
			.Start();

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLongs(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastLong = candle.ClosePrice;
		ProcessRatio(candle);
	}

	private void ProcessShorts(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastShort = candle.ClosePrice;
		ProcessRatio(candle);
	}

	private void ProcessRatio(ICandleMessage candle)
	{
		if (_lastLong <= 0m || _lastShort <= 0m)
			return;

		var ratio = _lastLong / (_lastLong + _lastShort);

		var mean = _sma.Process(ratio, candle.ServerTime, true).ToDecimal();
		var std = _stdDev.Process(ratio, candle.ServerTime, true).ToDecimal();

		if (!_sma.IsFormed || !_stdDev.IsFormed || std == 0m)
			return;

		var z = (ratio - mean) / std;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevZ = z;
			return;
		}

		if (Direction != TradeDirection.Short && Position <= 0 && _prevZ < LongEntryThreshold && z >= LongEntryThreshold)
		{
			BuyMarket();
		}
		else if (Direction != TradeDirection.Short && Position > 0 && _prevZ > LongExitThreshold && z <= LongExitThreshold)
		{
			SellMarket();
		}

		if (Direction != TradeDirection.Long && Position >= 0 && _prevZ > ShortEntryThreshold && z <= ShortEntryThreshold)
		{
			SellMarket();
		}
		else if (Direction != TradeDirection.Long && Position < 0 && _prevZ < ShortExitThreshold && z >= ShortExitThreshold)
		{
			BuyMarket();
		}

		_prevZ = z;
	}
}
