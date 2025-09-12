using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trades Bollinger Bands breakouts on daily candles with trend filter and ATR-based position sizing.
/// </summary>
public class DailyBollingerBandStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _trendFilterLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _riskRate;
	private readonly StrategyParam<int> _unit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private decimal _prevMiddle;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevMa;
	private decimal _prevClose;
	private bool _isInitialized;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }

	/// <summary>
	/// Trend filter moving average length.
	/// </summary>
	public int TrendFilterLength { get => _trendFilterLength.Value; set => _trendFilterLength.Value = value; }

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Risk percentage of capital per trade.
	/// </summary>
	public decimal RiskRate { get => _riskRate.Value; set => _riskRate.Value = value; }

	/// <summary>
	/// Volume unit step.
	/// </summary>
	public int Unit { get => _unit.Value; set => _unit.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Start date.
	/// </summary>
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// End date.
	/// </summary>
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public DailyBollingerBandStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators")
			.SetCanOptimize(true);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Deviation multiplier", "Indicators")
			.SetCanOptimize(true);

		_trendFilterLength = Param(nameof(TrendFilterLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Trend Filter Length", "Long MA period", "Indicators")
			.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Risk Management")
			.SetCanOptimize(true);

		_riskRate = Param(nameof(RiskRate), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Rate (%)", "Risk percentage of capital", "Risk Management")
			.SetCanOptimize(true);

		_unit = Param(nameof(Unit), 100)
			.SetGreaterThanZero()
			.SetDisplay("Unit", "Volume unit step", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(new DateTime(2000, 1, 4), TimeSpan.Zero))
			.SetDisplay("Start Time", "Strategy start date", "General");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(new DateTime(9999, 12, 30), TimeSpan.Zero))
			.SetDisplay("End Time", "Strategy end date", "General");
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
		_prevMiddle = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
		_prevMa = 0m;
		_prevClose = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerMultiplier
		};

		var ma = new SimpleMovingAverage { Length = TrendFilterLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(bollinger, ma, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal maValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevMiddle = middle;
			_prevUpper = upper;
			_prevLower = lower;
			_prevMa = maValue;
			_prevClose = candle.ClosePrice;
			_isInitialized = true;
			return;
		}

		var withinPeriod = candle.Time >= StartTime && candle.Time <= EndTime;

		var bbSlope = middle - _prevMiddle;
		var tfSlope = maValue - _prevMa;

		var crossUpper = _prevClose <= _prevUpper && candle.ClosePrice > upper;
		var crossLower = _prevClose >= _prevLower && candle.ClosePrice < lower;
		var crossDownMiddle = _prevClose >= _prevMiddle && candle.ClosePrice < middle;
		var crossUpMiddle = _prevClose <= _prevMiddle && candle.ClosePrice > middle;

		var size = CalculatePositionSize(atrValue);

		if (withinPeriod && Position == 0 && size > 0m)
		{
			if (crossUpper && bbSlope > 0m && tfSlope > 0m)
				BuyMarket(size);
			else if (crossLower && bbSlope < 0m && tfSlope < 0m)
				SellMarket(size);
		}

		if (withinPeriod)
		{
			if (Position > 0 && crossDownMiddle)
				SellMarket(Position);
			else if (Position < 0 && crossUpMiddle)
				BuyMarket(-Position);
		}

		_prevMiddle = middle;
		_prevUpper = upper;
		_prevLower = lower;
		_prevMa = maValue;
		_prevClose = candle.ClosePrice;
	}

	private decimal CalculatePositionSize(decimal atrValue)
	{
		if (atrValue <= 0m)
			return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		var s = (equity * RiskRate / 100m) / (2m * atrValue);
		var size = Math.Floor(s / Unit) * Unit;
		return size;
	}
}
