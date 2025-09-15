using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Builds renko candles from tick data and trades on direction changes.
/// </summary>
public class RenkoChartFromTicksStrategy : Strategy
{
	private readonly StrategyParam<decimal> _brickSize;
	private readonly StrategyParam<decimal> _volume;

	private DataType _renkoType;
	private bool? _prevUp;

	/// <summary>
	/// Renko brick size in price units.
	/// </summary>
	public decimal BrickSize
	{
		get => _brickSize.Value;
		set => _brickSize.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="RenkoChartFromTicksStrategy"/>.
	/// </summary>
	public RenkoChartFromTicksStrategy()
	{
		_brickSize = Param(nameof(BrickSize), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Brick Size", "Renko brick size", "General")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		_renkoType ??= DataType.Create(typeof(RenkoCandleMessage), new RenkoCandleArg
		{
			BoxSize = BrickSize,
			BuildFrom = RenkoBuildFrom.Ticks
		});

		return [(Security, _renkoType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(_renkoType);
		subscription.Bind(ProcessCandle).Start();

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

		var isUp = candle.OpenPrice < candle.ClosePrice;

		if (_prevUp != null && _prevUp != isUp)
		{
			if (isUp && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
			}
			else if (!isUp && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
			}
		}

		_prevUp = isUp;
	}
}
