using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold volume-based entry strategy.
/// Buys when two consecutive bullish volume bars exceed the volume moving average.
/// Takes profit at a fixed move from entry price.
/// </summary>
public class GoldVolumeBasedEntryStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeMaPeriod;
	private readonly StrategyParam<decimal> _targetMove;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevVolume;
	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// Volume moving average period.
	/// </summary>
	public int VolumeMaPeriod
	{
		get => _volumeMaPeriod.Value;
		set => _volumeMaPeriod.Value = value;
	}

	/// <summary>
	/// Profit target in asset currency.
	/// </summary>
	public decimal TargetMove
	{
		get => _targetMove.Value;
		set => _targetMove.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GoldVolumeBasedEntryStrategy()
	{
		_volumeMaPeriod = Param(nameof(VolumeMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Period", "Period for volume moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_targetMove = Param(nameof(TargetMove), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Target Move", "Profit target in asset currency", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_prevVolume = 0m;
		_prevOpen = 0m;
		_prevClose = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var volumeSma = new SMA { Length = VolumeMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(volumeSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, volumeSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal volMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var firstGreenVolume = _hasPrev && _prevVolume > volMa && _prevClose > _prevOpen;
		var secondGreenVolume = candle.TotalVolume > volMa && candle.ClosePrice > candle.OpenPrice && (!_hasPrev || candle.TotalVolume > _prevVolume);

		if (firstGreenVolume && secondGreenVolume && IsFormedAndOnlineAndAllowTrading() && Position <= 0)
		{
			BuyMarket();
			SellLimit(candle.ClosePrice + TargetMove);
		}

		_prevVolume = candle.TotalVolume;
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
