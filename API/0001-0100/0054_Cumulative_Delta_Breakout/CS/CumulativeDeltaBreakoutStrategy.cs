using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cumulative Delta Breakout strategy.
/// Estimates delta from candle direction and volume.
/// Long: Cumulative delta rising and price above SMA.
/// Short: Cumulative delta falling and price below SMA.
/// </summary>
public class CumulativeDeltaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _cumulativeDelta;
	private decimal _prevDelta;
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
	/// Initialize <see cref="CumulativeDeltaBreakoutStrategy"/>.
	/// </summary>
	public CumulativeDeltaBreakoutStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators")
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
		_cumulativeDelta = default;
		_prevDelta = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cumulativeDelta = 0;
		_prevDelta = 0;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Estimate delta from candle: bullish candle adds volume, bearish subtracts
		var delta = candle.ClosePrice >= candle.OpenPrice
			? candle.TotalVolume
			: -candle.TotalVolume;
		_cumulativeDelta += delta;

		if (_prevDelta == 0)
		{
			_prevDelta = _cumulativeDelta;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevDelta = _cumulativeDelta;
			return;
		}

		var deltaRising = _cumulativeDelta > _prevDelta;

		if (Position == 0)
		{
			if (deltaRising && candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (!deltaRising && candle.ClosePrice < smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && !deltaRising)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && deltaRising)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevDelta = _cumulativeDelta;
	}
}
