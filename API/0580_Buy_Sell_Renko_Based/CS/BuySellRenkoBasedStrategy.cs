using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on candle direction changes.
/// Buys when the candle close crosses above its open and sells on the opposite cross.
/// </summary>
public class BuySellRenkoBasedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BuySellRenkoBasedStrategy"/>.
	/// </summary>
	public BuySellRenkoBasedStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_hasPrev)
		{
			var prevBull = _prevClose > _prevOpen;
			var currBull = candle.ClosePrice > candle.OpenPrice;

			if (!prevBull && currBull && Position <= 0)
			{
				BuyMarket();
			}
			else if (prevBull && !currBull && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
