using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "NeuroNirvaman.mq4".
/// Rebuilds the perceptron-based supervisor that mixes Laguerre filtered +DI values with SilverTrend swings.
/// </summary>
public class NeuroNirvamanMq4Strategy : Strategy
{
	private readonly StrategyParam<decimal> _laguerreGamma;
	private readonly StrategyParam<decimal> _silverTrendKmax;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;

	private readonly StrategyParam<int> _silverTrend1Length;
	private readonly StrategyParam<int> _laguerre1Period;
	private readonly StrategyParam<decimal> _laguerre1Distance;
	private readonly StrategyParam<decimal> _x11;
	private readonly StrategyParam<decimal> _x12;
	private readonly StrategyParam<decimal> _takeProfit1;
	private readonly StrategyParam<decimal> _stopLoss1;

	private readonly StrategyParam<int> _silverTrend2Length;
	private readonly StrategyParam<int> _laguerre2Period;
	private readonly StrategyParam<decimal> _laguerre2Distance;
	private readonly StrategyParam<decimal> _x21;
	private readonly StrategyParam<decimal> _x22;
	private readonly StrategyParam<decimal> _takeProfit2;
	private readonly StrategyParam<decimal> _stopLoss2;

	private readonly StrategyParam<int> _laguerre3Period;
	private readonly StrategyParam<decimal> _laguerre3Distance;
	private readonly StrategyParam<int> _laguerre4Period;
	private readonly StrategyParam<decimal> _laguerre4Distance;
	private readonly StrategyParam<decimal> _x31;
	private readonly StrategyParam<decimal> _x32;

	private readonly StrategyParam<int> _pass;

	private AverageDirectionalIndex _laguerre1Indicator = null!;
	private AverageDirectionalIndex _laguerre2Indicator = null!;
	private AverageDirectionalIndex _laguerre3Indicator = null!;
	private AverageDirectionalIndex _laguerre4Indicator = null!;

	private LaguerrePlusDiState _laguerre1State = null!;
	private LaguerrePlusDiState _laguerre2State = null!;
	private LaguerrePlusDiState _laguerre3State = null!;
	private LaguerrePlusDiState _laguerre4State = null!;

	private SilverTrendState _silverTrend1State = null!;
	private SilverTrendState _silverTrend2State = null!;

	private decimal? _takeProfitPrice;
	private decimal? _stopLossPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="NeuroNirvamanMq4Strategy"/> class.
	/// </summary>
	public NeuroNirvamanMq4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to request candles", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Order volume expressed in lots", "Trading")
			.SetGreaterThanZero();

		_laguerreGamma = Param(nameof(LaguerreGamma), 0.764m)
			.SetRange(0m, 1m)
			.SetDisplay("Laguerre Gamma", "Smoothing factor applied inside the Laguerre filters", "Laguerre");

		_silverTrendKmax = Param(nameof(SilverTrendKmax), 50.6m)
			.SetGreaterThanZero()
			.SetDisplay("SilverTrend Kmax", "Sensitivity factor used by the SilverTrend indicator", "SilverTrend");

		_silverTrend1Length = Param(nameof(SilverTrend1Length), 7)
			.SetDisplay("SilverTrend Length #1", "Lookback for the first SilverTrend filter", "SilverTrend #1")
			.SetGreaterThanZero();

		_laguerre1Period = Param(nameof(Laguerre1Period), 14)
			.SetDisplay("Laguerre Period #1", "ADX period for the first Laguerre stream", "Laguerre #1")
			.SetGreaterThanZero();

		_laguerre1Distance = Param(nameof(Laguerre1Distance), 0m)
			.SetDisplay("Laguerre Distance #1", "Neutral zone width around 0.5 for the first Laguerre stream (percent)", "Laguerre #1")
			.SetNotNegative();

		_x11 = Param(nameof(X11), 100m)
			.SetDisplay("Weight X11", "Weight applied to the first Laguerre activation", "Perceptron #1");

		_x12 = Param(nameof(X12), 100m)
			.SetDisplay("Weight X12", "Weight applied to the first SilverTrend swing", "Perceptron #1");

		_takeProfit1 = Param(nameof(TakeProfit1), 100m)
			.SetDisplay("Take Profit #1", "Take-profit distance in points for perceptron #1", "Risk #1")
			.SetNotNegative();

		_stopLoss1 = Param(nameof(StopLoss1), 50m)
			.SetDisplay("Stop Loss #1", "Stop-loss distance in points for perceptron #1", "Risk #1")
			.SetNotNegative();

		_silverTrend2Length = Param(nameof(SilverTrend2Length), 7)
			.SetDisplay("SilverTrend Length #2", "Lookback for the second SilverTrend filter", "SilverTrend #2")
			.SetGreaterThanZero();

		_laguerre2Period = Param(nameof(Laguerre2Period), 14)
			.SetDisplay("Laguerre Period #2", "ADX period for the second Laguerre stream", "Laguerre #2")
			.SetGreaterThanZero();

		_laguerre2Distance = Param(nameof(Laguerre2Distance), 0m)
			.SetDisplay("Laguerre Distance #2", "Neutral zone width around 0.5 for the second Laguerre stream (percent)", "Laguerre #2")
			.SetNotNegative();

		_x21 = Param(nameof(X21), 100m)
			.SetDisplay("Weight X21", "Weight applied to the second Laguerre activation", "Perceptron #2");

		_x22 = Param(nameof(X22), 100m)
			.SetDisplay("Weight X22", "Weight applied to the second SilverTrend swing", "Perceptron #2");

		_takeProfit2 = Param(nameof(TakeProfit2), 100m)
			.SetDisplay("Take Profit #2", "Take-profit distance in points for perceptron #2", "Risk #2")
			.SetNotNegative();

		_stopLoss2 = Param(nameof(StopLoss2), 50m)
			.SetDisplay("Stop Loss #2", "Stop-loss distance in points for perceptron #2", "Risk #2")
			.SetNotNegative();

		_laguerre3Period = Param(nameof(Laguerre3Period), 14)
			.SetDisplay("Laguerre Period #3", "ADX period for the third Laguerre stream", "Laguerre #3")
			.SetGreaterThanZero();

		_laguerre3Distance = Param(nameof(Laguerre3Distance), 0m)
			.SetDisplay("Laguerre Distance #3", "Neutral zone width around 0.5 for the third Laguerre stream (percent)", "Laguerre #3")
			.SetNotNegative();

		_laguerre4Period = Param(nameof(Laguerre4Period), 14)
			.SetDisplay("Laguerre Period #4", "ADX period for the fourth Laguerre stream", "Laguerre #4")
			.SetGreaterThanZero();

		_laguerre4Distance = Param(nameof(Laguerre4Distance), 0m)
			.SetDisplay("Laguerre Distance #4", "Neutral zone width around 0.5 for the fourth Laguerre stream (percent)", "Laguerre #4")
			.SetNotNegative();

		_x31 = Param(nameof(X31), 100m)
			.SetDisplay("Weight X31", "Weight applied to the third Laguerre activation", "Perceptron #3");

		_x32 = Param(nameof(X32), 100m)
			.SetDisplay("Weight X32", "Weight applied to the fourth Laguerre activation", "Perceptron #3");

		_pass = Param(nameof(Pass), 3)
			.SetDisplay("Pass", "Supervisor branch used for trade decisions", "Logic");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public decimal LaguerreGamma
	{
		get => _laguerreGamma.Value;
		set => _laguerreGamma.Value = value;
	}

	public decimal SilverTrendKmax
	{
		get => _silverTrendKmax.Value;
		set => _silverTrendKmax.Value = value;
	}

	public int SilverTrend1Length
	{
		get => _silverTrend1Length.Value;
		set => _silverTrend1Length.Value = value;
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

	public decimal TakeProfit1
	{
		get => _takeProfit1.Value;
		set => _takeProfit1.Value = value;
	}

	public decimal StopLoss1
	{
		get => _stopLoss1.Value;
		set => _stopLoss1.Value = value;
	}

	public int SilverTrend2Length
	{
		get => _silverTrend2Length.Value;
		set => _silverTrend2Length.Value = value;
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

	public decimal TakeProfit2
	{
		get => _takeProfit2.Value;
		set => _takeProfit2.Value = value;
	}

	public decimal StopLoss2
	{
		get => _stopLoss2.Value;
		set => _stopLoss2.Value = value;
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
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetTargets();
		_laguerre1State?.Reset();
		_laguerre2State?.Reset();
		_laguerre3State?.Reset();
		_laguerre4State?.Reset();
		_silverTrend1State?.Reset();
		_silverTrend2State?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Synchronize the base volume property with the parameter used by the expert.
		Volume = TradeVolume;

		var gamma = LaguerreGamma;
		_laguerre1State = new LaguerrePlusDiState(gamma);
		_laguerre2State = new LaguerrePlusDiState(gamma);
		_laguerre3State = new LaguerrePlusDiState(gamma);
		_laguerre4State = new LaguerrePlusDiState(gamma);

		// Prepare ADX indicators that will supply +DI values to the Laguerre filters.
		_laguerre1Indicator = new AverageDirectionalIndex { Length = Laguerre1Period };
		_laguerre2Indicator = new AverageDirectionalIndex { Length = Laguerre2Period };
		_laguerre3Indicator = new AverageDirectionalIndex { Length = Laguerre3Period };
		_laguerre4Indicator = new AverageDirectionalIndex { Length = Laguerre4Period };

		// Initialize SilverTrend states that emulate the Sv2 indicator used in the original EA.
		var silverTrendKmax = SilverTrendKmax;
		_silverTrend1State = new SilverTrendState(SilverTrend1Length, silverTrendKmax);
		_silverTrend2State = new SilverTrendState(SilverTrend2Length, silverTrendKmax);

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

		// Emulate MT4 protective orders by monitoring candle extremes for exits.
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
			var shortVolume = Math.Abs(Position);
			if (_stopLossPrice is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(shortVolume);
				ResetTargets();
			}
			else if (_takeProfitPrice is decimal shortTarget && candle.LowPrice <= shortTarget)
			{
				BuyMarket(shortVolume);
				ResetTargets();
			}
		}

		// Update SilverTrend calculations for both perceptrons.
		_silverTrend1State.Length = SilverTrend1Length;
		_silverTrend2State.Length = SilverTrend2Length;
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
		{
			return -1m;
		}

		if (laguerreValue < lower)
		{
			return 1m;
		}

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
			{
				cu += l0 - l1;
			}
			else
			{
				cd += l1 - l0;
			}

			if (l1 >= l2)
			{
				cu += l1 - l2;
			}
			else
			{
				cd += l2 - l1;
			}

			if (l2 >= l3)
			{
				cu += l2 - l3;
			}
			else
			{
				cd += l3 - l2;
			}

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
		private readonly List<decimal> _highs = new();
		private readonly List<decimal> _lows = new();
		private readonly decimal _kmax;
		private bool? _uptrend;
		private int _length;

		public SilverTrendState(int length, decimal kmax)
		{
			_length = Math.Max(2, length);
			_kmax = kmax;
		}

		public int Length
		{
			get => _length;
			set
			{
				var normalized = Math.Max(2, value);
				if (_length == normalized)
				{
					return;
				}

				_length = normalized;
				Reset();
			}
		}

		public int Process(decimal high, decimal low, decimal close)
		{
			_highs.Insert(0, high);
			_lows.Insert(0, low);

			var maxCount = _length;
			if (_highs.Count > maxCount)
			{
				_highs.RemoveAt(_highs.Count - 1);
				_lows.RemoveAt(_lows.Count - 1);
			}

			if (_highs.Count < maxCount)
			{
				return 0;
			}

			var ssMax = _highs[0];
			var ssMin = _lows[0];
			for (var i = 1; i < _highs.Count; i++)
			{
				var highValue = _highs[i];
				if (highValue > ssMax)
				{
					ssMax = highValue;
				}

				var lowValue = _lows[i];
				if (lowValue < ssMin)
				{
					ssMin = lowValue;
				}
			}

			var distance = ssMax - ssMin;
			if (distance == 0m)
			{
				_uptrend ??= true;
				return _uptrend.Value ? 1 : -1;
			}

			var upperOffset = distance * (_kmax / 100m);
			var lowerOffset = distance * ((100m - _kmax) / 100m);
			var smax = ssMax - upperOffset;
			var smin = ssMin + lowerOffset;

			var uptrend = _uptrend ?? (close >= smax);

			if (close > smax)
			{
				uptrend = true;
			}
			else if (close < smin)
			{
				uptrend = false;
			}

			_uptrend = uptrend;
			return uptrend ? 1 : -1;
		}

		public void Reset()
		{
			_highs.Clear();
			_lows.Clear();
			_uptrend = null;
		}
	}
}
