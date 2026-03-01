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
/// Strategy that trades on renko brick direction changes.
/// </summary>
public class RenkoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _boxSize;
	private DataType _renkoType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// Renko brick size in price units.
	/// </summary>
	public decimal BoxSize
	{
		get => _boxSize.Value;
		set => _boxSize.Value = value;
	}


	/// <summary>
	/// Initializes <see cref="RenkoStrategy"/>.
	/// </summary>
	public RenkoStrategy()
	{
		_boxSize = Param(nameof(BoxSize), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Box Size", "Renko brick size", "Renko")
			
			.SetOptimize(5m, 20m, 1m);

	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		_renkoType ??= DataType.Create(typeof(RenkoCandleMessage), new Unit(BoxSize));

		return [(Security, _renkoType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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
				BuyMarket(Volume);
			}
			else if (prevBull && !currBull && Position >= 0)
			{
				SellMarket(Volume);
			}
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
