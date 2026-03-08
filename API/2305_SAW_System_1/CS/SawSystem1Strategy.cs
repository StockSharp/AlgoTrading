using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SAW System breakout strategy.
/// Uses ATR to calculate volatility range, then enters on breakout above/below
/// the open price offset by a fraction of ATR.
/// </summary>
public class SawSystem1Strategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevAtr;
	private decimal _sessionOpen;
	private bool _traded;
	private DateTime _currentDate;

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SawSystem1Strategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 0.5m)
			.SetDisplay("Breakout Multiplier", "Fraction of ATR for breakout offset", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_prevAtr = null;
		_sessionOpen = 0;
		_traded = false;
		_currentDate = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAtr = null;
		_sessionOpen = 0;
		_traded = false;
		_currentDate = default;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var date = candle.OpenTime.Date;

		// New day: record open price and reset
		if (date != _currentDate)
		{
			_currentDate = date;
			_sessionOpen = candle.OpenPrice;
			_traded = false;

			// Close any open position at start of new day
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();

			_prevAtr = atrValue;
			return;
		}

		if (_traded || _prevAtr is null || _sessionOpen == 0)
		{
			_prevAtr = atrValue;
			return;
		}

		var offset = _prevAtr.Value * BreakoutMultiplier;
		var upperBreak = _sessionOpen + offset;
		var lowerBreak = _sessionOpen - offset;

		if (candle.ClosePrice > upperBreak && Position <= 0)
		{
			BuyMarket();
			_traded = true;
		}
		else if (candle.ClosePrice < lowerBreak && Position >= 0)
		{
			SellMarket();
			_traded = true;
		}

		_prevAtr = atrValue;
	}
}
