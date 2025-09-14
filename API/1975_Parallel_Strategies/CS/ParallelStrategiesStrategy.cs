using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Heikin Ashi trend reversals with Donchian Channel breakouts and MACD confirmation.
/// </summary>
public class ParallelStrategiesStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian;
	private HeikinAshi _heikin;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private decimal? _prevHigh;
	private decimal? _prevLow;
	private int? _prevTrend;

	/// <summary>
	/// Donchian channel period for breakout detection.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Candle type for subscriptions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ParallelStrategiesStrategy()
	{
		_donchianPeriod = Param("DonchianPeriod", 5)
			.SetDisplay("Donchian Period", "Lookback for breakout calculation", "Indicators")
			.SetCanOptimize(true, 1, 20);
		_macdFast = Param("MacdFast", 12)
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators")
			.SetCanOptimize(true, 5, 30);
		_macdSlow = Param("MacdSlow", 26)
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators")
			.SetCanOptimize(true, 10, 60);
		_macdSignal = Param("MacdSignal", 9)
			.SetDisplay("MACD Signal", "Signal line period", "Indicators")
			.SetCanOptimize(true, 5, 30);
		_candleType = Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
			.SetDisplay("Candle Type", "Time frame for candles", "Common");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_donchian = new DonchianChannels { Length = DonchianPeriod };
		_heikin = new HeikinAshi();
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFast,
			LongPeriod = MacdSlow,
			SignalPeriod = MacdSignal
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, _heikin, _macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue heikinValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dc = (DonchianChannelsValue)donchianValue;
		var ha = (HeikinAshiValue)heikinValue;
		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		var trend = ha.Open < ha.Close ? 1 : -1;

		if (_prevHigh is decimal prevHigh && _prevLow is decimal prevLow && _prevTrend is int prevTrend)
		{
			if (trend > 0 && prevTrend < 0 && candle.ClosePrice > prevHigh && macd.Macd > macd.Signal && Position <= 0)
			{
				CancelActiveOrders();
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (trend < 0 && prevTrend > 0 && candle.ClosePrice < prevLow && macd.Macd < macd.Signal && Position >= 0)
			{
				CancelActiveOrders();
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}

		_prevHigh = dc.Upper;
		_prevLow = dc.Lower;
		_prevTrend = trend;
	}
}
