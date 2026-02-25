using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Accumulation strategy: buys at support levels (recent lows) and sells at new highs.
/// Uses Lowest indicator to detect support and Highest for resistance breakout exits.
/// </summary>
public class UnmitigatedLevelsAccumulationStrategy : Strategy
{
	private readonly StrategyParam<int> _lowLength;
	private readonly StrategyParam<int> _highLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLow;
	private decimal _prevHigh;

	public int LowLength { get => _lowLength.Value; set => _lowLength.Value = value; }
	public int HighLength { get => _highLength.Value; set => _highLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UnmitigatedLevelsAccumulationStrategy()
	{
		_lowLength = Param(nameof(LowLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Low Length", "Lowest period for support", "General");

		_highLength = Param(nameof(HighLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("High Length", "Highest period for resistance", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLow = 0;
		_prevHigh = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var lowest = new Lowest { Length = LowLength };
		var highest = new Highest { Length = HighLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(lowest, highest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, lowest);
			DrawIndicator(area, highest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lowValue, decimal highValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevLow == 0 || _prevHigh == 0)
		{
			_prevLow = lowValue;
			_prevHigh = highValue;
			return;
		}

		// Buy when price bounces off support (touches lowest and recovers)
		if (candle.LowPrice <= lowValue && candle.ClosePrice > lowValue && Position <= 0)
			BuyMarket();

		// Sell when price breaks to new high and pulls back
		if (candle.HighPrice >= highValue && candle.ClosePrice < highValue && Position >= 0)
			SellMarket();

		// Exit long if price breaks below support
		if (Position > 0 && candle.ClosePrice < _prevLow * 0.99m)
			SellMarket();

		// Exit short if price breaks above resistance
		if (Position < 0 && candle.ClosePrice > _prevHigh * 1.01m)
			BuyMarket();

		_prevLow = lowValue;
		_prevHigh = highValue;
	}
}
