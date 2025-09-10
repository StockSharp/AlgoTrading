using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buy-only strategy based on EMA and Bollinger Bands.
/// Opens long above EMA, shifts stop to EMA after a strong move and waits for reset after take profit.
/// </summary>
public class BuyOnlyEmaBbStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _bbStdDev;
	private readonly StrategyParam<decimal> _rrRatio;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bollinger;
	private decimal _longSl;
	private decimal _longTp;
	private bool _waitForNewCross;
	private decimal _prevClose;
	private decimal _prevEma;
	private bool _hasPrev;

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BbStdDev { get => _bbStdDev.Value; set => _bbStdDev.Value = value; }

	/// <summary>
	/// Reward to risk ratio.
	/// </summary>
	public decimal RrRatio { get => _rrRatio.Value; set => _rrRatio.Value = value; }

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BuyOnlyEmaBbStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Period for EMA", "Parameters");

		_bbStdDev = Param(nameof(BbStdDev), 0.7m)
			.SetGreaterThanZero()
			.SetDisplay("BB StdDev", "Deviation multiplier", "Parameters");

		_rrRatio = Param(nameof(RrRatio), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Reward/Risk", "Reward to risk ratio", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_longSl = 0m;
		_longTp = 0m;
		_waitForNewCross = false;
		_prevClose = 0m;
		_prevEma = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands
		{
			Length = EmaLength,
			Width = BbStdDev,
			MovingAverage = new ExponentialMovingAverage { Length = EmaLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal ema)
			return;

		var close = candle.ClosePrice;

		if (Position == 0 && !_waitForNewCross && close > ema)
		{
			BuyMarket(Volume);
			_longSl = lower;
			_longTp = close + (close - lower) * RrRatio;
		}
		else if (Position > 0)
		{
			if (close > upper)
			_longSl = ema;

			if (close < _longSl)
			{
			SellMarket(Position);
			}
			else if (close >= _longTp)
			{
			SellMarket(Position);
			_waitForNewCross = true;
			}
		}

		if (_waitForNewCross && _hasPrev && _prevClose >= _prevEma && close < ema)
		_waitForNewCross = false;

		_prevClose = close;
		_prevEma = ema;
		_hasPrev = true;
	}
}
