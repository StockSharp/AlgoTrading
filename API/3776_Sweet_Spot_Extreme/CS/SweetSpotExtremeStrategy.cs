using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA slope + CCI filter strategy.
/// Buys when EMA slope is up and CCI is oversold.
/// Sells when EMA slope is down and CCI is overbought.
/// Exits on EMA slope reversal.
/// </summary>
public class SweetSpotExtremeStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _buyCciLevel;
	private readonly StrategyParam<decimal> _sellCciLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevPrevEma;
	private bool _hasPrevEma;

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public decimal BuyCciLevel
	{
		get => _buyCciLevel.Value;
		set => _buyCciLevel.Value = value;
	}

	public decimal SellCciLevel
	{
		get => _sellCciLevel.Value;
		set => _sellCciLevel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SweetSpotExtremeStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetDisplay("EMA Period", "Trend EMA period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "CCI lookback", "Indicators");

		_buyCciLevel = Param(nameof(BuyCciLevel), -100m)
			.SetDisplay("Buy CCI", "Oversold CCI level for buy", "Indicators");

		_sellCciLevel = Param(nameof(SellCciLevel), 100m)
			.SetDisplay("Sell CCI", "Overbought CCI level for sell", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevEma = 0;
		_prevPrevEma = 0;
		_hasPrevEma = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrevEma)
		{
			_prevPrevEma = emaValue;
			_prevEma = emaValue;
			_hasPrevEma = true;
			return;
		}

		var slopeUp = emaValue > _prevEma && _prevEma > _prevPrevEma;
		var slopeDown = emaValue < _prevEma && _prevEma < _prevPrevEma;

		// Entry
		if (slopeUp && cciValue <= BuyCciLevel && Position <= 0)
		{
			BuyMarket();
		}
		else if (slopeDown && cciValue >= SellCciLevel && Position >= 0)
		{
			SellMarket();
		}
		// Exit on slope reversal
		else if (Position > 0 && emaValue < _prevEma)
		{
			SellMarket();
		}
		else if (Position < 0 && emaValue > _prevEma)
		{
			BuyMarket();
		}

		_prevPrevEma = _prevEma;
		_prevEma = emaValue;
	}
}
