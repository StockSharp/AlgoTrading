using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VIX Spike Strategy.
/// Uses Bollinger Bands to detect price spikes and trades mean reversion.
/// </summary>
public class VixSpikeStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _exitPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private int _barsSinceEntry;
	private int _cooldown;

	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public decimal BbWidth { get => _bbWidth.Value; set => _bbWidth.Value = value; }
	public int ExitPeriods { get => _exitPeriods.Value; set => _exitPeriods.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VixSpikeStrategy()
	{
		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands length", "Parameters");

		_bbWidth = Param(nameof(BbWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Bands width multiplier", "Parameters");

		_exitPeriods = Param(nameof(ExitPeriods), 15)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Bars to hold position", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
		_barsSinceEntry = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BbLength,
			Width = BbWidth
		};

		_barsSinceEntry = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = value as BollingerBandsValue;
		if (bb == null)
			return;

		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Spike down below lower band - buy (mean reversion)
		if (candle.ClosePrice < lower && Position <= 0)
		{
			BuyMarket();
			_cooldown = 60;
		}
		// Spike up above upper band - sell (mean reversion)
		else if (candle.ClosePrice > upper && Position >= 0)
		{
			SellMarket();
			_cooldown = 60;
		}
	}
}
