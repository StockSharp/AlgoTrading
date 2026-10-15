namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy combining CCI and MACD signal crossover with EMA trend filter.
/// Buy when MACD crosses above signal with CCI positive and price above EMA.
/// Sell when MACD crosses below signal with CCI negative and price below EMA.
/// </summary>
public class SmcTraderCamelCciMacd1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _cciPeriod;

	private decimal? _prevMacdMain;
	private decimal? _prevMacdSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	public SmcTraderCamelCciMacd1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaLength = Param(nameof(EmaLength), 34)
			.SetDisplay("EMA Length", "Trend EMA period", "Indicators");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "Fast EMA for MACD", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Slow EMA for MACD", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal line period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetDisplay("CCI Period", "CCI period", "Indicators");
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

		_prevMacdMain = null;
		_prevMacdSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacdMain = null;
		_prevMacdSignal = null;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, cci, ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue cciValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !cciValue.IsFinal || !emaValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdData)
			return;

		if (macdData.Macd is not decimal macdMain || macdData.Signal is not decimal macdSignal)
			return;

		var cci = cciValue.ToDecimal();
		var emaVal = emaValue.ToDecimal();

		if (_prevMacdMain is not decimal prevMain || _prevMacdSignal is not decimal prevSignal)
		{
			_prevMacdMain = macdMain;
			_prevMacdSignal = macdSignal;
			return;
		}

		var macdBullCross = prevMain <= prevSignal && macdMain > macdSignal;
		var macdBearCross = prevMain >= prevSignal && macdMain < macdSignal;

		// Long: MACD bullish cross + CCI > 0 + price above EMA
		if (Position <= 0 && macdBullCross && cci > 0 && candle.ClosePrice > emaVal)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Short: MACD bearish cross + CCI < 0 + price below EMA
		else if (Position >= 0 && macdBearCross && cci < 0 && candle.ClosePrice < emaVal)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMacdMain = macdMain;
		_prevMacdSignal = macdSignal;
	}
}
