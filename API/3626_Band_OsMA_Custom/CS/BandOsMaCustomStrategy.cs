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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 20)
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 50)
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 12)
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
			var buySignal = _prevOsma > _prevLower && osma <= lower && osma < ma;
			var sellSignal = _prevOsma < _prevUpper && osma >= upper && osma > ma;

			if (buySignal && Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + 1 : 1);
			else if (sellSignal && Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + 1 : 1);
		}

		_prevOsma = osma;
		_prevUpper = upper;
		_prevLower = lower;
		_prevMa = ma;
		_hasPrev = true;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_bollinger = null;
		_osmaMA = null;
		_prevOsma = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_prevMa = 0;
		_hasPrev = false;

		base.OnReseted();
	}
}
