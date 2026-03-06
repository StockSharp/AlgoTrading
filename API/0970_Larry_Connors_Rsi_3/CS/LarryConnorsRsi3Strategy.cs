using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Connors RSI 3 strategy.
/// Combines a long-term trend filter with short-term RSI exhaustion.
/// </summary>
public class LarryConnorsRsi3Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _dropTrigger;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _rsiPrev1;
	private decimal _rsiPrev2;
	private int _entriesExecuted;
	private int _barsSinceSignal;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// RSI trigger level for drop start.
	/// </summary>
	public decimal DropTrigger
	{
		get => _dropTrigger.Value;
		set => _dropTrigger.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum entries per run.
	/// </summary>
	public int MaxEntries
	{
		get => _maxEntries.Value;
		set => _maxEntries.Value = value;
	}

	/// <summary>
	/// Minimum bars between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="LarryConnorsRsi3Strategy"/>.
	/// </summary>
	public LarryConnorsRsi3Strategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 2)
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
			
			.SetOptimize(2, 5, 1);

		_smaPeriod = Param(nameof(SmaPeriod), 200)
			.SetDisplay("SMA Period", "Period for trend SMA", "Indicators")
			
			.SetOptimize(50, 300, 50);

		_dropTrigger = Param(nameof(DropTrigger), 60m)
			.SetDisplay("Drop Trigger", "RSI level required before drop", "Strategy")
			
			.SetOptimize(50m, 70m, 5m);

		_oversoldLevel = Param(nameof(OversoldLevel), 10m)
			.SetDisplay("Oversold Level", "RSI oversold threshold", "Strategy")
			
			.SetOptimize(5m, 20m, 1m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "RSI exit threshold", "Strategy")
			
			.SetOptimize(60m, 80m, 5m);

		_maxEntries = Param(nameof(MaxEntries), 45)
			.SetDisplay("Max Entries", "Maximum entries per run", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 10000)
			.SetDisplay("Cooldown Bars", "Minimum bars between orders", "Risk");

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
		_rsiPrev1 = 0m;
		_rsiPrev2 = 0m;
		_entriesExecuted = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entriesExecuted = 0;
		_barsSinceSignal = CooldownBars;

		var sma = new SMA { Length = SmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		var hasHistory = _rsiPrev1 != 0m && _rsiPrev2 != 0m;
		var condition1 = candle.ClosePrice > smaValue;
		var condition2 = hasHistory && _rsiPrev2 > _rsiPrev1 && _rsiPrev1 > rsiValue && _rsiPrev2 > DropTrigger;
		var condition3 = rsiValue < OversoldLevel;

		if (condition1 && condition2 && condition3 && Position <= 0 && _entriesExecuted < MaxEntries && _barsSinceSignal >= CooldownBars)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
		else if (rsiValue > OverboughtLevel && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_barsSinceSignal = 0;
		}

		_rsiPrev2 = _rsiPrev1;
		_rsiPrev1 = rsiValue;
	}
}
