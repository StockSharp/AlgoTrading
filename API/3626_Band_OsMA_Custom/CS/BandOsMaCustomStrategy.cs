using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining the MACD histogram (OsMA) with Bollinger Bands to trade reversals.
/// Similar to BandOsMa but with customizable moving average filter for exit signals.
/// </summary>
public class BandOsMaCustomStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _maPeriod;

	private BollingerBands _bollinger;
	private SMA _osmaMA;

	private decimal _prevOsma;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevMa;
	private bool _hasPrev;
	private int _signalDirection;

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

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public BandOsMaCustomStrategy()
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

		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetDisplay("MA Period", "OsMA moving average period for exit filter", "Indicators");
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

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		_osmaMA = new SMA { Length = MaPeriod };

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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var val = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (val.Macd is not decimal macdLine || val.Signal is not decimal signalLine)
			return;

		var osma = macdLine - signalLine;

		var bbResult = (BollingerBandsValue)_bollinger.Process(new DecimalIndicatorValue(_bollinger, osma, candle.CloseTime));
		if (bbResult.UpBand is not decimal upper || bbResult.LowBand is not decimal lower)
			return;

		var maResult = _osmaMA.Process(new DecimalIndicatorValue(_osmaMA, osma, candle.CloseTime));
		if (maResult.IsEmpty)
			return;

		var ma = maResult.GetValue<decimal>();

		if (_hasPrev)
		{
			// Exit logic: if long and OsMA crosses above MA, exit
			if (_signalDirection > 0 && osma >= ma && _prevOsma < _prevMa)
				_signalDirection = 0;
			else if (_signalDirection < 0 && osma <= ma && _prevOsma > _prevMa)
				_signalDirection = 0;

			// Entry: OsMA crosses below lower band (buy)
			if (_prevOsma > _prevLower && osma <= lower)
				_signalDirection = 1;
			// Entry: OsMA crosses above upper band (sell)
			else if (_prevOsma < _prevUpper && osma >= upper)
				_signalDirection = -1;

			// Execute trades
			if (Position > 0 && _signalDirection != 1)
				SellMarket();
			else if (Position < 0 && _signalDirection != -1)
				BuyMarket();
			else if (Position == 0 && _signalDirection == 1)
				BuyMarket();
			else if (Position == 0 && _signalDirection == -1)
				SellMarket();
		}

		_prevOsma = osma;
		_prevUpper = upper;
		_prevLower = lower;
		_prevMa = ma;
		_hasPrev = true;
	}
}
