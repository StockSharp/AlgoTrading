using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Aeron JJN Scalper expert advisor.
/// Detects reversal candle patterns (bullish candle after strong bearish, and vice versa)
/// and enters at breakout of the prior candle's open level.
/// </summary>
public class AeronJjnScalperEaStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _bodyMinAtr;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;
	private int _cooldown;

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal BodyMinAtr
	{
		get => _bodyMinAtr.Value;
		set => _bodyMinAtr.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AeronJjnScalperEaStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR indicator period", "Indicators");

		_bodyMinAtr = Param(nameof(BodyMinAtr), 1.5m)
			.SetDisplay("Body Min ATR", "Minimum candle body size as ATR multiple", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_hasPrev = false;
		_cooldown = 0;
		_prevOpen = 0;
		_prevClose = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = AtrLength };
		_hasPrev = false;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0)
		{
			SavePrev(candle);
			return;
		}

		if (_cooldown > 0)
			_cooldown--;

		if (_hasPrev && _cooldown == 0 && Position == 0)
		{
			var prevBody = Math.Abs(_prevClose - _prevOpen);
			var minBody = atrVal * BodyMinAtr;

			// Bullish candle after strong bearish candle => buy
			if (candle.ClosePrice > candle.OpenPrice && _prevClose < _prevOpen && prevBody >= minBody)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			// Bearish candle after strong bullish candle => sell
			else if (candle.ClosePrice < candle.OpenPrice && _prevClose > _prevOpen && prevBody >= minBody)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}

		SavePrev(candle);
	}

	private void SavePrev(ICandleMessage candle)
	{
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
