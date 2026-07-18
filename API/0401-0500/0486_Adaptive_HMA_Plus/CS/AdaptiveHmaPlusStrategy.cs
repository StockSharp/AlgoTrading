namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Hull Moving Average slope with ATR volatility filter.
/// Enters long when HMA slope is positive and volatility is expanding.
/// Enters short when HMA slope is negative and volatility is expanding.
/// </summary>
public class AdaptiveHmaPlusStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private HullMovingAverage _hma;
	private AverageTrueRange _atrShort;
	private AverageTrueRange _atrLong;

	private decimal _prevHma;
	private int _cooldownRemaining;

	public int HmaLength { get => _hmaLength.Value; set => _hmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdaptiveHmaPlusStrategy()
	{
		_hmaLength = Param(nameof(HmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("HMA Length", "Hull Moving Average period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

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
		_hma = null;
		_atrShort = null;
		_atrLong = null;
		_prevHma = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hma = new HullMovingAverage { Length = HmaLength };
		_atrShort = new AverageTrueRange { Length = 14 };
		_atrLong = new AverageTrueRange { Length = 46 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_hma, _atrShort, _atrLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal atrShortValue, decimal atrLongValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHma = hmaValue;
			return;
		}

		if (_prevHma == 0)
		{
			_prevHma = hmaValue;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevHma = hmaValue;
			return;
		}

		var slope = hmaValue - _prevHma;
		var volExpanding = atrShortValue > atrLongValue;

		// Buy: HMA slope positive + volatility expanding
		if (slope > 0 && volExpanding && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: HMA slope negative + volatility expanding
		else if (slope < 0 && volExpanding && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: slope turns negative
		else if (Position > 0 && slope <= 0)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: slope turns positive
		else if (Position < 0 && slope >= 0)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevHma = hmaValue;
	}
}
