using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chaikin Momentum Scalper strategy.
/// The Chaikin oscillator is EMA(FastLength) minus EMA(SlowLength) of the accumulation/distribution line. A cross above zero with the
/// close above SMA(SmaLength) goes long, a cross below zero with the close below the SMA goes short, reversing an opposite position.
/// Each entry freezes a stop AtrMultiplierSL ATRs and a target AtrMultiplierTP ATRs away.
/// </summary>
public class ChaikinMomentumScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastAdl;
	private ExponentialMovingAverage _slowAdl;
	private decimal _adl;
	private decimal? _prevOscillator;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// Fast EMA period of the oscillator.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period of the oscillator.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Period of the trend SMA.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
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
	/// ATR multiple of the stop.
	/// </summary>
	public decimal AtrMultiplierSL
	{
		get => _atrMultiplierSl.Value;
		set => _atrMultiplierSl.Value = value;
	}

	/// <summary>
	/// ATR multiple of the target.
	/// </summary>
	public decimal AtrMultiplierTP
	{
		get => _atrMultiplierTp.Value;
		set => _atrMultiplierTp.Value = value;
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
	public ChaikinMomentumScalperStrategy()
	{
		_fastLength = Param(nameof(FastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period of the oscillator", "Chaikin");

		_slowLength = Param(nameof(SlowLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period of the oscillator", "Chaikin");

		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period of the trend SMA", "Trend");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplierSl = Param(nameof(AtrMultiplierSL), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier SL", "ATR multiple of the stop", "Risk");

		_atrMultiplierTp = Param(nameof(AtrMultiplierTP), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier TP", "ATR multiple of the target", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_fastAdl = null;
		_slowAdl = null;
		_adl = 0m;
		_prevOscillator = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_fastAdl = new ExponentialMovingAverage { Length = FastLength };
		_slowAdl = new ExponentialMovingAverage { Length = SlowLength };
		var sma = new SimpleMovingAverage { Length = SmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;

		if (range > 0)
			_adl += ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / range * candle.TotalVolume;

		var fast = _fastAdl.Process(new DecimalIndicatorValue(_fastAdl, _adl, candle.OpenTime) { IsFinal = true });
		var slow = _slowAdl.Process(new DecimalIndicatorValue(_slowAdl, _adl, candle.OpenTime) { IsFinal = true });

		if (!fast.IsFormed || !slow.IsFormed)
			return;

		var oscillator = fast.GetValue<decimal>() - slow.GetValue<decimal>();
		var prevOscillator = _prevOscillator;
		_prevOscillator = oscillator;

		if (!smaValue.IsFormed || !atrValue.IsFormed || prevOscillator is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Stop and target frozen at entry.
		if (Position > 0 && _stopPrice is decimal longStop && _targetPrice is decimal longTarget && (candle.LowPrice <= longStop || candle.HighPrice >= longTarget))
		{
			SellMarket(Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		if (Position < 0 && _stopPrice is decimal shortStop && _targetPrice is decimal shortTarget && (candle.HighPrice >= shortStop || candle.LowPrice <= shortTarget))
		{
			BuyMarket(-Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		var close = candle.ClosePrice;
		var sma = smaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();

		if (prev <= 0 && oscillator > 0 && close > sma && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - atr * AtrMultiplierSL;
			_targetPrice = close + atr * AtrMultiplierTP;
		}
		else if (prev >= 0 && oscillator < 0 && close < sma && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + atr * AtrMultiplierSL;
			_targetPrice = close - atr * AtrMultiplierTP;
		}
	}
}
