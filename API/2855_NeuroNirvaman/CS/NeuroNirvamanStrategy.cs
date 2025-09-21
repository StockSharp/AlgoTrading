using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Neuro Nirvaman perceptron-based strategy converted from MetaTrader 5.
/// Combines Laguerre smoothed +DI readings with SilverTrend swings to trigger trades.
/// </summary>
public class NeuroNirvamanStrategy : Strategy
{
	private const decimal LaguerreGamma = 0.764m;

	private readonly StrategyParam<int> _risk1;
	private readonly StrategyParam<int> _laguerre1Period;
	private readonly StrategyParam<decimal> _laguerre1Distance;
	private readonly StrategyParam<decimal> _x11;
	private readonly StrategyParam<decimal> _x12;
	private readonly StrategyParam<int> _tp1;
	private readonly StrategyParam<int> _sl1;

	private readonly StrategyParam<int> _risk2;
	private readonly StrategyParam<int> _laguerre2Period;
	private readonly StrategyParam<decimal> _laguerre2Distance;
	private readonly StrategyParam<decimal> _x21;
	private readonly StrategyParam<decimal> _x22;
	private readonly StrategyParam<int> _tp2;
	private readonly StrategyParam<int> _sl2;

	private readonly StrategyParam<int> _laguerre3Period;
	private readonly StrategyParam<decimal> _laguerre3Distance;
	private readonly StrategyParam<int> _laguerre4Period;
	private readonly StrategyParam<decimal> _laguerre4Distance;
	private readonly StrategyParam<decimal> _x31;
	private readonly StrategyParam<decimal> _x32;

	private readonly StrategyParam<int> _pass;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly LaguerrePlusDiState _laguerre1State = new(LaguerreGamma);
	private readonly LaguerrePlusDiState _laguerre2State = new(LaguerreGamma);
	private readonly LaguerrePlusDiState _laguerre3State = new(LaguerreGamma);
	private readonly LaguerrePlusDiState _laguerre4State = new(LaguerreGamma);

	private AverageDirectionalIndex _laguerre1Indicator = null!;
	private AverageDirectionalIndex _laguerre2Indicator = null!;
	private AverageDirectionalIndex _laguerre3Indicator = null!;
	private AverageDirectionalIndex _laguerre4Indicator = null!;

	private SilverTrendState _silverTrend1State = null!;
	private SilverTrendState _silverTrend2State = null!;

	private decimal? _takeProfitPrice;
	private decimal? _stopLossPrice;

	public NeuroNirvamanStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for analysis", "General");

		_volume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Trade Volume", "Order volume in lots", "Trading")
		.SetGreaterThanZero();

		_risk1 = Param(nameof(Risk1), 3)
		.SetDisplay("SilverTrend Risk #1", "Risk parameter for the first SilverTrend filter", "Indicators")
		.SetNotNegative();

		_laguerre1Period = Param(nameof(Laguerre1Period), 14)
		.SetDisplay("Laguerre Period #1", "ADX period for the first Laguerre filter", "Indicators")
		.SetGreaterThanZero();

		_laguerre1Distance = Param(nameof(Laguerre1Distance), 0m)
		.SetDisplay("Laguerre Distance #1", "Trigger distance for the first Laguerre filter", "Indicators")
		.SetNotNegative();

		_x11 = Param(nameof(X11), 100m)
		.SetDisplay("Weight X11", "Weight applied to the first Laguerre tension", "Perceptrons");

		_x12 = Param(nameof(X12), 100m)
		.SetDisplay("Weight X12", "Weight applied to the first SilverTrend signal", "Perceptrons");

		_tp1 = Param(nameof(TakeProfit1), 100)
		.SetDisplay("Take Profit #1", "Take profit in points for the first perceptron", "Risk")
		.SetNotNegative();

		_sl1 = Param(nameof(StopLoss1), 50)
		.SetDisplay("Stop Loss #1", "Stop loss in points for the first perceptron", "Risk")
		.SetNotNegative();

		_risk2 = Param(nameof(Risk2), 9)
		.SetDisplay("SilverTrend Risk #2", "Risk parameter for the second SilverTrend filter", "Indicators")
		.SetNotNegative();

		_laguerre2Period = Param(nameof(Laguerre2Period), 14)
		.SetDisplay("Laguerre Period #2", "ADX period for the second Laguerre filter", "Indicators")
		.SetGreaterThanZero();

		_laguerre2Distance = Param(nameof(Laguerre2Distance), 0m)
		.SetDisplay("Laguerre Distance #2", "Trigger distance for the second Laguerre filter", "Indicators")
		.SetNotNegative();

		_x21 = Param(nameof(X21), 100m)
		.SetDisplay("Weight X21", "Weight applied to the second Laguerre tension", "Perceptrons");

		_x22 = Param(nameof(X22), 100m)
		.SetDisplay("Weight X22", "Weight applied to the second SilverTrend signal", "Perceptrons");

		_tp2 = Param(nameof(TakeProfit2), 100)
		.SetDisplay("Take Profit #2", "Take profit in points for the second perceptron", "Risk")
		.SetNotNegative();

		_sl2 = Param(nameof(StopLoss2), 50)
		.SetDisplay("Stop Loss #2", "Stop loss in points for the second perceptron", "Risk")
		.SetNotNegative();

		_laguerre3Period = Param(nameof(Laguerre3Period), 14)
		.SetDisplay("Laguerre Period #3", "ADX period for the third Laguerre filter", "Indicators")
		.SetGreaterThanZero();

		_laguerre3Distance = Param(nameof(Laguerre3Distance), 0m)
		.SetDisplay("Laguerre Distance #3", "Trigger distance for the third Laguerre filter", "Indicators")
		.SetNotNegative();

		_laguerre4Period = Param(nameof(Laguerre4Period), 14)
		.SetDisplay("Laguerre Period #4", "ADX period for the fourth Laguerre filter", "Indicators")
		.SetGreaterThanZero();

		_laguerre4Distance = Param(nameof(Laguerre4Distance), 0m)
		.SetDisplay("Laguerre Distance #4", "Trigger distance for the fourth Laguerre filter", "Indicators")
		.SetNotNegative();

		_x31 = Param(nameof(X31), 100m)
		.SetDisplay("Weight X31", "Weight applied to the third Laguerre activation", "Perceptrons");

		_x32 = Param(nameof(X32), 100m)
		.SetDisplay("Weight X32", "Weight applied to the fourth Laguerre activation", "Perceptrons");

		_pass = Param(nameof(Pass), 3)
		.SetDisplay("Pass", "Determines which perceptrons participate in the decision", "Logic")
		.SetNotNegative();
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	public int Risk1
	{
		get => _risk1.Value;
		set => _risk1.Value = value;
	}

	public int Laguerre1Period
	{
		get => _laguerre1Period.Value;
		set => _laguerre1Period.Value = value;
	}

	public decimal Laguerre1Distance
	{
		get => _laguerre1Distance.Value;
		set => _laguerre1Distance.Value = value;
	}

	public decimal X11
	{
		get => _x11.Value;
		set => _x11.Value = value;
	}

	public decimal X12
	{
		get => _x12.Value;
		set => _x12.Value = value;
	}

	public int TakeProfit1
	{
		get => _tp1.Value;
		set => _tp1.Value = value;
	}

	public int StopLoss1
	{
		get => _sl1.Value;
		set => _sl1.Value = value;
	}

	public int Risk2
	{
		get => _risk2.Value;
		set => _risk2.Value = value;
	}

	public int Laguerre2Period
	{
		get => _laguerre2Period.Value;
		set => _laguerre2Period.Value = value;
	}

	public decimal Laguerre2Distance
	{
		get => _laguerre2Distance.Value;
		set => _laguerre2Distance.Value = value;
	}

	public decimal X21
	{
		get => _x21.Value;
		set => _x21.Value = value;
	}

	public decimal X22
	{
		get => _x22.Value;
		set => _x22.Value = value;
	}

	public int TakeProfit2
	{
		get => _tp2.Value;
		set => _tp2.Value = value;
	}

	public int StopLoss2
	{
		get => _sl2.Value;
		set => _sl2.Value = value;
	}

	public int Laguerre3Period
	{
		get => _laguerre3Period.Value;
		set => _laguerre3Period.Value = value;
	}

	public decimal Laguerre3Distance
	{
		get => _laguerre3Distance.Value;
		set => _laguerre3Distance.Value = value;
	}

	public int Laguerre4Period
	{
		get => _laguerre4Period.Value;
		set => _laguerre4Period.Value = value;
	}

	public decimal Laguerre4Distance
	{
		get => _laguerre4Distance.Value;
		set => _laguerre4Distance.Value = value;
	}

	public decimal X31
	{
		get => _x31.Value;
		set => _x31.Value = value;
	}

	public decimal X32
	{
		get => _x32.Value;
		set => _x32.Value = value;
	}

	public int Pass
	{
		get => _pass.Value;
		set => _pass.Value = value;
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

		_takeProfitPrice = null;
		_stopLossPrice = null;

		_laguerre1State.Reset();
		_laguerre2State.Reset();
		_laguerre3State.Reset();
		_laguerre4State.Reset();

		_silverTrend1State?.Reset();
		_silverTrend2State?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Synchronize default trade volume with the base strategy property.
		Volume = TradeVolume;

		_laguerre1Indicator = new AverageDirectionalIndex { Length = Laguerre1Period };
		_laguerre2Indicator = new AverageDirectionalIndex { Length = Laguerre2Period };
		_laguerre3Indicator = new AverageDirectionalIndex { Length = Laguerre3Period };
		_laguerre4Indicator = new AverageDirectionalIndex { Length = Laguerre4Period };

		_silverTrend1State = new SilverTrendState(Risk1);
		_silverTrend2State = new SilverTrendState(Risk2);

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(_laguerre1Indicator, _laguerre2Indicator, _laguerre3Indicator, _laguerre4Indicator, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _laguerre1Indicator);
			DrawIndicator(area, _laguerre2Indicator);
			DrawIndicator(area, _laguerre3Indicator);
			DrawIndicator(area, _laguerre4Indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue laguerre1Value, IIndicatorValue laguerre2Value, IIndicatorValue laguerre3Value, IIndicatorValue laguerre4Value)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var step = Security.PriceStep ?? 1m;

		// Manage open position exits before looking for new signals.
		if (Position > 0)
		{
			if (_stopLossPrice is decimal longStop && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				ResetTargets();
			}
			else if (_takeProfitPrice is decimal longTarget && candle.HighPrice >= longTarget)
			{
				SellMarket(Position);
				ResetTargets();
			}
		}
		else if (Position < 0)
		{
			if (_stopLossPrice is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
			}
			else if (_takeProfitPrice is decimal shortTarget && candle.LowPrice <= shortTarget)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
			}
		}

		// Update SilverTrend states with the most recent candle.
		_silverTrend1State.Risk = Risk1;
		_silverTrend2State.Risk = Risk2;

		var silver1 = _silverTrend1State.Process(candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		var silver2 = _silverTrend2State.Process(candle.HighPrice, candle.LowPrice, candle.ClosePrice);

		if (!laguerre1Value.IsFinal || !laguerre2Value.IsFinal || !laguerre3Value.IsFinal || !laguerre4Value.IsFinal)
		{
			return;
		}

		var adx1 = (AverageDirectionalIndexValue)laguerre1Value;
		var adx2 = (AverageDirectionalIndexValue)laguerre2Value;
		var adx3 = (AverageDirectionalIndexValue)laguerre3Value;
		var adx4 = (AverageDirectionalIndexValue)laguerre4Value;

		if (adx1.Dx.Plus is not decimal plus1 ||
		adx2.Dx.Plus is not decimal plus2 ||
		adx3.Dx.Plus is not decimal plus3 ||
		adx4.Dx.Plus is not decimal plus4)
		{
			return;
		}

		var lag1 = _laguerre1State.Process(plus1);
		var lag2 = _laguerre2State.Process(plus2);
		var lag3 = _laguerre3State.Process(plus3);
		var lag4 = _laguerre4State.Process(plus4);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (Position != 0)
		{
			return;
		}

		var decision = EvaluateSupervisor(lag1, lag2, lag3, lag4, silver1, silver2, out var takeProfitPoints, out var stopLossPoints);

		if (decision > 0)
		{
			var entryPrice = candle.ClosePrice;
			BuyMarket(TradeVolume);
			_takeProfitPrice = entryPrice + takeProfitPoints * step;
			_stopLossPrice = entryPrice - stopLossPoints * step;
		}
		else if (decision < 0)
		{
			var entryPrice = candle.ClosePrice;
			SellMarket(TradeVolume);
			_takeProfitPrice = entryPrice - takeProfitPoints * step;
			_stopLossPrice = entryPrice + stopLossPoints * step;
		}
	}

	private int EvaluateSupervisor(decimal lag1, decimal lag2, decimal lag3, decimal lag4, int silver1, int silver2, out decimal takeProfitPoints, out decimal stopLossPoints)
	{
		var perceptron1 = ComputePerceptron1(lag1, silver1);
		var perceptron2 = ComputePerceptron2(lag2, silver2);
		var perceptron3 = ComputePerceptron3(lag3, lag4);

		takeProfitPoints = TakeProfit1;
		stopLossPoints = StopLoss1;

		switch (Pass)
		{
			case 3:
			if (perceptron3 > 0m)
			{
				if (perceptron2 > 0m)
				{
					takeProfitPoints = TakeProfit2;
					stopLossPoints = StopLoss2;
					return 1;
				}
			}
			else if (perceptron1 < 0m)
			{
				takeProfitPoints = TakeProfit1;
				stopLossPoints = StopLoss1;
				return -1;
			}
			break;

			case 2:
			if (perceptron2 > 0m)
			{
				takeProfitPoints = TakeProfit2;
				stopLossPoints = StopLoss2;
				return 1;
			}
			return -1;

			case 1:
			if (perceptron1 < 0m)
			{
				takeProfitPoints = TakeProfit1;
				stopLossPoints = StopLoss1;
				return -1;
			}
			return 1;
		}

		return 0;
	}

	private decimal ComputePerceptron1(decimal lag1, int silver1)
	{
		var weightLaguerre = X11 - 100m;
		var weightSilver = X12 - 100m;
		var tension = ComputeTensionSignal(lag1, Laguerre1Distance);
		return weightLaguerre * tension + weightSilver * silver1;
	}

	private decimal ComputePerceptron2(decimal lag2, int silver2)
	{
		var weightLaguerre = X21 - 100m;
		var weightSilver = X22 - 100m;
		var tension = ComputeTensionSignal(lag2, Laguerre2Distance);
		return weightLaguerre * tension + weightSilver * silver2;
	}

	private decimal ComputePerceptron3(decimal lag3, decimal lag4)
	{
		var weightLaguerre3 = X31 - 100m;
		var weightLaguerre4 = X32 - 100m;
		var activation3 = ComputeTensionSignal(lag3, Laguerre3Distance);
		var activation4 = ComputeTensionSignal(lag4, Laguerre4Distance);
		return weightLaguerre3 * activation3 + weightLaguerre4 * activation4;
	}

	private static decimal ComputeTensionSignal(decimal laguerreValue, decimal distance)
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

	private void ResetTargets()
	{
		_takeProfitPrice = null;
		_stopLossPrice = null;
	}

	private sealed class LaguerrePlusDiState
	{
		private readonly decimal _gamma;
		private decimal _l0;
		private decimal _l1;
		private decimal _l2;
		private decimal _l3;

		public LaguerrePlusDiState(decimal gamma)
		{
			_gamma = gamma;
		}

		public decimal Process(decimal plusDi)
		{
			var l0Prev = _l0;
			var l1Prev = _l1;
			var l2Prev = _l2;
			var l3Prev = _l3;

			var l0 = (1m - _gamma) * plusDi + _gamma * l0Prev;
			var l1 = -_gamma * l0 + l0Prev + _gamma * l1Prev;
			var l2 = -_gamma * l1 + l1Prev + _gamma * l2Prev;
			var l3 = -_gamma * l2 + l2Prev + _gamma * l3Prev;

			var cu = 0m;
			var cd = 0m;

			if (l0 >= l1)
			cu = l0 - l1;
			else
			cd = l1 - l0;

			if (l1 >= l2)
			cu += l1 - l2;
			else
			cd += l2 - l1;

			if (l2 >= l3)
			cu += l2 - l3;
			else
			cd += l3 - l2;

			var sum = cu + cd;
			var result = sum == 0m ? 0.5m : cu / sum;

			_l0 = l0;
			_l1 = l1;
			_l2 = l2;
			_l3 = l3;

			return result;
		}

		public void Reset()
		{
			_l0 = 0m;
			_l1 = 0m;
			_l2 = 0m;
			_l3 = 0m;
		}
	}

	private sealed class SilverTrendState
	{
		private const int Ssp = 9;

		private readonly List<decimal> _highs = new();
		private readonly List<decimal> _lows = new();
		private readonly List<decimal> _closes = new();
		private bool _uptrend;

		public SilverTrendState(int risk)
		{
			Risk = risk;
		}

		public int Risk { get; set; }

		public int Process(decimal high, decimal low, decimal close)
		{
			_highs.Insert(0, high);
			_lows.Insert(0, low);
			_closes.Insert(0, close);

			if (_highs.Count > Ssp + 1)
			{
				_highs.RemoveAt(_highs.Count - 1);
				_lows.RemoveAt(_lows.Count - 1);
				_closes.RemoveAt(_closes.Count - 1);
			}

			if (_highs.Count < Ssp + 1)
			return 0;

			decimal avgRange = 0m;
			for (var i = 0; i <= Ssp; i++)
			avgRange += Math.Abs(_highs[i] - _lows[i]);

			var range = avgRange / (Ssp + 1);

			var ssMax = _lows[0];
			var ssMin = _closes[0];
			for (var i = 0; i <= Ssp - 1; i++)
			{
				var highValue = _highs[i];
				if (ssMax < highValue)
				ssMax = highValue;

				var lowValue = _lows[i];
				if (ssMin >= lowValue)
				ssMin = lowValue;
			}

			var k = 33 - Risk;
			var delta = ssMax - ssMin;
			var smin = ssMin + delta * k / 100m;
			var smax = ssMax - delta * k / 100m;

			var uptrend = _uptrend;
			if (close < smin)
			uptrend = false;
			else if (close > smax)
			uptrend = true;

			var signal = 0;
			if (uptrend != _uptrend)
			signal = uptrend ? -1 : 1;

			_uptrend = uptrend;
			return signal;
		}

		public void Reset()
		{
			_highs.Clear();
			_lows.Clear();
			_closes.Clear();
			_uptrend = false;
		}
	}
}
