using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD and Bollinger Bands indicators
/// </summary>
public class MacdBollingerStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private int _cooldown;

	/// <summary>
	/// MACD fast EMA period
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal line period
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// ATR period for stop-loss
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public MacdBollingerStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetRange(5, 20)
			.SetDisplay("MACD Fast", "MACD fast EMA period", "MACD")
			;

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetRange(15, 40)
			.SetDisplay("MACD Slow", "MACD slow EMA period", "MACD")
			;

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetRange(5, 15)
			.SetDisplay("MACD Signal", "MACD signal line period", "MACD")
			;

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Bollinger")
			;

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetRange(1.0m, 3.0m)
			.SetDisplay("Bollinger Deviation", "Bollinger Bands standard deviation multiplier", "Bollinger")
			;

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(7, 28)
			.SetDisplay("ATR Period", "ATR period for stop-loss calculation", "Risk Management")
			;

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetRange(1m, 4m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
			;

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_cooldown = 0;
	}

	/// <inheritdoc />
		protected override void OnStarted2(DateTime time)
		{
				base.OnStarted2(time);

		// Initialize indicators
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				LongMa = { Length = MacdSlow },
				ShortMa = { Length = MacdFast },
			},
			SignalMa = { Length = MacdSignal }
		};

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var atr = new AverageTrueRange { Length = AtrPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, macd, atr, ProcessIndicators)
			.Start();
		
		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		var bollingerTyped = (BollingerBandsValue)bollingerValue;
		var upperBand = bollingerTyped.UpBand;
		var lowerBand = bollingerTyped.LowBand;
		var middleBand = bollingerTyped.MovingAverage;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd ?? 0m;
		var signal = macdTyped.Signal ?? 0m;

		var price = candle.ClosePrice;

		// Trading logic:
		// Long: MACD > Signal && Price < BB_lower (trend up with oversold conditions)
		// Short: MACD < Signal && Price > BB_upper (trend down with overbought conditions)
		
		var macdCrossOver = macd > signal;
		if (_cooldown > 0)
			_cooldown--;

		if (_cooldown == 0 && macdCrossOver && price < middleBand * 0.999m && Position <= 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (_cooldown == 0 && !macdCrossOver && price > middleBand * 1.001m && Position >= 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit conditions
		else if (Position > 0 && !macdCrossOver)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && macdCrossOver)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
