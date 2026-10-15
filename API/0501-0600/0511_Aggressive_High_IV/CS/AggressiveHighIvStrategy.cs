using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aggressive High IV strategy.
/// Trades fast/slow EMA crossovers only while ATR is above its mean plus one standard deviation. A bullish cross goes long and
/// a bearish cross goes short, reversing an opposite position. Each position is closed by an ATR stop-loss or take-profit
/// measured from the entry close.
/// </summary>
public class AggressiveHighIvStrategy : Strategy
{
	private const decimal _stopAtrMultiple = 2m;
	private const decimal _takeAtrMultiple = 4m;

	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _atrMeanLength;
	private readonly StrategyParam<int> _atrStdLength;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _atrMean;
	private StandardDeviation _atrStd;
	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Period of the ATR mean.
	/// </summary>
	public int AtrMeanLength
	{
		get => _atrMeanLength.Value;
		set => _atrMeanLength.Value = value;
	}

	/// <summary>
	/// Period of the ATR standard deviation.
	/// </summary>
	public int AtrStdLength
	{
		get => _atrStdLength.Value;
		set => _atrStdLength.Value = value;
	}

	/// <summary>
	/// Fraction of equity risked per trade (informational).
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AggressiveHighIvStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Fast EMA period", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Slow EMA period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Volatility");

		_atrMeanLength = Param(nameof(AtrMeanLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Mean Length", "Period of the ATR mean", "Volatility");

		_atrStdLength = Param(nameof(AtrStdLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Std Length", "Period of the ATR standard deviation", "Volatility");

		_riskFactor = Param(nameof(RiskFactor), 0.01m)
			.SetNotNegative()
			.SetDisplay("Risk Factor", "Fraction of equity risked per trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_atrMean = null;
		_atrStd = null;
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		_atrMean = new SimpleMovingAverage { Length = AtrMeanLength };
		_atrStd = new StandardDeviation { Length = AtrStdLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrMean = _atrMean.Process(atr, candle.ServerTime, true).ToDecimal();
		var atrStd = _atrStd.Process(atr, candle.ServerTime, true).ToDecimal();

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal pf || prevSlow is not decimal ps || !_atrMean.IsFormed || !_atrStd.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckExits(candle))
			return;

		var highVolatility = atr > atrMean + atrStd;
		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;
		var close = candle.ClosePrice;

		if (highVolatility && crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - _stopAtrMultiple * atr;
			_takePrice = close + _takeAtrMultiple * atr;
		}
		else if (highVolatility && crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + _stopAtrMultiple * atr;
			_takePrice = close - _takeAtrMultiple * atr;
		}
	}

	private bool CheckExits(ICandleMessage candle)
	{
		if (Position > 0 && _stopPrice > 0m && (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice))
		{
			SellMarket(Position);
			_stopPrice = 0m;
			_takePrice = 0m;
			return true;
		}

		if (Position < 0 && _stopPrice > 0m && (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice))
		{
			BuyMarket(-Position);
			_stopPrice = 0m;
			_takePrice = 0m;
			return true;
		}

		return false;
	}
}
