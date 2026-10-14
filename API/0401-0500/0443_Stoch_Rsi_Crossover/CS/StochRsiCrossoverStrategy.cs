namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Stoch RSI Crossover Strategy.
/// RSI is turned into a Stochastic RSI over StochLength bars and smoothed into %K (SmoothK) and %D (SmoothD).
/// A long opens when %K crosses above %D with %K in [10, 60], EMA1 > EMA2 > EMA3 and the close above EMA1.
/// A short opens when %K crosses below %D with %K in [40, 95], EMA1 &lt; EMA2 &lt; EMA3 and the close below EMA1.
/// There is no built-in exit: an opposite signal reverses the position. The ATR multipliers only describe suggested levels.
/// </summary>
public class StochRsiCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _smoothK;
	private readonly StrategyParam<int> _smoothD;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _ema1Length;
	private readonly StrategyParam<int> _ema2Length;
	private readonly StrategyParam<int> _ema3Length;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrLossMultiplier;
	private readonly StrategyParam<decimal> _atrProfitMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _rsiHigh;
	private Lowest _rsiLow;
	private SimpleMovingAverage _kMa;
	private SimpleMovingAverage _dMa;

	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// %K smoothing.
	/// </summary>
	public int SmoothK
	{
		get => _smoothK.Value;
		set => _smoothK.Value = value;
	}

	/// <summary>
	/// %D smoothing.
	/// </summary>
	public int SmoothD
	{
		get => _smoothD.Value;
		set => _smoothD.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Stochastic period applied to RSI.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int Ema1Length
	{
		get => _ema1Length.Value;
		set => _ema1Length.Value = value;
	}

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int Ema2Length
	{
		get => _ema2Length.Value;
		set => _ema2Length.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int Ema3Length
	{
		get => _ema3Length.Value;
		set => _ema3Length.Value = value;
	}

	/// <summary>
	/// ATR period of the suggested levels.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the suggested stop loss.
	/// </summary>
	public decimal AtrLossMultiplier
	{
		get => _atrLossMultiplier.Value;
		set => _atrLossMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the suggested profit target.
	/// </summary>
	public decimal AtrProfitMultiplier
	{
		get => _atrProfitMultiplier.Value;
		set => _atrProfitMultiplier.Value = value;
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
	public StochRsiCrossoverStrategy()
	{
		_smoothK = Param(nameof(SmoothK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smooth K", "%K smoothing", "Stoch RSI");

		_smoothD = Param(nameof(SmoothD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smooth D", "%D smoothing", "Stoch RSI");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Stoch RSI");

		_stochLength = Param(nameof(StochLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Length", "Stochastic period applied to RSI", "Stoch RSI");

		_ema1Length = Param(nameof(Ema1Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA1 Length", "Fast EMA period", "Trend");

		_ema2Length = Param(nameof(Ema2Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA2 Length", "Medium EMA period", "Trend");

		_ema3Length = Param(nameof(Ema3Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA3 Length", "Slow EMA period", "Trend");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period of the suggested levels", "Risk");

		_atrLossMultiplier = Param(nameof(AtrLossMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Loss Multiplier", "ATR multiplier of the suggested stop loss", "Risk");

		_atrProfitMultiplier = Param(nameof(AtrProfitMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Profit Multiplier", "ATR multiplier of the suggested profit target", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_prevD = null;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema1 = new ExponentialMovingAverage { Length = Ema1Length };
		var ema2 = new ExponentialMovingAverage { Length = Ema2Length };
		var ema3 = new ExponentialMovingAverage { Length = Ema3Length };

		_rsiHigh = new Highest { Length = StochLength };
		_rsiLow = new Lowest { Length = StochLength };
		_kMa = new SimpleMovingAverage { Length = SmoothK };
		_dMa = new SimpleMovingAverage { Length = SmoothD };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ema1, ema2, ema3, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema1);
			DrawIndicator(area, ema2);
			DrawIndicator(area, ema3);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue ema1Value, IIndicatorValue ema2Value, IIndicatorValue ema3Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var high = _rsiHigh.Process(rsi, candle.OpenTime, true).ToDecimal();
		var low = _rsiLow.Process(rsi, candle.OpenTime, true).ToDecimal();

		if (!_rsiHigh.IsFormed || !_rsiLow.IsFormed)
			return;

		var stoch = high - low > 0m ? 100m * (rsi - low) / (high - low) : 0m;
		var kValue = _kMa.Process(stoch, candle.OpenTime, true);
		if (!_kMa.IsFormed)
			return;

		var k = kValue.ToDecimal();
		var dValue = _dMa.Process(k, candle.OpenTime, true);
		if (!_dMa.IsFormed)
			return;

		var d = dValue.ToDecimal();

		var prevK = _prevK;
		var prevD = _prevD;
		_prevK = k;
		_prevD = d;

		if (prevK is not decimal pk || prevD is not decimal pd)
			return;

		if (!ema1Value.IsFormed || !ema2Value.IsFormed || !ema3Value.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema1 = ema1Value.GetValue<decimal>();
		var ema2 = ema2Value.GetValue<decimal>();
		var ema3 = ema3Value.GetValue<decimal>();
		var close = candle.ClosePrice;

		var crossUp = pk <= pd && k > d;
		var crossDown = pk >= pd && k < d;

		var longSignal = crossUp && k >= 10m && k <= 60m && ema1 > ema2 && ema2 > ema3 && close > ema1;
		var shortSignal = crossDown && k >= 40m && k <= 95m && ema1 < ema2 && ema2 < ema3 && close < ema1;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
