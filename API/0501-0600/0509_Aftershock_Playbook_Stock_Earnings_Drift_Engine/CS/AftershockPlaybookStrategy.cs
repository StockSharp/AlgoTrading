using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aftershock earnings drift strategy.
/// Uses large price moves relative to ATR as proxy for earnings surprise.
/// </summary>
public class AftershockPlaybookStrategy : Strategy
{
	private readonly StrategyParam<decimal> _atrMult;
	private readonly StrategyParam<int> _atrLen;
	private readonly StrategyParam<decimal> _surpriseThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevClose;
	private int _cooldownRemaining;

	public decimal AtrMultiplier { get => _atrMult.Value; set => _atrMult.Value = value; }
	public int AtrLength { get => _atrLen.Value; set => _atrLen.Value = value; }
	public decimal SurpriseThreshold { get => _surpriseThreshold.Value; set => _surpriseThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AftershockPlaybookStrategy()
	{
		_atrMult = Param(nameof(AtrMultiplier), 1.0m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for surprise threshold", "Strategy");

		_atrLen = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR lookback period", "Strategy");

		_surpriseThreshold = Param(nameof(SurpriseThreshold), 1.0m)
			.SetDisplay("Surprise Threshold", "ATR multiplier for detecting surprise moves", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

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
		_prevClose = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (_prevClose == 0 || atrValue == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = candle.ClosePrice;
			return;
		}

		var change = candle.ClosePrice - _prevClose;
		var threshold = atrValue * SurpriseThreshold;

		// Detect large price moves (earnings surprise proxy)
		if (change > threshold && Position <= 0)
		{
			// Large positive move - go long (drift continuation)
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (change < -threshold && Position >= 0)
		{
			// Large negative move - go short (drift continuation)
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit if price reverses by ATR amount
		else if (Position > 0 && change < -atrValue * AtrMultiplier)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && change > atrValue * AtrMultiplier)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
	}
}
