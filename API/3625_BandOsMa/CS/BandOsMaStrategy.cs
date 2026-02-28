using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining the MACD histogram (OsMA) with Bollinger Bands to trade reversals.
/// When OsMA crosses below lower Bollinger band, buy signal is generated.
/// When OsMA crosses above upper Bollinger band, sell signal is generated.
/// </summary>
public class BandOsMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;

	private decimal _prevOsma;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public BandOsMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal EMA length", "Indicators");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 14)
			.SetDisplay("Bollinger Period", "OsMA Bollinger Bands period", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Bollinger Deviation", "Bollinger Bands deviation", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private BollingerBands _bollinger;

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var val = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (val.Macd is not decimal macdLine || val.Signal is not decimal signalLine)
			return;

		var osma = macdLine - signalLine;

		_bollinger ??= new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var bbResult = (BollingerBandsValue)_bollinger.Process(new DecimalIndicatorValue(_bollinger, osma, candle.CloseTime));
		if (bbResult.UpBand is not decimal upper || bbResult.LowBand is not decimal lower)
		{
			return;
		}

		if (_hasPrev)
		{
			// Buy: OsMA crosses below lower band (reversal up expected)
			if (_prevOsma > _prevLower && osma <= lower && Position <= 0)
			{
				BuyMarket();
			}
			// Sell: OsMA crosses above upper band (reversal down expected)
			else if (_prevOsma < _prevUpper && osma >= upper && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevOsma = osma;
		_prevUpper = upper;
		_prevLower = lower;
		_hasPrev = true;
	}
}
