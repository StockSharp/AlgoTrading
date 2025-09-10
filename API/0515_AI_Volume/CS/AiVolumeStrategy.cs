using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AI Volume Strategy - trades volume spikes in trend direction.
/// </summary>
public class AiVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _volumeEmaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _exitBars;

	private ExponentialMovingAverage _priceEma;
	private ExponentialMovingAverage _volumeEma;
	private int _barsInPosition;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume EMA length.
	/// </summary>
	public int VolumeEmaLength
	{
		get => _volumeEmaLength.Value;
		set => _volumeEmaLength.Value = value;
	}

	/// <summary>
	/// Multiplier for volume spike detection.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Bars to hold position before exit.
	/// </summary>
	public int ExitBars
	{
		get => _exitBars.Value;
		set => _exitBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AiVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_volumeEmaLength = Param(nameof(VolumeEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume EMA Length", "Length for volume EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Multiplier for volume spike detection", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_exitBars = Param(nameof(ExitBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Exit position after this many bars", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);
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
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceEma = new ExponentialMovingAverage { Length = 50 };
		_volumeEma = new ExponentialMovingAverage { Length = VolumeEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_priceEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _priceEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal priceEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeEmaValue = _volumeEma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!_priceEma.IsFormed || !_volumeEma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var volumeSpike = candle.TotalVolume > volumeEmaValue * VolumeMultiplier;
		var trendUp = candle.ClosePrice > priceEmaValue;
		var trendDown = candle.ClosePrice < priceEmaValue;
		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		if (volumeSpike)
		{
			if (trendUp && isBullish && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsInPosition = 0;
			}
			else if (trendDown && isBearish && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsInPosition = 0;
			}
		}

		if (Position != 0)
		{
			_barsInPosition++;
			if (_barsInPosition >= ExitBars)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_barsInPosition = 0;
			}
		}
	}
}

