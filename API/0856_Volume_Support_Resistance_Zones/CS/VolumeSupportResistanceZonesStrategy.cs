namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Volume-based Support & Resistance Zones V2.
/// Buys when price enters a support zone and exits on a resistance zone.
/// </summary>
public class VolumeSupportResistanceZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeMaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private SimpleMovingAverage _volumeSma;

	private decimal?[] _highs;
	private decimal?[] _lows;
	private decimal?[] _opens;
	private decimal?[] _closes;
	private decimal?[] _volumes;
	private decimal?[] _volumeSmaValues;
	private int _arrayCount;

	private decimal? _support;
	private decimal? _resistance;

	/// <summary>
	/// Volume moving average period.
	/// </summary>
	public int VolumeMaPeriod
	{
		get => _volumeMaPeriod.Value;
		set => _volumeMaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start date for trading.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date for trading.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="VolumeSupportResistanceZonesStrategy"/>.
	/// </summary>
	public VolumeSupportResistanceZonesStrategy()
	{
		_volumeMaPeriod = Param(nameof(VolumeMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA", "Volume moving average period", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2020, 12, 4)))
			.SetDisplay("Start Date", "Start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2025, 12, 31)))
			.SetDisplay("End Date", "End date", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_volumeSma = new SimpleMovingAverage { Length = VolumeMaPeriod };

		_highs = new decimal?[6];
		_lows = new decimal?[6];
		_opens = new decimal?[6];
		_closes = new decimal?[6];
		_volumes = new decimal?[6];
		_volumeSmaValues = new decimal?[6];
		_arrayCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var smaValue = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();

		for (var i = 0; i < _highs.Length - 1; i++)
		{
			_highs[i] = _highs[i + 1];
			_lows[i] = _lows[i + 1];
			_opens[i] = _opens[i + 1];
			_closes[i] = _closes[i + 1];
			_volumes[i] = _volumes[i + 1];
			_volumeSmaValues[i] = _volumeSmaValues[i + 1];
		}

		_highs[^1] = candle.HighPrice;
		_lows[^1] = candle.LowPrice;
		_opens[^1] = candle.OpenPrice;
		_closes[^1] = candle.ClosePrice;
		_volumes[^1] = candle.TotalVolume;
		_volumeSmaValues[^1] = smaValue;

		if (_arrayCount < _highs.Length)
			_arrayCount++;

		if (_arrayCount == _highs.Length)
		{
			var up = _highs[2] > _highs[1] && _highs[1] > _highs[0] && _highs[3] < _highs[2] && _highs[4] < _highs[3] && _volumes[2] > _volumeSmaValues[2];
			var down = _lows[2] < _lows[1] && _lows[1] < _lows[0] && _lows[3] > _lows[2] && _lows[4] > _lows[3] && _volumes[2] > _volumeSmaValues[2];

			if (up)
				_resistance = _closes[2] >= _opens[2] ? _closes[2] : _opens[2];

			if (down)
				_support = _closes[2] >= _opens[2] ? _opens[2] : _closes[2];
		}

		var time = candle.OpenTime;

		if (time < StartDate || time > EndDate)
			return;

		if (_support is decimal support && candle.ClosePrice <= support && Position <= 0)
			BuyMarket();

		if (_resistance is decimal resistance && candle.ClosePrice >= resistance && Position > 0)
			SellMarket(Position);
	}
}

