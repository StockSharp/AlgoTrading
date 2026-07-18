using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Divergence strategy.
/// Long entry: Price falls but volume increases (possible accumulation).
/// Short entry: Price rises but volume increases (possible distribution).
/// Exit: Price crosses MA.
/// </summary>
public class VolumeDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _previousClose;
	private decimal _previousVolume;
	private int _cooldown;

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Initialize <see cref="VolumeDivergenceStrategy"/>.
	/// </summary>
	public VolumeDivergenceStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average", "Indicators")
			.SetOptimize(10, 50, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_previousClose = default;
		_previousVolume = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousClose = 0;
		_previousVolume = 0;
		_cooldown = 0;

		var ma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousClose == 0)
		{
			_previousClose = candle.ClosePrice;
			_previousVolume = candle.TotalVolume;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousClose = candle.ClosePrice;
			_previousVolume = candle.TotalVolume;
			return;
		}

		var priceDown = candle.ClosePrice < _previousClose;
		var priceUp = candle.ClosePrice > _previousClose;
		var volumeUp = candle.TotalVolume > _previousVolume;

		var bullishDivergence = priceDown && volumeUp;
		var bearishDivergence = priceUp && volumeUp;

		if (Position == 0)
		{
			if (bullishDivergence && candle.ClosePrice < maValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (bearishDivergence && candle.ClosePrice > maValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && candle.ClosePrice < maValue && !bullishDivergence)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > maValue && !bearishDivergence)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_previousClose = candle.ClosePrice;
		_previousVolume = candle.TotalVolume;
	}
}
