using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy with fair value gap detection and multiple take-profit targets.
/// </summary>
public class JohnBobTradingBotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevClose;
	private decimal _prevPdHigh;
	private decimal _prevPdLow;
	private decimal _prev1High;
	private decimal _prev1Low;
	private decimal _prev2High;
	private decimal _prev2Low;
	private bool _initialized;

	private decimal _longStop;
	private decimal _shortStop;
	private decimal _tp1;
	private decimal _tp2;
	private decimal _tp3;
	private decimal _tp4;
	private decimal _tp5;
	private bool _tp1Hit;
	private bool _tp2Hit;
	private bool _tp3Hit;
	private bool _tp4Hit;
	private bool _tp5Hit;

	/// <summary>
	/// ATR multiplier for stop-loss calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="JohnBobTradingBotStrategy"/>.
	/// </summary>
	public JohnBobTradingBotStrategy()
	{
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Mult", "ATR stop multiplier", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevClose = default;
		_prevPdHigh = default;
		_prevPdLow = default;
		_prev1High = default;
		_prev1Low = default;
		_prev2High = default;
		_prev2Low = default;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = 14 };
		_highest = new Highest { Length = 50 };
		_lowest = new Lowest { Length = 50 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}
	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highestValue = _highest.Process(candle.HighPrice).ToDecimal();
		var lowestValue = _lowest.Process(candle.LowPrice).ToDecimal();

		if (!_atr.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevPdHigh = highestValue;
			_prevPdLow = lowestValue;
			_prev1High = candle.HighPrice;
			_prev1Low = candle.LowPrice;
			_prev2High = candle.HighPrice;
			_prev2Low = candle.LowPrice;
			_initialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var crossUp = _prevClose <= _prevPdLow && close > lowestValue;
		var crossDown = _prevClose >= _prevPdHigh && close < highestValue;

		var fvgUp = _prev2Low > candle.HighPrice;
		var fvgDown = _prev2High < candle.LowPrice;

		var buySignal = crossUp || fvgUp;
		var sellSignal = crossDown || fvgDown;

		if (buySignal && Position <= 0)
		{
			for (var i = 0; i < 5; i++)
				BuyMarket();

			_longStop = close - atrValue * AtrMultiplier;
			_tp1 = close + atrValue * 1m;
			_tp2 = close + atrValue * 1.5m;
			_tp3 = close + atrValue * 2m;
			_tp4 = close + atrValue * 2.5m;
			_tp5 = close + atrValue * 3m;
			_tp1Hit = _tp2Hit = _tp3Hit = _tp4Hit = _tp5Hit = false;
		}
		else if (sellSignal && Position >= 0)
		{
			for (var i = 0; i < 5; i++)
				SellMarket();

			_shortStop = close + atrValue * AtrMultiplier;
			_tp1 = close - atrValue * 1m;
			_tp2 = close - atrValue * 1.5m;
			_tp3 = close - atrValue * 2m;
			_tp4 = close - atrValue * 2.5m;
			_tp5 = close - atrValue * 3m;
			_tp1Hit = _tp2Hit = _tp3Hit = _tp4Hit = _tp5Hit = false;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _longStop)
			{
				SellMarket(Position);
			}
			else
			{
				if (!_tp1Hit && candle.HighPrice >= _tp1)
				{
					SellMarket(1);
					_tp1Hit = true;
				}
				if (!_tp2Hit && candle.HighPrice >= _tp2)
				{
					SellMarket(1);
					_tp2Hit = true;
				}
				if (!_tp3Hit && candle.HighPrice >= _tp3)
				{
					SellMarket(1);
					_tp3Hit = true;
				}
				if (!_tp4Hit && candle.HighPrice >= _tp4)
				{
					SellMarket(1);
					_tp4Hit = true;
				}
				if (!_tp5Hit && candle.HighPrice >= _tp5)
				{
					SellMarket(1);
					_tp5Hit = true;
				}
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortStop)
			{
				BuyMarket(Math.Abs(Position));
			}
			else
			{
				if (!_tp1Hit && candle.LowPrice <= _tp1)
				{
					BuyMarket(1);
					_tp1Hit = true;
				}
				if (!_tp2Hit && candle.LowPrice <= _tp2)
				{
					BuyMarket(1);
					_tp2Hit = true;
				}
				if (!_tp3Hit && candle.LowPrice <= _tp3)
				{
					BuyMarket(1);
					_tp3Hit = true;
				}
				if (!_tp4Hit && candle.LowPrice <= _tp4)
				{
					BuyMarket(1);
					_tp4Hit = true;
				}
				if (!_tp5Hit && candle.LowPrice <= _tp5)
				{
					BuyMarket(1);
					_tp5Hit = true;
				}
			}
		}

		_prev2High = _prev1High;
		_prev2Low = _prev1Low;
		_prev1High = candle.HighPrice;
		_prev1Low = candle.LowPrice;
		_prevClose = close;
		_prevPdHigh = highestValue;
		_prevPdLow = lowestValue;
	}
}
