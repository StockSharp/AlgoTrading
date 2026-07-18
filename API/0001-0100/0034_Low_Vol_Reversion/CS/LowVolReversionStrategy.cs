using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on mean reversion during periods of low volatility.
/// It identifies periods of low ATR and opens positions when price
/// deviates from its moving average, expecting a return to the mean.
/// </summary>
public class LowVolReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrLookbackPeriod;
	private readonly StrategyParam<decimal> _atrThresholdPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _avgAtr;
	private int _lookbackCounter;
	private int _cooldown;

	/// <summary>
	/// Period for Moving Average calculation.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for ATR average calculation.
	/// </summary>
	public int AtrLookbackPeriod
	{
		get => _atrLookbackPeriod.Value;
		set => _atrLookbackPeriod.Value = value;
	}

	/// <summary>
	/// ATR threshold as percentage of average ATR.
	/// </summary>
	public decimal AtrThresholdPercent
	{
		get => _atrThresholdPercent.Value;
		set => _atrThresholdPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
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
	/// Initialize the Low Volatility Reversion strategy.
	/// </summary>
	public LowVolReversionStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 21, 7);

		_atrLookbackPeriod = Param(nameof(AtrLookbackPeriod), 20)
			.SetDisplay("ATR Lookback", "Lookback period for ATR average calculation", "Indicators")
			.SetOptimize(10, 50, 10);

		_atrThresholdPercent = Param(nameof(AtrThresholdPercent), 80m)
			.SetDisplay("ATR Threshold %", "ATR threshold as percentage of average ATR", "Entry")
			.SetOptimize(30m, 90m, 10m);

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
		_avgAtr = default;
		_lookbackCounter = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_avgAtr = 0;
		_lookbackCounter = 0;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Gather ATR values for average calculation
		if (_lookbackCounter < AtrLookbackPeriod)
		{
			if (_lookbackCounter == 0)
				_avgAtr = atrValue;
			else
				_avgAtr = (_avgAtr * _lookbackCounter + atrValue) / (_lookbackCounter + 1);

			_lookbackCounter++;
			return;
		}
		else
		{
			_avgAtr = (_avgAtr * (AtrLookbackPeriod - 1) + atrValue) / AtrLookbackPeriod;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Check if we're in a low volatility period
		decimal atrThreshold = _avgAtr * (AtrThresholdPercent / 100);
		bool isLowVolatility = atrValue < atrThreshold;

		if (Position == 0 && isLowVolatility)
		{
			if (candle.ClosePrice < smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (candle.ClosePrice > smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice > smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice < smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
	}
}
