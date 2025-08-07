namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Heikin Ashi Strategy V2
/// </summary>
public class HeikinAshiV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<bool> _onlyLong;
	private readonly StrategyParam<bool> _useMacdFilter;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;

	public HeikinAshiV2Strategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "Heikin Ashi Candle");

		_emaPeriod = Param(nameof(EmaPeriod), 1)
			.SetDisplay("EMA Period", "Fast EMA period", "Heikin Ashi EMA");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
			.SetDisplay("Slow EMA Period", "Slow EMA period", "Slow EMA");

		_onlyLong = Param(nameof(OnlyLong), false)
			.SetDisplay("Only Long", "Show only long entries", "Strategy");

		_useMacdFilter = Param(nameof(UseMacdFilter), false)
			.SetDisplay("Use MACD Filter", "Enable MACD filter", "MACD");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	public bool OnlyLong
	{
		get => _onlyLong.Value;
		set => _onlyLong.Value = value;
	}

	public bool UseMacdFilter
	{
		get => _useMacdFilter.Value;
		set => _useMacdFilter.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, CandleType) };

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_fastEma = new ExponentialMovingAverage { Length = EmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		if (UseMacdFilter)
		{
			_macd = new()
			{
				Macd =
				{
					ShortMa = { Length = 12 },
					LongMa = { Length = 26 },
				},
				SignalMa = { Length = 9 }
			};
		}

		// Subscribe to candles
		var subscription = this.SubscribeCandles(CandleType);
		
		if (UseMacdFilter)
		{
			subscription
				.BindEx(_fastEma, _slowEma, _macd, OnProcessWithMacd)
				.Start();
		}
		else
		{
			subscription
				.BindEx(_fastEma, _slowEma, OnProcessWithoutMacd)
				.Start();
		}

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma, System.Drawing.Color.Blue);
			DrawIndicator(area, _slowEma, System.Drawing.Color.Purple);
			DrawOwnTrades(area);
		}
	}

	private void OnProcessWithMacd(ICandleMessage candle, IIndicatorValue fastEmaValue, IIndicatorValue slowEmaValue, IIndicatorValue macdValue)
	{
		ProcessCandle(candle, fastEmaValue.ToDecimal(), slowEmaValue.ToDecimal(), macdValue);
	}

	private void OnProcessWithoutMacd(ICandleMessage candle, IIndicatorValue fastEmaValue, IIndicatorValue slowEmaValue)
	{
		ProcessCandle(candle, fastEmaValue.ToDecimal(), slowEmaValue.ToDecimal(), null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue, IIndicatorValue macdValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (UseMacdFilter && !_macd.IsFormed)
			return;

		// Calculate Heikin-Ashi values
		decimal haOpen, haClose;

		if (_prevHaOpen == 0)
		{
			// First candle
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
		}
		else
		{
			// Calculate based on previous HA candle
			haOpen = (_prevHaOpen + _prevHaClose) / 2;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
		}

		// Get previous values for crossover detection
		var prevFastEma = _fastEma.GetValue(1);
		var prevSlowEma = _slowEma.GetValue(1);

		// MACD filter
		var macdFilter = true;
		if (UseMacdFilter && macdValue != null)
		{
			var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
			var macdLine = macdTyped.Macd;
			var signalLine = macdTyped.Signal;

			// For long: MACD > Signal, for short: MACD < Signal
			macdFilter = Position <= 0 ? macdLine > signalLine : macdLine < signalLine;
		}

		// Check for crossovers
		var goLong = fastEmaValue > slowEmaValue && prevFastEma <= prevSlowEma && macdFilter;
		var goShort = fastEmaValue < slowEmaValue && prevFastEma >= prevSlowEma && macdFilter;

		// Execute trades
		if (goLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!OnlyLong && goShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (OnlyLong && goShort && Position > 0)
		{
			// Close long position
			ClosePosition();
		}

		// Store current HA values for next candle
		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
	}
}