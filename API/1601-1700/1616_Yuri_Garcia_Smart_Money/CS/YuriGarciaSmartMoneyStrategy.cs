using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smart money strategy using HTF zones, cumulative delta and wick pullback.
/// </summary>
public class YuriGarciaSmartMoneyStrategy : Strategy
{
	private readonly StrategyParam<int> _zoneLookback;
	private readonly StrategyParam<decimal> _zoneBuffer;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _prevBull;
	private bool _prevBear;
	private bool _isReady;

	public int ZoneLookback { get => _zoneLookback.Value; set => _zoneLookback.Value = value; }
	public decimal ZoneBuffer { get => _zoneBuffer.Value; set => _zoneBuffer.Value = value; }
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public YuriGarciaSmartMoneyStrategy()
	{
		_zoneLookback = Param(nameof(ZoneLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Zone Lookback", "Lookback for high/low zone", "General");

		_zoneBuffer = Param(nameof(ZoneBuffer), 0.002m)
			.SetDisplay("Zone Buffer", "Buffer percent", "General");

		_stopPercent = Param(nameof(StopPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percentage", "Risk");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("RRR", "Risk reward ratio", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_prevHigh = 0;
		_prevLow = 0;
		_prevBull = false;
		_prevBear = false;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = ZoneLookback };
		var lowest = new Lowest { Length = ZoneLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highZone, decimal lowZone)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevHigh = highZone;
			_prevLow = lowZone;
			_isReady = true;
			return;
		}

		var top = _prevHigh * (1 + ZoneBuffer);
		var bottom = _prevLow * (1 - ZoneBuffer);

		var isBull = candle.ClosePrice > candle.OpenPrice;
		var isBear = candle.ClosePrice < candle.OpenPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var pullLong = isBull && _prevBear && candle.LowPrice <= candle.OpenPrice - body / 2m;
		var pullShort = isBear && _prevBull && candle.HighPrice >= candle.ClosePrice + body / 2m;
		_prevBull = isBull;
		_prevBear = isBear;

		var nearSupport = candle.ClosePrice <= bottom * 1.02m;
		var nearResistance = candle.ClosePrice >= top * 0.98m;

		if (nearSupport && pullLong && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (nearResistance && pullShort && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}

		if (Position > 0 && _entryPrice > 0)
		{
			var stop = _entryPrice * (1 - StopPercent / 100m);
			var target = _entryPrice * (1 + StopPercent * RiskReward / 100m);
			if (candle.ClosePrice <= stop || candle.ClosePrice >= target)
				SellMarket();
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var stop = _entryPrice * (1 + StopPercent / 100m);
			var target = _entryPrice * (1 - StopPercent * RiskReward / 100m);
			if (candle.ClosePrice >= stop || candle.ClosePrice <= target)
				BuyMarket();
		}

		_prevHigh = highZone;
		_prevLow = lowZone;
	}
}
