namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades using either MA, MACD or Bollinger Bands signals.
/// </summary>
public class MaMacdBbBackTesterStrategy : Strategy
{
	public enum IndicatorType
	{
		MA,
		MACD,
		BB
	}

	public enum TradeDirection
	{
		Long,
		Short
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<IndicatorType> _indicator;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private SimpleMovingAverage _ma;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private BollingerBands _bollinger;

	private decimal? _prevClose;
	private decimal? _prevMa;
	private decimal? _prevMacd;
	private decimal? _prevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public IndicatorType Indicator { get => _indicator.Value; set => _indicator.Value = value; }
	public TradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public decimal BbMultiplier { get => _bbMultiplier.Value; set => _bbMultiplier.Value = value; }
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	public MaMacdBbBackTesterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");

		_indicator = Param(nameof(Indicator), IndicatorType.MA)
			.SetDisplay("Indicator", "Trading indicator", "General");

		_direction = Param(nameof(Direction), TradeDirection.Long)
			.SetDisplay("Direction", "Trade direction", "General");

		_maLength = Param(nameof(MaLength), 50)
			.SetDisplay("MA Length", "Moving average period", "MA");

		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "MACD fast length", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "MACD slow length", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("Signal Length", "MACD signal length", "MACD");

		_bbLength = Param(nameof(BbLength), 20)
			.SetDisplay("BB Length", "Bollinger bands period", "BB");

		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetDisplay("BB Multiplier", "Bollinger bands multiplier", "BB");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2015, 1, 2, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Start date", "Date Range");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2023, 12, 29, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "End date", "Date Range");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		switch (Indicator)
		{
			case IndicatorType.MA:
				_ma = new SimpleMovingAverage { Length = MaLength };
				subscription.Bind(_ma, ProcessMa).Start();
				break;
			case IndicatorType.MACD:
				_macd = new MovingAverageConvergenceDivergenceSignal
				{
					Macd =
					{
						ShortMa = { Length = FastLength },
						LongMa = { Length = SlowLength },
					},
					SignalMa = { Length = SignalLength }
				};
				subscription.BindEx(_macd, ProcessMacd).Start();
				break;
			case IndicatorType.BB:
				_bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
				subscription.Bind(_bollinger, ProcessBb).Start();
				break;
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_ma != null)
				DrawIndicator(area, _ma);
			if (_macd != null)
				DrawIndicator(area, _macd);
			if (_bollinger != null)
				DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMa(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		if (time < StartDate || time > EndDate)
			return;

		if (!_ma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevClose is not decimal prevClose || _prevMa is not decimal prevMa)
		{
			_prevClose = candle.ClosePrice;
			_prevMa = ma;
			return;
		}

		var crossover = prevClose <= prevMa && candle.ClosePrice > ma;
		var crossunder = prevClose >= prevMa && candle.ClosePrice < ma;

		if (Direction == TradeDirection.Long)
		{
			if (crossover && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (crossunder && Position > 0)
			{
				SellMarket(Position);
			}
		}
		else
		{
			if (crossunder && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			else if (crossover && Position < 0)
			{
				BuyMarket(-Position);
			}
		}

		_prevClose = candle.ClosePrice;
		_prevMa = ma;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		if (time < StartDate || time > EndDate)
			return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
			return;

		if (!_macd.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevMacd is not decimal prevMacd || _prevSignal is not decimal prevSignal)
		{
			_prevMacd = macdLine;
			_prevSignal = signalLine;
			return;
		}

		var crossover = prevMacd <= prevSignal && macdLine > signalLine;
		var crossunder = prevMacd >= prevSignal && macdLine < signalLine;

		if (Direction == TradeDirection.Long)
		{
			if (crossover && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (crossunder && Position > 0)
			{
				SellMarket(Position);
			}
		}
		else
		{
			if (crossunder && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			else if (crossover && Position < 0)
			{
				BuyMarket(-Position);
			}
		}

		_prevMacd = macdLine;
		_prevSignal = signalLine;
	}

	private void ProcessBb(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		if (time < StartDate || time > EndDate)
			return;

		if (!_bollinger.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (Direction == TradeDirection.Long)
		{
			if (candle.ClosePrice < lower && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (candle.ClosePrice > middle && Position > 0)
			{
				SellMarket(Position);
			}
		}
		else
		{
			if (candle.ClosePrice > upper && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			else if (candle.ClosePrice < middle && Position < 0)
			{
				BuyMarket(-Position);
			}
		}
	}
}
