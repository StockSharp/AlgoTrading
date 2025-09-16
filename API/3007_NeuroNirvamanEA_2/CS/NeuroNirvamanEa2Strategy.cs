using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Neuro Nirvaman EA 2 strategy converted from MQL5 implementation.
/// Combines Laguerre-smoothed +DI values with SilverTrend breakout signals.
/// </summary>
public class NeuroNirvamanEa2Strategy : Strategy
{
	private const decimal LaguerreGamma = 0.764m;
	private const int SilverTrendLength = 9;

	private readonly StrategyParam<int> _risk1;
	private readonly StrategyParam<int> _laguerre1Period;
	private readonly StrategyParam<decimal> _laguerre1Distance;
	private readonly StrategyParam<decimal> _x11;
	private readonly StrategyParam<decimal> _x12;
	private readonly StrategyParam<decimal> _tp1;
	private readonly StrategyParam<decimal> _sl1;

	private readonly StrategyParam<int> _risk2;
	private readonly StrategyParam<int> _laguerre2Period;
	private readonly StrategyParam<decimal> _laguerre2Distance;
	private readonly StrategyParam<decimal> _x21;
	private readonly StrategyParam<decimal> _x22;
	private readonly StrategyParam<decimal> _tp2;
	private readonly StrategyParam<decimal> _sl2;

	private readonly StrategyParam<int> _laguerre3Period;
	private readonly StrategyParam<decimal> _laguerre3Distance;
	private readonly StrategyParam<int> _laguerre4Period;
	private readonly StrategyParam<decimal> _laguerre4Distance;
	private readonly StrategyParam<decimal> _x31;
	private readonly StrategyParam<decimal> _x32;

	private readonly StrategyParam<int> _pass;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<DataType> _candleType;

	private LaguerrePlusDiState _laguerre1State = null!;
	private LaguerrePlusDiState _laguerre2State = null!;
	private LaguerrePlusDiState _laguerre3State = null!;
	private LaguerrePlusDiState _laguerre4State = null!;
	private SilverTrendState _silverTrend1State = null!;
	private SilverTrendState _silverTrend2State = null!;

	private TimeSpan _startTime;
	private TimeSpan _endTime;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _hasActiveTargets;

	/// <summary>
	/// Risk parameter for SilverTrend #1.
	/// </summary>
	public int Risk1
	{
		get => _risk1.Value;
		set => _risk1.Value = value;
	}

	/// <summary>
	/// Laguerre period for the first tension filter.
	/// </summary>
	public int Laguerre1Period
	{
		get => _laguerre1Period.Value;
		set => _laguerre1Period.Value = value;
	}

	/// <summary>
	/// Distance threshold for Laguerre #1 decisions.
	/// </summary>
	public decimal Laguerre1Distance
	{
		get => _laguerre1Distance.Value;
		set => _laguerre1Distance.Value = value;
	}

	/// <summary>
	/// Weight X11 for perceptron #1.
	/// </summary>
	public decimal X11
	{
		get => _x11.Value;
		set => _x11.Value = value;
	}

	/// <summary>
	/// Weight X12 for perceptron #1.
	/// </summary>
	public decimal X12
	{
		get => _x12.Value;
		set => _x12.Value = value;
	}

	/// <summary>
	/// Take profit distance for perceptron #1 trades.
	/// </summary>
	public decimal TakeProfit1
	{
		get => _tp1.Value;
		set => _tp1.Value = value;
	}

	/// <summary>
	/// Stop loss distance for perceptron #1 trades.
	/// </summary>
	public decimal StopLoss1
	{
		get => _sl1.Value;
		set => _sl1.Value = value;
	}

	/// <summary>
	/// Risk parameter for SilverTrend #2.
	/// </summary>
	public int Risk2
	{
		get => _risk2.Value;
		set => _risk2.Value = value;
	}

	/// <summary>
	/// Laguerre period for the second tension filter.
	/// </summary>
	public int Laguerre2Period
	{
		get => _laguerre2Period.Value;
		set => _laguerre2Period.Value = value;
	}

	/// <summary>
	/// Distance threshold for Laguerre #2 decisions.
	/// </summary>
	public decimal Laguerre2Distance
	{
		get => _laguerre2Distance.Value;
		set => _laguerre2Distance.Value = value;
	}

	/// <summary>
	/// Weight X21 for perceptron #2.
	/// </summary>
	public decimal X21
	{
		get => _x21.Value;
		set => _x21.Value = value;
	}

	/// <summary>
	/// Weight X22 for perceptron #2.
	/// </summary>
	public decimal X22
	{
		get => _x22.Value;
		set => _x22.Value = value;
	}

	/// <summary>
	/// Take profit distance for perceptron #2 trades.
	/// </summary>
	public decimal TakeProfit2
	{
		get => _tp2.Value;
		set => _tp2.Value = value;
	}

	/// <summary>
	/// Stop loss distance for perceptron #2 trades.
	/// </summary>
	public decimal StopLoss2
	{
		get => _sl2.Value;
		set => _sl2.Value = value;
	}

	/// <summary>
	/// Laguerre period for perceptron #3 first input.
	/// </summary>
	public int Laguerre3Period
	{
		get => _laguerre3Period.Value;
		set => _laguerre3Period.Value = value;
	}

	/// <summary>
	/// Distance threshold for Laguerre #3 decisions.
	/// </summary>
	public decimal Laguerre3Distance
	{
		get => _laguerre3Distance.Value;
		set => _laguerre3Distance.Value = value;
	}

	/// <summary>
	/// Laguerre period for perceptron #3 second input.
	/// </summary>
	public int Laguerre4Period
	{
		get => _laguerre4Period.Value;
		set => _laguerre4Period.Value = value;
	}

	/// <summary>
	/// Distance threshold for Laguerre #4 decisions.
	/// </summary>
	public decimal Laguerre4Distance
	{
		get => _laguerre4Distance.Value;
		set => _laguerre4Distance.Value = value;
	}

	/// <summary>
	/// Weight X31 for perceptron #3.
	/// </summary>
	public decimal X31
	{
		get => _x31.Value;
		set => _x31.Value = value;
	}

	/// <summary>
	/// Weight X32 for perceptron #3.
	/// </summary>
	public decimal X32
	{
		get => _x32.Value;
		set => _x32.Value = value;
	}

	/// <summary>
	/// Pass selection controlling which perceptrons are active.
	/// 1 uses perceptron #1, 2 uses #2, 3 combines #2 and #3, while 4 disables trading.
	/// </summary>
	public int Pass
	{
		get => _pass.Value;
		set => _pass.Value = value;
	}

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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
	/// Trading session start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
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
	/// Trading session end minute.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NeuroNirvamanEa2Strategy"/> class.
	/// </summary>
	public NeuroNirvamanEa2Strategy()
	{
		_risk1 = Param(nameof(Risk1), 3)
			.SetRange(1, 33)
			.SetDisplay("Risk #1", "SilverTrend risk parameter for the first signal", "SilverTrend #1")
			.SetCanOptimize(true);

		_laguerre1Period = Param(nameof(Laguerre1Period), 14)
			.SetRange(1, 100)
			.SetDisplay("Laguerre #1 Period", "ADX length for Laguerre #1", "Laguerre Filters")
			.SetCanOptimize(true);

		_laguerre1Distance = Param(nameof(Laguerre1Distance), 0m)
			.SetRange(0m, 50m)
			.SetDisplay("Laguerre #1 Distance", "Distance threshold in percent", "Laguerre Filters")
			.SetCanOptimize(true);

		_x11 = Param(nameof(X11), 100m)
			.SetRange(0m, 200m)
			.SetDisplay("X11", "Weight for tension signal in perceptron #1", "Perceptron #1")
			.SetCanOptimize(true);

		_x12 = Param(nameof(X12), 100m)
			.SetRange(0m, 200m)
			.SetDisplay("X12", "Weight for SilverTrend signal in perceptron #1", "Perceptron #1")
			.SetCanOptimize(true);

		_tp1 = Param(nameof(TakeProfit1), 100m)
			.SetRange(10m, 500m)
			.SetDisplay("Take Profit #1", "Profit target in price steps for perceptron #1", "Perceptron #1")
			.SetCanOptimize(true);

		_sl1 = Param(nameof(StopLoss1), 50m)
			.SetRange(5m, 500m)
			.SetDisplay("Stop Loss #1", "Stop distance in price steps for perceptron #1", "Perceptron #1")
			.SetCanOptimize(true);

		_risk2 = Param(nameof(Risk2), 9)
			.SetRange(1, 33)
			.SetDisplay("Risk #2", "SilverTrend risk parameter for the second signal", "SilverTrend #2")
			.SetCanOptimize(true);

		_laguerre2Period = Param(nameof(Laguerre2Period), 14)
			.SetRange(1, 100)
			.SetDisplay("Laguerre #2 Period", "ADX length for Laguerre #2", "Laguerre Filters")
			.SetCanOptimize(true);

		_laguerre2Distance = Param(nameof(Laguerre2Distance), 0m)
			.SetRange(0m, 50m)
			.SetDisplay("Laguerre #2 Distance", "Distance threshold in percent", "Laguerre Filters")
			.SetCanOptimize(true);

		_x21 = Param(nameof(X21), 100m)
			.SetRange(0m, 200m)
			.SetDisplay("X21", "Weight for tension signal in perceptron #2", "Perceptron #2")
			.SetCanOptimize(true);

		_x22 = Param(nameof(X22), 100m)
			.SetRange(0m, 200m)
			.SetDisplay("X22", "Weight for SilverTrend signal in perceptron #2", "Perceptron #2")
			.SetCanOptimize(true);

		_tp2 = Param(nameof(TakeProfit2), 100m)
			.SetRange(10m, 500m)
			.SetDisplay("Take Profit #2", "Profit target in price steps for perceptron #2", "Perceptron #2")
			.SetCanOptimize(true);

		_sl2 = Param(nameof(StopLoss2), 50m)
			.SetRange(5m, 500m)
			.SetDisplay("Stop Loss #2", "Stop distance in price steps for perceptron #2", "Perceptron #2")
			.SetCanOptimize(true);

		_laguerre3Period = Param(nameof(Laguerre3Period), 14)
			.SetRange(1, 100)
			.SetDisplay("Laguerre #3 Period", "ADX length feeding perceptron #3", "Laguerre Filters")
			.SetCanOptimize(true);

		_laguerre3Distance = Param(nameof(Laguerre3Distance), 0m)
			.SetRange(0m, 50m)
			.SetDisplay("Laguerre #3 Distance", "Distance threshold in percent", "Laguerre Filters")
			.SetCanOptimize(true);

		_laguerre4Period = Param(nameof(Laguerre4Period), 14)
			.SetRange(1, 100)
			.SetDisplay("Laguerre #4 Period", "ADX length feeding perceptron #3", "Laguerre Filters")
			.SetCanOptimize(true);

		_laguerre4Distance = Param(nameof(Laguerre4Distance), 0m)
			.SetRange(0m, 50m)
			.SetDisplay("Laguerre #4 Distance", "Distance threshold in percent", "Laguerre Filters")
			.SetCanOptimize(true);

		_x31 = Param(nameof(X31), 100m)
			.SetRange(0m, 200m)
			.SetDisplay("X31", "Weight for Laguerre #3 in perceptron #3", "Perceptron #3")
			.SetCanOptimize(true);

		_x32 = Param(nameof(X32), 100m)
			.SetRange(0m, 200m)
			.SetDisplay("X32", "Weight for Laguerre #4 in perceptron #3", "Perceptron #3")
			.SetCanOptimize(true);

		_pass = Param(nameof(Pass), 4)
			.SetRange(1, 4)
			.SetDisplay("Pass", "Selects which perceptrons drive orders", "Logic")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetRange(0.01m, 10m)
			.SetDisplay("Trade Volume", "Base order volume", "General")
			.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 9)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Trading window start hour", "Session");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Trading window start minute", "Session");

		_endHour = Param(nameof(EndHour), 17)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Trading window end hour", "Session");

		_endMinute = Param(nameof(EndMinute), 3)
			.SetRange(0, 59)
			.SetDisplay("End Minute", "Trading window end minute", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle subscription", "General");
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
		_laguerre1State?.Reset();
		_laguerre2State?.Reset();
		_laguerre3State?.Reset();
		_laguerre4State?.Reset();
		_silverTrend1State?.Reset();
		_silverTrend2State?.Reset();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_hasActiveTargets = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_laguerre1State = new LaguerrePlusDiState(Laguerre1Period);
		_laguerre2State = new LaguerrePlusDiState(Laguerre2Period);
		_laguerre3State = new LaguerrePlusDiState(Laguerre3Period);
		_laguerre4State = new LaguerrePlusDiState(Laguerre4Period);

		_silverTrend1State = new SilverTrendState { Risk = Risk1 };
		_silverTrend2State = new SilverTrendState { Risk = Risk2 };

		_startTime = new TimeSpan(StartHour, StartMinute, 0);
		_endTime = new TimeSpan(EndHour, EndMinute, 0);
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_hasActiveTargets = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(
				_laguerre1State.Indicator,
				_laguerre2State.Indicator,
				_laguerre3State.Indicator,
				_laguerre4State.Indicator,
				ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _laguerre1State.Indicator);
			DrawIndicator(area, _laguerre2State.Indicator);
			DrawIndicator(area, _laguerre3State.Indicator);
			DrawIndicator(area, _laguerre4State.Indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue laguerre1Value, IIndicatorValue laguerre2Value, IIndicatorValue laguerre3Value, IIndicatorValue laguerre4Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_silverTrend1State.Risk = Risk1;
		_silverTrend2State.Risk = Risk2;

		var silverTrend1Signal = _silverTrend1State.Process(candle);
		var silverTrend2Signal = _silverTrend2State.Process(candle);

		if (!laguerre1Value.IsFinal || !laguerre2Value.IsFinal || !laguerre3Value.IsFinal || !laguerre4Value.IsFinal)
			return;

		var laguerre1 = _laguerre1State.Process(laguerre1Value);
		var laguerre2 = _laguerre2State.Process(laguerre2Value);
		var laguerre3 = _laguerre3State.Process(laguerre3Value);
		var laguerre4 = _laguerre4State.Process(laguerre4Value);

		if (laguerre1 is null || laguerre2 is null || laguerre3 is null || laguerre4 is null)
			return;

		if (!IsWithinTradingWindow(candle.OpenTime))
		{
			if (Position != 0)
				ClosePosition();
			_hasActiveTargets = false;
			return;
		}

		if (_hasActiveTargets)
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				{
					SellMarket(Position);
					_hasActiveTargets = false;
				}
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				{
					BuyMarket(-Position);
					_hasActiveTargets = false;
				}
			}
			else
			{
				_hasActiveTargets = false;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		var direction = Supervisor(
			laguerre1.Value,
			laguerre2.Value,
			laguerre3.Value,
			laguerre4.Value,
			silverTrend1Signal,
			silverTrend2Signal,
			out var takeProfitSteps,
			out var stopLossSteps);

		if (direction > 0)
		{
			EnterLong(candle, takeProfitSteps, stopLossSteps);
		}
		else if (direction < 0)
		{
			EnterShort(candle, takeProfitSteps, stopLossSteps);
		}
	}

	private int Supervisor(decimal laguerre1, decimal laguerre2, decimal laguerre3, decimal laguerre4, int silverTrend1Signal, int silverTrend2Signal, out decimal takeProfitSteps, out decimal stopLossSteps)
	{
		takeProfitSteps = TakeProfit1;
		stopLossSteps = StopLoss1;

		var perceptron1 = CalculatePerceptron1(laguerre1, silverTrend1Signal);
		var perceptron2 = CalculatePerceptron2(laguerre2, silverTrend2Signal);
		var perceptron3 = CalculatePerceptron3(laguerre3, laguerre4);

		switch (Pass)
		{
			case 3:
				if (perceptron3 > 0m && perceptron2 > 0m)
				{
					takeProfitSteps = TakeProfit2;
					stopLossSteps = StopLoss2;
					return 1;
				}

				if (perceptron3 <= 0m && perceptron1 < 0m)
				{
					takeProfitSteps = TakeProfit1;
					stopLossSteps = StopLoss1;
					return -1;
				}

				return 0;

			case 2:
				if (perceptron2 > 0m)
				{
					takeProfitSteps = TakeProfit2;
					stopLossSteps = StopLoss2;
					return 1;
				}

				return -1;

			case 1:
				if (perceptron1 < 0m)
				{
					takeProfitSteps = TakeProfit1;
					stopLossSteps = StopLoss1;
					return -1;
				}

				return 1;

			default:
				return 0;
		}
	}

	private decimal CalculatePerceptron1(decimal laguerreValue, int silverTrendSignal)
	{
		var tension = CalculateTension(laguerreValue, Laguerre1Distance);
		var weightTension = X11 - 100m;
		var weightTrend = X12 - 100m;
		return weightTension * tension + weightTrend * silverTrendSignal;
	}

	private decimal CalculatePerceptron2(decimal laguerreValue, int silverTrendSignal)
	{
		var tension = CalculateTension(laguerreValue, Laguerre2Distance);
		var weightTension = X21 - 100m;
		var weightTrend = X22 - 100m;
		return weightTension * tension + weightTrend * silverTrendSignal;
	}

	private decimal CalculatePerceptron3(decimal laguerre3, decimal laguerre4)
	{
		var a1 = CalculateTension(laguerre3, Laguerre3Distance);
		var a2 = CalculateTension(laguerre4, Laguerre4Distance);
		var weight1 = X31 - 100m;
		var weight2 = X32 - 100m;
		return weight1 * a1 + weight2 * a2;
	}

	private static decimal CalculateTension(decimal laguerreValue, decimal distance)
	{
		var threshold = distance / 100m;
		var upper = 0.5m + threshold;
		var lower = 0.5m - threshold;

		if (laguerreValue > upper)
			return -1m;

		if (laguerreValue < lower)
			return 1m;

		return 0m;
	}

	private void EnterLong(ICandleMessage candle, decimal takeProfitSteps, decimal stopLossSteps)
	{
		var volume = Volume <= 0m ? 1m : Volume;
		BuyMarket(volume);
		var step = Security?.PriceStep ?? 1m;
		_entryPrice = candle.ClosePrice;
		_takePrice = _entryPrice + takeProfitSteps * step;
		_stopPrice = _entryPrice - stopLossSteps * step;
		_hasActiveTargets = true;
	}

	private void EnterShort(ICandleMessage candle, decimal takeProfitSteps, decimal stopLossSteps)
	{
		var volume = Volume <= 0m ? 1m : Volume;
		SellMarket(volume);
		var step = Security?.PriceStep ?? 1m;
		_entryPrice = candle.ClosePrice;
		_takePrice = _entryPrice - takeProfitSteps * step;
		_stopPrice = _entryPrice + stopLossSteps * step;
		_hasActiveTargets = true;
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		_hasActiveTargets = false;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var current = time.TimeOfDay;

		if (_startTime == _endTime)
			return true;

		if (_startTime < _endTime)
			return current >= _startTime && current <= _endTime;

		return current >= _startTime || current <= _endTime;
	}

	private sealed class LaguerrePlusDiState
	{
		private decimal _l0;
		private decimal _l1;
		private decimal _l2;
		private decimal _l3;
		private bool _initialized;

		public LaguerrePlusDiState(int period)
		{
			Indicator = new AverageDirectionalIndex { Length = period };
		}

		public AverageDirectionalIndex Indicator { get; }

		public decimal? Process(IIndicatorValue value)
		{
			if (value is not AverageDirectionalIndexValue adxValue || !value.IsFinal)
				return null;

			if (adxValue.Dx.Plus is not decimal plusDi)
				return null;

			var input = plusDi / 100m;

			if (!_initialized)
			{
				_l0 = input;
				_l1 = input;
				_l2 = input;
				_l3 = input;
				_initialized = true;
				return input;
			}

			var prevL0 = _l0;
			var prevL1 = _l1;
			var prevL2 = _l2;
			var prevL3 = _l3;

			_l0 = (1m - LaguerreGamma) * input + LaguerreGamma * prevL0;
			_l1 = -LaguerreGamma * _l0 + prevL0 + LaguerreGamma * prevL1;
			_l2 = -LaguerreGamma * _l1 + prevL1 + LaguerreGamma * prevL2;
			_l3 = -LaguerreGamma * _l2 + prevL2 + LaguerreGamma * prevL3;

			var cu = 0m;
			var cd = 0m;

			if (_l0 >= _l1)
				cu += _l0 - _l1;
			else
				cd += _l1 - _l0;

			if (_l1 >= _l2)
				cu += _l1 - _l2;
			else
				cd += _l2 - _l1;

			if (_l2 >= _l3)
				cu += _l2 - _l3;
			else
				cd += _l3 - _l2;

			var sum = cu + cd;
			return sum == 0m ? 0m : cu / sum;
		}

		public void Reset()
		{
			Indicator.Reset();
			_l0 = 0m;
			_l1 = 0m;
			_l2 = 0m;
			_l3 = 0m;
			_initialized = false;
		}
	}

	private sealed class SilverTrendState
	{
		private readonly Highest _highest;
		private readonly Lowest _lowest;
		private readonly SimpleMovingAverage _rangeAverage;
		private bool? _trend;

		public SilverTrendState()
		{
			_highest = new Highest { Length = SilverTrendLength };
			_lowest = new Lowest { Length = SilverTrendLength };
			_rangeAverage = new SimpleMovingAverage { Length = SilverTrendLength + 1 };
		}

		public int Risk { get; set; }

		public int Process(ICandleMessage candle)
		{
			var time = candle.OpenTime;
			var highValue = _highest.Process(candle.HighPrice, time, true);
			var lowValue = _lowest.Process(candle.LowPrice, time, true);
			var rangeValue = _rangeAverage.Process(candle.HighPrice - candle.LowPrice, time, true);

			if (!highValue.IsFinal || !lowValue.IsFinal || !rangeValue.IsFinal)
				return 0;

			var ssMax = highValue.ToDecimal();
			var ssMin = lowValue.ToDecimal();
			var distance = ssMax - ssMin;
			var k = (33m - Risk) / 100m;
			var smin = ssMin + distance * k;
			var smax = ssMax - distance * k;

			var uptrend = _trend ?? false;

			if (candle.ClosePrice < smin)
				uptrend = false;

			if (candle.ClosePrice > smax)
				uptrend = true;

			int signal = 0;

			if (_trend.HasValue && uptrend != _trend.Value)
				signal = uptrend ? 1 : -1;

			_trend = uptrend;
			return signal;
		}

		public void Reset()
		{
			_highest.Reset();
			_lowest.Reset();
			_rangeAverage.Reset();
			_trend = null;
		}
	}
}
