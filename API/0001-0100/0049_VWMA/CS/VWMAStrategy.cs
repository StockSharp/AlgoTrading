using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Weighted Moving Average (VWMA) Strategy.
/// Long entry: Price crosses above VWMA.
/// Short entry: Price crosses below VWMA.
/// </summary>
public class VWMAStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _previousClosePrice;
	private decimal _previousVWMA;
	private int _cooldown;

	/// <summary>
	/// VWMA Period.
	/// </summary>
	public int VWMAPeriod
	{
		get => _vwmaPeriod.Value;
		set => _vwmaPeriod.Value = value;
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
	/// Initialize <see cref="VWMAStrategy"/>.
	/// </summary>
	public VWMAStrategy()
	{
		_vwmaPeriod = Param(nameof(VWMAPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("VWMA Period", "Period for Volume Weighted Moving Average", "Indicators")
			.SetOptimize(5, 30, 5);

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
		_previousClosePrice = default;
		_previousVWMA = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousClosePrice = 0;
		_previousVWMA = 0;
		_cooldown = 0;

		var vwma = new VolumeWeightedMovingAverage { Length = VWMAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(vwma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwmaPrice)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousClosePrice == 0)
		{
			_previousClosePrice = candle.ClosePrice;
			_previousVWMA = vwmaPrice;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousClosePrice = candle.ClosePrice;
			_previousVWMA = vwmaPrice;
			return;
		}

		var crossoverUp = _previousClosePrice <= _previousVWMA && candle.ClosePrice > vwmaPrice;
		var crossoverDown = _previousClosePrice >= _previousVWMA && candle.ClosePrice < vwmaPrice;

		if (Position == 0 && crossoverUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && crossoverDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && crossoverDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && crossoverUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_previousClosePrice = candle.ClosePrice;
		_previousVWMA = vwmaPrice;
	}
}
