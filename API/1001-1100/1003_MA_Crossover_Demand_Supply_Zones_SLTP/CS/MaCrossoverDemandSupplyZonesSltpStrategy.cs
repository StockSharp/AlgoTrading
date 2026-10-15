using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA crossover with demand and supply zones and percent stop loss / take profit.
/// The demand zone is the lowest low and the supply zone the highest high of the last ZoneLookback candles. Price is near a zone
/// when the close is within ZoneStrength percent of it. A short SMA crossing above the long SMA near the demand zone goes long, a cross
/// below near the supply zone goes short, reversing an opposite position. Positions exit on percent stop loss and take profit.
/// </summary>
public class MaCrossoverDemandSupplyZonesSltpStrategy : Strategy
{
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<int> _zoneLookback;
	private readonly StrategyParam<decimal> _zoneStrength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevShort;
	private decimal? _prevLong;

	/// <summary>
	/// Short SMA period.
	/// </summary>
	public int ShortMaLength
	{
		get => _shortMaLength.Value;
		set => _shortMaLength.Value = value;
	}

	/// <summary>
	/// Long SMA period.
	/// </summary>
	public int LongMaLength
	{
		get => _longMaLength.Value;
		set => _longMaLength.Value = value;
	}

	/// <summary>
	/// Candles used to find demand and supply zones.
	/// </summary>
	public int ZoneLookback
	{
		get => _zoneLookback.Value;
		set => _zoneLookback.Value = value;
	}

	/// <summary>
	/// Distance from a zone, in percent, that counts as near.
	/// </summary>
	public decimal ZoneStrength
	{
		get => _zoneStrength.Value;
		set => _zoneStrength.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
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
	public MaCrossoverDemandSupplyZonesSltpStrategy()
	{
		_shortMaLength = Param(nameof(ShortMaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short MA", "Short SMA period", "Indicators");

		_longMaLength = Param(nameof(LongMaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long MA", "Long SMA period", "Indicators");

		_zoneLookback = Param(nameof(ZoneLookback), 50)
			.SetGreaterThanZero()
			.SetDisplay("Zone Lookback", "Candles used to find demand and supply zones", "Zones");

		_zoneStrength = Param(nameof(ZoneStrength), 2m)
			.SetNotNegative()
			.SetDisplay("Zone Strength", "Distance from a zone in percent that counts as near", "Zones");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

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
		_prevShort = null;
		_prevLong = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = null;
		_prevLong = null;

		var shortMa = new SimpleMovingAverage { Length = ShortMaLength };
		var longMa = new SimpleMovingAverage { Length = LongMaLength };
		var highest = new Highest { Length = ZoneLookback };
		var lowest = new Lowest { Length = ZoneLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(shortMa, longMa, highest, lowest, ProcessCandle)
			.Start();

		StartProtection(
			TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, longMa);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue shortValue, IIndicatorValue longValue, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!shortValue.IsFormed || !longValue.IsFormed || !highestValue.IsFormed || !lowestValue.IsFormed)
			return;

		var shortMa = shortValue.GetValue<decimal>();
		var longMa = longValue.GetValue<decimal>();
		var supply = highestValue.GetValue<decimal>();
		var demand = lowestValue.GetValue<decimal>();

		var prevShort = _prevShort;
		var prevLong = _prevLong;
		_prevShort = shortMa;
		_prevLong = longMa;

		if (prevShort is not decimal ps || prevLong is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var nearDemand = close <= demand * (1m + ZoneStrength / 100m);
		var nearSupply = close >= supply * (1m - ZoneStrength / 100m);

		var crossUp = ps <= pl && shortMa > longMa;
		var crossDown = ps >= pl && shortMa < longMa;

		if (crossUp && nearDemand && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && nearSupply && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
