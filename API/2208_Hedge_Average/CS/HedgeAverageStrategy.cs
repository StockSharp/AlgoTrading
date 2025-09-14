using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedge Average strategy based on comparison of moving averages of open and close prices.
/// </summary>
public class HedgeAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailing;

	private SimpleMovingAverage _maOpen1;
	private SimpleMovingAverage _maClose1;
	private SimpleMovingAverage _maOpen2;
	private SimpleMovingAverage _maClose2;

	private decimal _prevMaOpen1;
	private decimal _prevMaClose1;
	private decimal _prevMaOpen2;
	private decimal _prevMaClose2;
	private bool _isInitialized;

	/// <summary>
	/// Period for first pair of moving averages.
	/// </summary>
	public int Period1
	{
		get => _period1.Value;
		set => _period1.Value = value;
	}

	/// <summary>
	/// Period for second pair of moving averages.
	/// </summary>
	public int Period2
	{
		get => _period2.Value;
		set => _period2.Value = value;
	}

	/// <summary>
	/// Trading start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Use trailing stop based on <see cref="StopLoss"/> distance.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="HedgeAverageStrategy"/>.
	/// </summary>
	public HedgeAverageStrategy()
	{
		_period1 = Param(nameof(Period1), 4)
			.SetGreaterThanZero()
			.SetDisplay("Period 1", "Period for first MA pair", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_period2 = Param(nameof(Period2), 4)
			.SetGreaterThanZero()
			.SetDisplay("Period 2", "Period for second MA pair", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_startHour = Param(nameof(StartHour), 6)
			.SetDisplay("Start Hour", "Hour to start trading", "Time");

		_endHour = Param(nameof(EndHour), 20)
			.SetDisplay("End Hour", "Hour to stop trading", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 20m);

		_stopLoss = Param(nameof(StopLoss), 100m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 20m);

		_useTrailing = Param(nameof(UseTrailing), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");
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
		_prevMaOpen1 = _prevMaClose1 = _prevMaOpen2 = _prevMaClose2 = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_maOpen1 = new SimpleMovingAverage { Length = Period1 };
		_maClose1 = new SimpleMovingAverage { Length = Period1 };
		_maOpen2 = new SimpleMovingAverage { Length = Period2 };
		_maClose2 = new SimpleMovingAverage { Length = Period2 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Price),
			new Unit(StopLoss, UnitTypes.Price),
			isStopTrailing: UseTrailing,
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Update indicators with current open and close prices
		var ma1o = _maOpen1.Process(candle.OpenPrice, candle.OpenTime, true).GetValue<decimal>();
		var ma1c = _maClose1.Process(candle.ClosePrice, candle.OpenTime, true).GetValue<decimal>();
		var ma2o = _maOpen2.Process(candle.OpenPrice, candle.OpenTime, true).GetValue<decimal>();
		var ma2c = _maClose2.Process(candle.ClosePrice, candle.OpenTime, true).GetValue<decimal>();

		// Initialize previous values
		if (!_isInitialized)
		{
			_prevMaOpen1 = ma1o;
			_prevMaClose1 = ma1c;
			_prevMaOpen2 = ma2o;
			_prevMaClose2 = ma2c;
			_isInitialized = true;
			return;
		}

		if (!IsTradingHour(candle.OpenTime.UtcDateTime))
		{
			_prevMaOpen1 = ma1o;
			_prevMaClose1 = ma1c;
			_prevMaOpen2 = ma2o;
			_prevMaClose2 = ma2c;
			return;
		}

		var buySignal = _prevMaOpen2 > _prevMaClose2 && _prevMaOpen1 < _prevMaClose1;
		var sellSignal = _prevMaOpen2 < _prevMaClose2 && _prevMaOpen1 > _prevMaClose1;

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sellSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevMaOpen1 = ma1o;
		_prevMaClose1 = ma1c;
		_prevMaOpen2 = ma2o;
		_prevMaClose2 = ma2c;
	}

	private bool IsTradingHour(DateTime time)
	{
		var hour = time.Hour;
		if (StartHour > EndHour)
			return hour >= StartHour || hour < EndHour;
		return hour >= StartHour && hour < EndHour;
	}
}
