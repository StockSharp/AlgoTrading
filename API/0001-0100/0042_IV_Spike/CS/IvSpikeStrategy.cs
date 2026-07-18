using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// IV Spike strategy based on implied volatility spikes.
/// Enters long when IV increases above threshold and price is below MA,
/// or short when IV increases and price is above MA.
/// </summary>
public class IvSpikeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _ivPeriod;
	private readonly StrategyParam<decimal> _ivSpikeThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _previousIV;
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
	/// IV Period (for historical volatility calculation).
	/// </summary>
	public int IVPeriod
	{
		get => _ivPeriod.Value;
		set => _ivPeriod.Value = value;
	}

	/// <summary>
	/// IV Spike Threshold (minimum IV increase for signal).
	/// </summary>
	public decimal IVSpikeThreshold
	{
		get => _ivSpikeThreshold.Value;
		set => _ivSpikeThreshold.Value = value;
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
	/// Initialize the IV Spike strategy.
	/// </summary>
	public IvSpikeStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 10);

		_ivPeriod = Param(nameof(IVPeriod), 20)
			.SetDisplay("IV Period", "Period for volatility calculation", "Indicators")
			.SetOptimize(10, 30, 5);

		_ivSpikeThreshold = Param(nameof(IVSpikeThreshold), 1.5m)
			.SetDisplay("IV Spike Threshold", "Minimum IV increase multiplier", "Entry")
			.SetOptimize(1.2m, 2.0m, 0.1m);

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
		_previousIV = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousIV = 0;
		_cooldown = 0;

		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var hv = new StandardDeviation { Length = IVPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, hv, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal ivValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousIV == 0 && ivValue > 0)
		{
			_previousIV = ivValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousIV = ivValue;
			return;
		}

		var ivChange = _previousIV > 0 ? ivValue / _previousIV : 0;

		if (Position == 0 && ivChange >= IVSpikeThreshold)
		{
			if (candle.ClosePrice < maValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (candle.ClosePrice > maValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && ivValue < _previousIV)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && ivValue < _previousIV)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_previousIV = ivValue;
	}
}
