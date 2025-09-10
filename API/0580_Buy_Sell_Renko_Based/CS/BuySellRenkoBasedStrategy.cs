using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on renko brick direction changes.
/// Buys when the renko close crosses above its open and sells on the opposite cross.
/// </summary>
public class BuySellRenkoBasedStrategy : Strategy
{
	private readonly StrategyParam<int> _renkoAtrLength;
	private DataType _renkoType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// ATR period used to calculate renko brick size.
	/// </summary>
	public int RenkoAtrLength
	{
		get => _renkoAtrLength.Value;
		set => _renkoAtrLength.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BuySellRenkoBasedStrategy"/>.
	/// </summary>
	public BuySellRenkoBasedStrategy()
	{
		_renkoAtrLength = Param(nameof(RenkoAtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for renko brick size", "Renko")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		_renkoType ??= DataType.Create(typeof(RenkoCandleMessage), new RenkoCandleArg
		{
			BuildFrom = RenkoBuildFrom.Atr,
			Length = RenkoAtrLength
		});

		return [(Security, _renkoType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(_renkoType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
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
