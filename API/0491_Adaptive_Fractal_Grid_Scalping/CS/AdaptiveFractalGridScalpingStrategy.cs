using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on fractal pivots, ATR volatility, and SMA trend filter.
/// Buys when price breaks above fractal high in uptrend, sells when breaks below fractal low in downtrend.
/// </summary>
public class AdaptiveFractalGridScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private decimal? _fractalHigh;
	private decimal? _fractalLow;
	private decimal _entryPrice;
	private int _cooldownRemaining;
	private int _barCount;

	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public decimal StopMultiplier { get => _stopMultiplier.Value; set => _stopMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdaptiveFractalGridScalpingStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "Parameters")
			.SetOptimize(7, 28, 7);

		_smaLength = Param(nameof(SmaLength), 50)
			.SetDisplay("SMA Length", "SMA period", "Parameters")
			.SetOptimize(20, 100, 10);

		_stopMultiplier = Param(nameof(StopMultiplier), 2m)
			.SetDisplay("Stop Multiplier", "ATR multiplier for stop/TP", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_h1 = _h2 = _h3 = _h4 = _h5 = 0;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0;
		_fractalHigh = null;
		_fractalLow = null;
		_entryPrice = 0;
		_cooldownRemaining = 0;
		_barCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrLength };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barCount++;

		// Update fractal buffers
		_h1 = _h2; _h2 = _h3; _h3 = _h4; _h4 = _h5; _h5 = candle.HighPrice;
		_l1 = _l2; _l2 = _l3; _l3 = _l4; _l4 = _l5; _l5 = candle.LowPrice;

		if (_barCount < 5)
			return;

		// Detect fractals
		if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
			_fractalHigh = _h3;

		if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
			_fractalLow = _l3;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Exit on ATR-based stop/TP
		if (Position > 0 && _entryPrice > 0)
		{
			var stopLoss = _entryPrice - atrValue * StopMultiplier;
			var takeProfit = _entryPrice + atrValue * StopMultiplier * 2;

			if (candle.ClosePrice <= stopLoss || candle.ClosePrice >= takeProfit)
			{
				SellMarket(Math.Abs(Position));
				_cooldownRemaining = CooldownBars;
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var stopLoss = _entryPrice + atrValue * StopMultiplier;
			var takeProfit = _entryPrice - atrValue * StopMultiplier * 2;

			if (candle.ClosePrice >= stopLoss || candle.ClosePrice <= takeProfit)
			{
				BuyMarket(Math.Abs(Position));
				_cooldownRemaining = CooldownBars;
				_entryPrice = 0;
				return;
			}
		}

		// Entry signals
		var isBullish = candle.ClosePrice > smaValue;
		var isBearish = candle.ClosePrice < smaValue;

		if (isBullish && _fractalHigh is decimal fh && candle.ClosePrice > fh && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}
		else if (isBearish && _fractalLow is decimal fl && candle.ClosePrice < fl && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}
	}
}
