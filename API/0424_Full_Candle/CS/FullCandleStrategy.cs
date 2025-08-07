namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Full Candle Strategy
/// </summary>
public class FullCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<decimal> _shadowPercent;
	private readonly StrategyParam<bool> _useTP;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<bool> _useSL;
	private readonly StrategyParam<decimal> _slPercent;

	private ExponentialMovingAverage _ema;
	private decimal? _entryPrice;
	private DateTimeOffset? _entryTime;

	public FullCandleStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_emaLength = Param(nameof(EmaLength), 10)
			.SetDisplay("EMA Length", "EMA period", "Moving Averages");

		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long entries", "Enable long positions", "Strategy");

		_showShort = Param(nameof(ShowShort), true)
			.SetDisplay("Short entries", "Enable short positions", "Strategy");

		_shadowPercent = Param(nameof(ShadowPercent), 5m)
			.SetDisplay("Shadow Percent", "Maximum shadow percentage", "Strategy");

		_useTP = Param(nameof(UseTP), false)
			.SetDisplay("Enable TP", "Enable Take Profit", "Take Profit");

		_tpPercent = Param(nameof(TPPercent), 1.2m)
			.SetDisplay("TP Percent", "Take Profit percentage", "Take Profit");

		_useSL = Param(nameof(UseSL), false)
			.SetDisplay("Enable SL", "Enable Stop Loss", "Stop Loss");

		_slPercent = Param(nameof(SLPercent), 1.8m)
			.SetDisplay("SL Percent", "Stop Loss percentage", "Stop Loss");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	public decimal ShadowPercent
	{
		get => _shadowPercent.Value;
		set => _shadowPercent.Value = value;
	}

	public bool UseTP
	{
		get => _useTP.Value;
		set => _useTP.Value = value;
	}

	public decimal TPPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	public bool UseSL
	{
		get => _useSL.Value;
		set => _useSL.Value = value;
	}

	public decimal SLPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = default;
		_entryTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize EMA indicator
		_ema = new ExponentialMovingAverage
		{
			Length = EmaLength
		};

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema, System.Drawing.Color.Purple);
			DrawOwnTrades(area);
		}

		// Setup protection if enabled
		if (UseTP && UseSL)
		{
			StartProtection(
				new Unit(TPPercent, UnitTypes.Percent),
				new Unit(SLPercent, UnitTypes.Percent)
			);
		}
		else if (UseTP)
		{
			StartProtection(
				new Unit(TPPercent, UnitTypes.Percent),
				null
			);
		}
		else if (UseSL)
		{
			StartProtection(
				null,
				new Unit(SLPercent, UnitTypes.Percent)
			);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicator to form
		if (!_ema.IsFormed)
			return;

		// Calculate candle metrics
		var candleSize = candle.HighPrice - candle.LowPrice;
		if (candleSize == 0)
			return;

		// Calculate shadow size
		decimal shadowSize;
		if (candle.ClosePrice > candle.OpenPrice)
		{
			// Green candle - upper shadow
			shadowSize = candle.HighPrice - candle.ClosePrice;
		}
		else
		{
			// Red candle - lower shadow
			shadowSize = candle.ClosePrice - candle.LowPrice;
		}

		var shadowPercentage = (shadowSize * 100) / candleSize;

		// Entry conditions
		var entryLong = candle.ClosePrice > candle.OpenPrice && 
						candle.ClosePrice > emaValue && 
						shadowPercentage <= ShadowPercent;

		var entryShort = candle.ClosePrice < candle.OpenPrice && 
						 candle.ClosePrice < emaValue && 
						 shadowPercentage <= ShadowPercent;

		// Exit conditions
		var exitLong = false;
		var exitShort = false;

		if (Position > 0 && _entryPrice.HasValue)
		{
			// Exit long when price is above entry + 0.2% and candle is green
			exitLong = candle.ClosePrice > (_entryPrice.Value * 1.002m) && 
					   candle.ClosePrice > candle.OpenPrice;
		}
		else if (Position < 0 && _entryPrice.HasValue)
		{
			// Exit short when price is below entry - 0.2% and candle is red
			exitShort = candle.ClosePrice < (_entryPrice.Value * 0.998m) && 
						candle.ClosePrice < candle.OpenPrice;
		}

		// Execute trades
		if (exitLong || exitShort)
		{
			ClosePosition();
			_entryPrice = null;
			_entryTime = null;
		}
		else if (ShowLong && entryLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
		}
		else if (ShowShort && entryShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
		}
	}
}