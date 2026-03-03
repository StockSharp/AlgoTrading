using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR Range strategy.
/// Enters long when price moves up by at least ATR over N candles,
/// enters short when price moves down by at least ATR over N candles.
/// </summary>
public class AtrRangeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _nBarsAgoPrice;
	private int _barCounter;
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
	/// ATR Period.
	/// </summary>
	public int ATRPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Lookback Period (N candles for price movement).
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
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
	/// Initialize the ATR Range strategy.
	/// </summary>
	public AtrRangeStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 10);

		_atrPeriod = Param(nameof(ATRPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 28, 7);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 5)
			.SetDisplay("Lookback Period", "Number of candles to measure price movement", "Entry")
			.SetOptimize(3, 10, 1);

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
		_nBarsAgoPrice = default;
		_barCounter = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_nBarsAgoPrice = 0;
		_barCounter = 0;
		_cooldown = 0;

		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var atr = new AverageTrueRange { Length = ATRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barCounter++;

		if (_barCounter == 1 || _barCounter % LookbackPeriod == 1)
		{
			_nBarsAgoPrice = candle.ClosePrice;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Check at end of each lookback period
		if (_barCounter % LookbackPeriod == 0)
		{
			var priceMovement = candle.ClosePrice - _nBarsAgoPrice;
			var absMovement = Math.Abs(priceMovement);

			if (absMovement >= atrValue)
			{
				if (Position == 0 && priceMovement > 0)
				{
					BuyMarket();
					_cooldown = CooldownBars;
				}
				else if (Position == 0 && priceMovement < 0)
				{
					SellMarket();
					_cooldown = CooldownBars;
				}
			}
		}

		// Exit logic: price crosses MA
		if (Position > 0 && candle.ClosePrice < maValue)
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
