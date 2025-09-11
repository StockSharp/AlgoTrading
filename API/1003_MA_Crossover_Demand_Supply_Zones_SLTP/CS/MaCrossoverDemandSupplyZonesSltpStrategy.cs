using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with demand and supply zones.
/// Long when short SMA crosses above long SMA near a demand zone.
/// Short when short SMA crosses below long SMA near a supply zone.
/// Exits with fixed percent stop loss and take profit.
/// </summary>
public class MaCrossoverDemandSupplyZonesSltpStrategy : Strategy
{
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<int> _zoneLookback;
	private readonly StrategyParam<int> _zoneStrength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Lowest _lowest = null!;
	private Highest _highest = null!;
	private bool _initialized;
	private decimal _prevShort;
	private decimal _prevLong;
	private decimal? _demandZone;
	private decimal? _supplyZone;
	private readonly Queue<ICandleMessage> _recentCandles = new();

	/// <summary>
	/// Short SMA length.
	/// </summary>
	public int ShortMaLength { get => _shortMaLength.Value; set => _shortMaLength.Value = value; }

	/// <summary>
	/// Long SMA length.
	/// </summary>
	public int LongMaLength { get => _longMaLength.Value; set => _longMaLength.Value = value; }

	/// <summary>
	/// Lookback period for zone detection.
	/// </summary>
	public int ZoneLookback { get => _zoneLookback.Value; set => _zoneLookback.Value = value; }

	/// <summary>
	/// Strength in bars for zones.
	/// </summary>
	public int ZoneStrength { get => _zoneStrength.Value; set => _zoneStrength.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public MaCrossoverDemandSupplyZonesSltpStrategy()
	{
		_shortMaLength = Param(nameof(ShortMaLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Short MA", "Short moving average length", "Indicators");

		_longMaLength = Param(nameof(LongMaLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("Long MA", "Long moving average length", "Indicators");

		_zoneLookback = Param(nameof(ZoneLookback), 50)
		.SetGreaterThanZero()
		.SetDisplay("Zone Lookback", "Bars for zone search", "Zones");

		_zoneStrength = Param(nameof(ZoneStrength), 2)
		.SetGreaterThanZero()
		.SetDisplay("Zone Strength", "Bars back for zone origin", "Zones");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles for calculations", "General");
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
		_initialized = false;
		_prevShort = 0m;
		_prevLong = 0m;
		_demandZone = null;
		_supplyZone = null;
		_recentCandles.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var shortSma = new SimpleMovingAverage { Length = ShortMaLength };
		var longSma = new SimpleMovingAverage { Length = LongMaLength };
		_lowest = new Lowest { Length = ZoneLookback };
		_highest = new Highest { Length = ZoneLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(shortSma, longSma, _lowest, _highest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortSma);
			DrawIndicator(area, longSma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortMa, decimal longMa, decimal lowest, decimal highest)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_recentCandles.Enqueue(candle);
		if (_recentCandles.Count > ZoneStrength + 1)
		_recentCandles.Dequeue();

		if (!_initialized)
		{
			_prevShort = shortMa;
			_prevLong = longMa;
			_initialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_recentCandles.Count > ZoneStrength)
		{
			var zoneCandle = _recentCandles.Peek();

			if (_lowest.IsFormed && lowest == zoneCandle.LowPrice && zoneCandle.ClosePrice > zoneCandle.OpenPrice)
			_demandZone = zoneCandle.LowPrice;

			if (_highest.IsFormed && highest == zoneCandle.HighPrice && zoneCandle.ClosePrice < zoneCandle.OpenPrice)
			_supplyZone = zoneCandle.HighPrice;
		}

		var isNearDemand = _demandZone is decimal dz && candle.ClosePrice <= dz * 1.01m;
		var isNearSupply = _supplyZone is decimal sz && candle.ClosePrice >= sz * 0.99m;

		if (Position == 0)
		{
			var crossover = _prevShort <= _prevLong && shortMa > longMa;
			var crossunder = _prevShort >= _prevLong && shortMa < longMa;

			if (crossover && isNearDemand)
			BuyMarket();
			else if (crossunder && isNearSupply)
			SellMarket();
		}
		else if (Position > 0)
		{
			var entry = PositionPrice;
			var stop = entry * (1 - StopLossPercent / 100m);
			var take = entry * (1 + TakeProfitPercent / 100m);

			if (candle.ClosePrice <= stop || candle.ClosePrice >= take)
			SellMarket(Position);
		}
		else
		{
			var entry = PositionPrice;
			var stop = entry * (1 + StopLossPercent / 100m);
			var take = entry * (1 - TakeProfitPercent / 100m);

			if (candle.ClosePrice >= stop || candle.ClosePrice <= take)
			BuyMarket(Math.Abs(Position));
		}

		_prevShort = shortMa;
		_prevLong = longMa;
	}
}
