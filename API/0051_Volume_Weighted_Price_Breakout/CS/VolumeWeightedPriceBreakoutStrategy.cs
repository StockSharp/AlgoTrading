using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Weighted Price Breakout Strategy.
/// Long entry: Price rises above VWMA.
/// Short entry: Price falls below VWMA.
/// Exit: Price crosses MA in the opposite direction.
/// </summary>
public class VolumeWeightedPriceBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _vwapPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

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
	/// VWAP Period.
	/// </summary>
	public int VWAPPeriod
	{
		get => _vwapPeriod.Value;
		set => _vwapPeriod.Value = value;
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
	/// Initialize <see cref="VolumeWeightedPriceBreakoutStrategy"/>.
	/// </summary>
	public VolumeWeightedPriceBreakoutStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average", "Indicators")
			.SetOptimize(10, 50, 10);

		_vwapPeriod = Param(nameof(VWAPPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("VWAP Period", "Period for VWMA", "Indicators")
			.SetOptimize(10, 30, 5);

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
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cooldown = 0;

		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var vwma = new VolumeWeightedMovingAverage { Length = VWAPPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, vwma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, vwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal vwmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (Position == 0)
		{
			if (candle.ClosePrice > vwmaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (candle.ClosePrice < vwmaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && candle.ClosePrice < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
