namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that combines moving average and volume indicators.
/// Buys on MA crossover with volume confirmation, sells on reverse crossover.
/// </summary>
public class MaVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevClose;
	private decimal _prevSma;
	private bool _hasPrev;
	private decimal _prevVolume;
	private int _cooldown;

	/// <summary>
	/// Data type for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for moving average calculation.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Volume threshold multiplier for volume confirmation.
	/// </summary>
	public decimal VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaVolumeStrategy"/>.
	/// </summary>
	public MaVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "Period for moving average calculation", "MA Settings");

		_volumeThreshold = Param(nameof(VolumeThreshold), 1.2m)
			.SetDisplay("Volume Threshold", "Volume threshold multiplier", "Volume Settings");

		_cooldownBars = Param(nameof(CooldownBars), 150)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_prevClose = 0;
		_prevSma = 0;
		_hasPrev = false;
		_prevVolume = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var volumeSma = new SimpleMovingAverage { Length = 20 };
		var priceSma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);

		// Use volumeSma to track volume average via separate bind
		subscription.Bind(priceSma, OnProcess);
		subscription
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, priceSma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var vol = candle.TotalVolume;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevClose = close;
			_prevSma = smaValue;
			_prevVolume = vol;
			_hasPrev = true;
			return;
		}

		// Volume confirmation: current volume is above threshold * previous volume
		var volumeOk = _prevVolume > 0 && vol > _prevVolume * VolumeThreshold;

		if (_hasPrev)
		{
			// Price crosses above MA with volume - buy
			if (_prevClose <= _prevSma && close > smaValue && volumeOk && Position == 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			// Price crosses below MA with volume - sell
			else if (_prevClose >= _prevSma && close < smaValue && volumeOk && Position == 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			// Exit long on MA cross down
			else if (_prevClose >= _prevSma && close < smaValue && Position > 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			// Exit short on MA cross up
			else if (_prevClose <= _prevSma && close > smaValue && Position < 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		_prevClose = close;
		_prevSma = smaValue;
		_prevVolume = vol;
		_hasPrev = true;
	}
}
