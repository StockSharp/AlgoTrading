using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Channel Strategy by Kevin Davey.
/// Enters long when price closes below the lower band and short when it closes above the upper band.
/// </summary>
public class KeltnerChannelByKevinDaveyStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _entriesExecuted;
	private int _barsSinceSignal;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Maximum entries per run.
	/// </summary>
	public int MaxEntries { get => _maxEntries.Value; set => _maxEntries.Value = value; }

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public KeltnerChannelByKevinDaveyStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 10)
			.SetDisplay("EMA Period", "Period for Exponential Moving Average", "Indicators")
			
			.SetOptimize(5, 30, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for Average True Range", "Indicators")
			
			.SetOptimize(5, 30, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.6m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR to form channel", "Indicators")
			
			.SetOptimize(1m, 3m, 0.1m);

		_maxEntries = Param(nameof(MaxEntries), 45)
			.SetDisplay("Max Entries", "Maximum entries per run", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_entriesExecuted = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entriesExecuted = 0;
		_barsSinceSignal = CooldownBars;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		var upperBand = emaValue + AtrMultiplier * atrValue;
		var lowerBand = emaValue - AtrMultiplier * atrValue;

		if (_barsSinceSignal < CooldownBars)
			return;

		if (Position > 0 && candle.ClosePrice >= emaValue)
		{
			SellMarket();
			_barsSinceSignal = 0;
		}
		else if (Position < 0 && candle.ClosePrice <= emaValue)
		{
			BuyMarket();
			_barsSinceSignal = 0;
		}
		else if (Position == 0 && _entriesExecuted < MaxEntries && _barsSinceSignal >= CooldownBars)
		{
			if (candle.ClosePrice < lowerBand)
			{
				BuyMarket();
				_entriesExecuted++;
				_barsSinceSignal = 0;
			}
			else if (candle.ClosePrice > upperBand)
			{
				SellMarket();
				_entriesExecuted++;
				_barsSinceSignal = 0;
			}
		}
	}
}
