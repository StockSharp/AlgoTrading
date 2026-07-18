using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner width mean reversion strategy.
/// Trades contractions and expansions of Keltner Channel width around its recent average.
/// </summary>
public class KeltnerWidthMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<decimal> _widthDeviationMultiplier;
	private readonly StrategyParam<int> _widthLookbackPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;
	private decimal[] _widthHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;

	/// <summary>
	/// Period for EMA calculation.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
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
	/// Multiplier for Keltner Channel bands.
	/// </summary>
	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier for width standard deviation thresholds.
	/// </summary>
	public decimal WidthDeviationMultiplier
	{
		get => _widthDeviationMultiplier.Value;
		set => _widthDeviationMultiplier.Value = value;
	}

	/// <summary>
	/// Lookback period for width statistics.
	/// </summary>
	public int WidthLookbackPeriod
	{
		get => _widthLookbackPeriod.Value;
		set => _widthLookbackPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Cooldown bars between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes a new instance of <see cref="KeltnerWidthMeanReversionStrategy"/>.
	/// </summary>
	public KeltnerWidthMeanReversionStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Multiplier", "Multiplier for Keltner Channel bands", "Indicators");

		_widthDeviationMultiplier = Param(nameof(WidthDeviationMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Width Dev Multiplier", "Multiplier for width deviation threshold", "Strategy Parameters");

		_widthLookbackPeriod = Param(nameof(WidthLookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Width Lookback", "Lookback period for width statistics", "Strategy Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

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
		_ema = null;
		_atr = null;
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_widthHistory = new decimal[WidthLookbackPeriod];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_widthHistory = new decimal[WidthLookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_atr.IsFormed)
			return;

		var width = 2m * KeltnerMultiplier * atrValue;

		_widthHistory[_currentIndex] = width;
		_currentIndex = (_currentIndex + 1) % WidthLookbackPeriod;

		if (_filledCount < WidthLookbackPeriod)
			_filledCount++;

		if (_filledCount < WidthLookbackPeriod)
			return;

		var avgWidth = 0m;
		var sumSq = 0m;

		for (var i = 0; i < WidthLookbackPeriod; i++)
			avgWidth += _widthHistory[i];

		avgWidth /= WidthLookbackPeriod;

		for (var i = 0; i < WidthLookbackPeriod; i++)
		{
			var diff = _widthHistory[i] - avgWidth;
			sumSq += diff * diff;
		}

		var stdWidth = (decimal)Math.Sqrt((double)(sumSq / WidthLookbackPeriod));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var lowerThreshold = avgWidth - WidthDeviationMultiplier * stdWidth;
		var upperThreshold = avgWidth + WidthDeviationMultiplier * stdWidth;

		if (Position == 0)
		{
			if (width < lowerThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (width > upperThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && width >= avgWidth)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && width <= avgWidth)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
