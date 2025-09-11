using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double EMA RSI crossover strategy.
/// </summary>
public class DemaRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiSmooth;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailStopPoints;
	private readonly StrategyParam<bool> _useSession;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema1;
	private ExponentialMovingAverage _ema2;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _smooth;

	private decimal _prevRsi;
	private decimal _prevSmooth;
	private bool _isInitialized;

	/// <summary>
	/// EMA length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI smoothing length.
	/// </summary>
	public int RsiSmoothLength { get => _rsiSmooth.Value; set => _rsiSmooth.Value = value; }

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Trailing stop in points.
	/// </summary>
	public decimal TrailStopPoints { get => _trailStopPoints.Value; set => _trailStopPoints.Value = value; }

	/// <summary>
	/// Use trading session filter.
	/// </summary>
	public bool UseSession { get => _useSession.Value; set => _useSession.Value = value; }

	/// <summary>
	/// Session start (UTC).
	/// </summary>
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }

	/// <summary>
	/// Session end (UTC).
	/// </summary>
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="DemaRsiStrategy"/>.
	/// </summary>
	public DemaRsiStrategy()
	{
		_maLength = Param(nameof(MaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "EMA length", "General");

		_rsiLength = Param(nameof(RsiLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "General");

		_rsiSmooth = Param(nameof(RsiSmoothLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("RSI Smooth", "RSI smoothing length", "General");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100000m)
			.SetDisplay("Take Profit", "Take profit in price points", "Risk");

		_trailStopPoints = Param(nameof(TrailStopPoints), 150m)
			.SetDisplay("Trail Stop", "Trailing stop in price points", "Risk");

		_useSession = Param(nameof(UseSession), true)
			.SetDisplay("Use Session", "Enable session filter", "Session");

		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(4))
			.SetDisplay("Session Start", "Session start time (UTC)", "Session");

		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(15))
			.SetDisplay("Session End", "Session end time (UTC)", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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

		_ema1 = null;
		_ema2 = null;
		_rsi = null;
		_smooth = null;
		_prevRsi = 0m;
		_prevSmooth = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema1 = new ExponentialMovingAverage { Length = MaLength };
		_ema2 = new ExponentialMovingAverage { Length = MaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_smooth = new ExponentialMovingAverage { Length = RsiSmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPoints, UnitTypes.Price),
			new Unit(TrailStopPoints, UnitTypes.Price),
			isStopTrailing: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (UseSession && !InSession(candle.OpenTime))
		{
			CloseAll();
			return;
		}

		var ema1Value = _ema1.Process(candle).ToDecimal();
		var ema2Value = _ema2.Process(ema1Value, candle.OpenTime, true).ToDecimal();
		var rsiValue = _rsi.Process(ema2Value, candle.OpenTime, true).ToDecimal();
		var smoothValue = _smooth.Process(rsiValue, candle.OpenTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevRsi = rsiValue;
			_prevSmooth = smoothValue;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevRsi <= _prevSmooth && rsiValue > smoothValue;
		var crossDown = _prevRsi >= _prevSmooth && rsiValue < smoothValue;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevRsi = rsiValue;
		_prevSmooth = smoothValue;
	}

	private bool InSession(DateTimeOffset time)
	{
		var t = time.TimeOfDay;
		return t >= SessionStart && t < SessionEnd;
	}

	private void CloseAll()
	{
		CancelActiveOrders();
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
