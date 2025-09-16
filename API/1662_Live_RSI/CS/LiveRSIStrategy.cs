using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects trend reversals using multiple RSI sources and Parabolic SAR.
/// Buys when RSI values form a bullish sequence and price is above SAR,
/// sells when RSI values form a bearish sequence and price is below SAR.
/// </summary>
public class LiveRSIStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<bool> _checkHour;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsiClose = null!;
	private RelativeStrengthIndex _rsiWeighted = null!;
	private RelativeStrengthIndex _rsiTypical = null!;
	private RelativeStrengthIndex _rsiMedian = null!;
	private RelativeStrengthIndex _rsiOpen = null!;
	private ParabolicSar _sar = null!;

	private TrendDirection _lastTrend;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Step parameter for Parabolic SAR.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Stop loss value in absolute price units.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Enable trading only during selected hours.
	/// </summary>
	public bool CheckHour
	{
		get => _checkHour.Value;
		set => _checkHour.Value = value;
	}

	/// <summary>
	/// Trading session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
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
	/// Initializes <see cref="LiveRSIStrategy"/>.
	/// </summary>
	public LiveRSIStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI", "Parameters");

		_sarStep = Param(nameof(SarStep), 0.08m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Step for Parabolic SAR", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 40)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_checkHour = Param(nameof(CheckHour), false)
			.SetDisplay("Check Hour", "Restrict trading hours", "General");

		_startHour = Param(nameof(StartHour), 17)
			.SetDisplay("Start Hour", "Trading start hour", "General");

		_endHour = Param(nameof(EndHour), 1)
			.SetDisplay("End Hour", "Trading end hour", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsiClose = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiWeighted = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiTypical = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiMedian = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiOpen = new RelativeStrengthIndex { Length = RsiPeriod };
		_sar = new ParabolicSar { Acceleration = SarStep, MaxAcceleration = 0.1m };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(default, new Unit(StopLoss, UnitTypes.Absolute));

		_lastTrend = TrendDirection.None;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_rsiClose.Process(candle.ClosePrice);
		_rsiWeighted.Process((candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m);
		_rsiTypical.Process((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m);
		_rsiMedian.Process((candle.HighPrice + candle.LowPrice) / 2m);
		_rsiOpen.Process(candle.OpenPrice);
		_sar.Process(candle);

		if (_rsiClose.Last is not decimal rsiClose ||
			_rsiWeighted.Last is not decimal rsiWeighted ||
			_rsiTypical.Last is not decimal rsiTypical ||
			_rsiMedian.Last is not decimal rsiMedian ||
			_rsiOpen.Last is not decimal rsiOpen ||
			_sar.Last is not decimal sar)
			return;

		var trend = DetectTrend(candle, rsiClose, rsiWeighted, rsiTypical, rsiMedian, rsiOpen, sar);

		if (_lastTrend == TrendDirection.None)
		{
			_lastTrend = trend;
			return;
		}

		if (trend == TrendDirection.Bull && _lastTrend == TrendDirection.Bear && Position <= 0)
		{
			BuyMarket();
			_lastTrend = TrendDirection.Bull;
		}
		else if (trend == TrendDirection.Bear && _lastTrend == TrendDirection.Bull && Position >= 0)
		{
			SellMarket();
			_lastTrend = TrendDirection.Bear;
		}

		if (Position > 0)
		{
			SellStop(sar, Position);
		}
		else if (Position < 0)
		{
			BuyStop(sar, -Position);
		}
	}

	private TrendDirection DetectTrend(ICandleMessage candle, decimal rsiClose, decimal rsiWeighted,
		decimal rsiTypical, decimal rsiMedian, decimal rsiOpen, decimal sar)
	{
		var hourOk = !CheckHour || (candle.OpenTime.Hour > StartHour && candle.OpenTime.Hour < EndHour);

		if (hourOk && rsiClose > rsiWeighted && rsiWeighted > rsiTypical && rsiTypical > rsiMedian &&
			rsiMedian > rsiOpen && candle.ClosePrice > sar && rsiClose > 50m)
		{
			return TrendDirection.Bull;
		}

		if (hourOk && rsiClose < rsiWeighted && rsiWeighted < rsiTypical && rsiTypical < rsiMedian &&
			rsiMedian < rsiOpen && candle.ClosePrice < sar && rsiClose < 50m)
		{
			return TrendDirection.Bear;
		}

		return TrendDirection.None;
	}

	private enum TrendDirection
	{
		None,
		Bull,
		Bear
	}
}
