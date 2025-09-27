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
/// Reimplementation of the MetaTrader expert advisor "Combo_Right.mq4".
/// Combines a CCI filter with three perceptrons that evaluate open price momentum across configurable spans.
/// </summary>
public class ComboRightPerceptronStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit1;
	private readonly StrategyParam<decimal> _stopLoss1;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _x12;
	private readonly StrategyParam<int> _x22;
	private readonly StrategyParam<int> _x32;
	private readonly StrategyParam<int> _x42;
	private readonly StrategyParam<decimal> _takeProfit2;
	private readonly StrategyParam<decimal> _stopLoss2;
	private readonly StrategyParam<int> _perceptron1Period;
	private readonly StrategyParam<int> _x13;
	private readonly StrategyParam<int> _x23;
	private readonly StrategyParam<int> _x33;
	private readonly StrategyParam<int> _x43;
	private readonly StrategyParam<decimal> _takeProfit3;
	private readonly StrategyParam<decimal> _stopLoss3;
	private readonly StrategyParam<int> _perceptron2Period;
	private readonly StrategyParam<int> _x14;
	private readonly StrategyParam<int> _x24;
	private readonly StrategyParam<int> _x34;
	private readonly StrategyParam<int> _x44;
	private readonly StrategyParam<int> _perceptron3Period;
	private readonly StrategyParam<int> _passMode;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private CCI _cci;

	private decimal[] _openBuffer = Array.Empty<decimal>();
	private int _barIndex;
	private int _requiredHistory;

	private decimal _perceptron1W1;
	private decimal _perceptron1W2;
	private decimal _perceptron1W3;
	private decimal _perceptron1W4;

	private decimal _perceptron2W1;
	private decimal _perceptron2W2;
	private decimal _perceptron2W3;
	private decimal _perceptron2W4;

	private decimal _perceptron3W1;
	private decimal _perceptron3W2;
	private decimal _perceptron3W3;
	private decimal _perceptron3W4;

/// <summary>
/// Initializes the strategy parameters and optimization metadata.
/// </summary>
	public ComboRightPerceptronStrategy()
	{
		_takeProfit1 = Param(nameof(TakeProfit1), 50m)
		.SetDisplay("Take Profit 1", "Profit target in points for the base CCI signal", "Risk Management")
		.SetGreaterThanOrEqual(0m)
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_stopLoss1 = Param(nameof(StopLoss1), 50m)
		.SetDisplay("Stop Loss 1", "Stop distance in points for the base CCI signal", "Risk Management")
		.SetGreaterThanOrEqual(0m)
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_cciPeriod = Param(nameof(CciPeriod), 10)
		.SetDisplay("CCI Period", "Period of the Commodity Channel Index filter", "Indicators")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_x12 = Param(nameof(X12), 100)
		.SetDisplay("X12", "Weight applied to the first perceptron difference #1", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x22 = Param(nameof(X22), 100)
		.SetDisplay("X22", "Weight applied to the first perceptron difference #2", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x32 = Param(nameof(X32), 100)
		.SetDisplay("X32", "Weight applied to the first perceptron difference #3", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x42 = Param(nameof(X42), 100)
		.SetDisplay("X42", "Weight applied to the first perceptron difference #4", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_takeProfit2 = Param(nameof(TakeProfit2), 50m)
		.SetDisplay("Take Profit 2", "Profit target in points for the bearish perceptron", "Risk Management")
		.SetGreaterThanOrEqual(0m)
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_stopLoss2 = Param(nameof(StopLoss2), 50m)
		.SetDisplay("Stop Loss 2", "Stop distance in points for the bearish perceptron", "Risk Management")
		.SetGreaterThanOrEqual(0m)
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_perceptron1Period = Param(nameof(Perceptron1Period), 20)
		.SetDisplay("Perceptron 1 Period", "Stride (bars) between samples for the bearish perceptron", "Perceptrons")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_x13 = Param(nameof(X13), 100)
		.SetDisplay("X13", "Weight applied to the second perceptron difference #1", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x23 = Param(nameof(X23), 100)
		.SetDisplay("X23", "Weight applied to the second perceptron difference #2", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x33 = Param(nameof(X33), 100)
		.SetDisplay("X33", "Weight applied to the second perceptron difference #3", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x43 = Param(nameof(X43), 100)
		.SetDisplay("X43", "Weight applied to the second perceptron difference #4", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_takeProfit3 = Param(nameof(TakeProfit3), 50m)
		.SetDisplay("Take Profit 3", "Profit target in points for the bullish perceptron", "Risk Management")
		.SetGreaterThanOrEqual(0m)
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_stopLoss3 = Param(nameof(StopLoss3), 50m)
		.SetDisplay("Stop Loss 3", "Stop distance in points for the bullish perceptron", "Risk Management")
		.SetGreaterThanOrEqual(0m)
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_perceptron2Period = Param(nameof(Perceptron2Period), 20)
		.SetDisplay("Perceptron 2 Period", "Stride (bars) between samples for the bullish perceptron", "Perceptrons")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_x14 = Param(nameof(X14), 100)
		.SetDisplay("X14", "Weight applied to the confirmation perceptron difference #1", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x24 = Param(nameof(X24), 100)
		.SetDisplay("X24", "Weight applied to the confirmation perceptron difference #2", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x34 = Param(nameof(X34), 100)
		.SetDisplay("X34", "Weight applied to the confirmation perceptron difference #3", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_x44 = Param(nameof(X44), 100)
		.SetDisplay("X44", "Weight applied to the confirmation perceptron difference #4", "Perceptrons")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 5);

		_perceptron3Period = Param(nameof(Perceptron3Period), 20)
		.SetDisplay("Perceptron 3 Period", "Stride (bars) between samples for the confirmation perceptron", "Perceptrons")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_passMode = Param(nameof(PassMode), 1)
		.SetDisplay("Pass", "Supervisor mode reproduced from the MQL expert", "Trading")
		.SetGreaterThanOrEqual(1)
		.SetLessThanOrEqual(4);

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetDisplay("Trade Volume", "Order volume used for new entries", "Trading")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for signal calculations", "General");
	}

/// <summary>
/// Profit target in points for the base CCI signal.
/// </summary>
	public decimal TakeProfit1
	{
		get => _takeProfit1.Value;
		set => _takeProfit1.Value = value;
	}

/// <summary>
/// Stop distance in points for the base CCI signal.
/// </summary>
	public decimal StopLoss1
	{
		get => _stopLoss1.Value;
		set => _stopLoss1.Value = value;
	}

/// <summary>
/// Period of the Commodity Channel Index filter.
/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

/// <summary>
/// Raw weight parameter for the first perceptron difference #1.
/// </summary>
	public int X12
	{
		get => _x12.Value;
		set => _x12.Value = value;
	}

/// <summary>
/// Raw weight parameter for the first perceptron difference #2.
/// </summary>
	public int X22
	{
		get => _x22.Value;
		set => _x22.Value = value;
	}

/// <summary>
/// Raw weight parameter for the first perceptron difference #3.
/// </summary>
	public int X32
	{
		get => _x32.Value;
		set => _x32.Value = value;
	}

/// <summary>
/// Raw weight parameter for the first perceptron difference #4.
/// </summary>
	public int X42
	{
		get => _x42.Value;
		set => _x42.Value = value;
	}

/// <summary>
/// Profit target in points for the bearish perceptron.
/// </summary>
	public decimal TakeProfit2
	{
		get => _takeProfit2.Value;
		set => _takeProfit2.Value = value;
	}

/// <summary>
/// Stop distance in points for the bearish perceptron.
/// </summary>
	public decimal StopLoss2
	{
		get => _stopLoss2.Value;
		set => _stopLoss2.Value = value;
	}

/// <summary>
/// Stride (bars) between samples for the bearish perceptron.
/// </summary>
	public int Perceptron1Period
	{
		get => _perceptron1Period.Value;
		set => _perceptron1Period.Value = value;
	}

/// <summary>
/// Raw weight parameter for the second perceptron difference #1.
/// </summary>
	public int X13
	{
		get => _x13.Value;
		set => _x13.Value = value;
	}

/// <summary>
/// Raw weight parameter for the second perceptron difference #2.
/// </summary>
	public int X23
	{
		get => _x23.Value;
		set => _x23.Value = value;
	}

/// <summary>
/// Raw weight parameter for the second perceptron difference #3.
/// </summary>
	public int X33
	{
		get => _x33.Value;
		set => _x33.Value = value;
	}

/// <summary>
/// Raw weight parameter for the second perceptron difference #4.
/// </summary>
	public int X43
	{
		get => _x43.Value;
		set => _x43.Value = value;
	}

/// <summary>
/// Profit target in points for the bullish perceptron.
/// </summary>
	public decimal TakeProfit3
	{
		get => _takeProfit3.Value;
		set => _takeProfit3.Value = value;
	}

/// <summary>
/// Stop distance in points for the bullish perceptron.
/// </summary>
	public decimal StopLoss3
	{
		get => _stopLoss3.Value;
		set => _stopLoss3.Value = value;
	}

/// <summary>
/// Stride (bars) between samples for the bullish perceptron.
/// </summary>
	public int Perceptron2Period
	{
		get => _perceptron2Period.Value;
		set => _perceptron2Period.Value = value;
	}

/// <summary>
/// Raw weight parameter for the confirmation perceptron difference #1.
/// </summary>
	public int X14
	{
		get => _x14.Value;
		set => _x14.Value = value;
	}

/// <summary>
/// Raw weight parameter for the confirmation perceptron difference #2.
/// </summary>
	public int X24
	{
		get => _x24.Value;
		set => _x24.Value = value;
	}

/// <summary>
/// Raw weight parameter for the confirmation perceptron difference #3.
/// </summary>
	public int X34
	{
		get => _x34.Value;
		set => _x34.Value = value;
	}

/// <summary>
/// Raw weight parameter for the confirmation perceptron difference #4.
/// </summary>
	public int X44
	{
		get => _x44.Value;
		set => _x44.Value = value;
	}

/// <summary>
/// Stride (bars) between samples for the confirmation perceptron.
/// </summary>
	public int Perceptron3Period
	{
		get => _perceptron3Period.Value;
		set => _perceptron3Period.Value = value;
	}

/// <summary>
/// Supervisor mode reproduced from the MetaTrader expert.
/// </summary>
	public int PassMode
	{
		get => _passMode.Value;
		set => _passMode.Value = value;
	}

/// <summary>
/// Order volume used for new entries.
/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

/// <summary>
/// Candle series used for signal calculations.
/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_cci = null;
		_openBuffer = Array.Empty<decimal>();
		_barIndex = 0;
		_requiredHistory = 0;
	}

/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_perceptron1W1 = X12 - 100m;
		_perceptron1W2 = X22 - 100m;
		_perceptron1W3 = X32 - 100m;
		_perceptron1W4 = X42 - 100m;

		_perceptron2W1 = X13 - 100m;
		_perceptron2W2 = X23 - 100m;
		_perceptron2W3 = X33 - 100m;
		_perceptron2W4 = X43 - 100m;

		_perceptron3W1 = X14 - 100m;
		_perceptron3W2 = X24 - 100m;
		_perceptron3W3 = X34 - 100m;
		_perceptron3W4 = X44 - 100m;

		_requiredHistory = Math.Max(Math.Max(Perceptron1Period * 4, Perceptron2Period * 4), Perceptron3Period * 4) + 1;
		var bufferLength = Math.Max(_requiredHistory + 4, 32);
		_openBuffer = new decimal[bufferLength];
		_barIndex = 0;

		_cci = new CCI
		{
			Length = CciPeriod,
			Type = IndicatorPriceTypes.Open
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var len = _openBuffer.Length;
		_openBuffer[_barIndex % len] = candle.OpenPrice;
		_barIndex++;

		if (_barIndex <= _requiredHistory)
		return;

		if (_cci is null || !_cci.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var (signal, takeSteps, stopSteps) = EvaluateSignal(candle.ClosePrice, cciValue);

		if (signal > 0m && Position <= 0m)
		{
			EnterPosition(true, candle.ClosePrice, takeSteps, stopSteps);
		}
		else if (signal < 0m && Position >= 0m)
		{
			EnterPosition(false, candle.ClosePrice, takeSteps, stopSteps);
		}
	}

	private (decimal signal, decimal takeSteps, decimal stopSteps) EvaluateSignal(decimal closePrice, decimal cciValue)
	{
		var signal = cciValue;
		var takeSteps = TakeProfit1;
		var stopSteps = StopLoss1;

		if (PassMode == 4)
		{
			var perceptron3 = CalculatePerceptron(closePrice, Perceptron3Period, _perceptron3W1, _perceptron3W2, _perceptron3W3, _perceptron3W4);
			var perceptron2 = CalculatePerceptron(closePrice, Perceptron2Period, _perceptron2W1, _perceptron2W2, _perceptron2W3, _perceptron2W4);
			var perceptron1 = CalculatePerceptron(closePrice, Perceptron1Period, _perceptron1W1, _perceptron1W2, _perceptron1W3, _perceptron1W4);

			if (perceptron3 is null || perceptron2 is null || perceptron1 is null)
			return (0m, 0m, 0m);

			if (perceptron3 > 0m)
			{
				if (perceptron2 > 0m)
				{
					signal = 1m;
					takeSteps = TakeProfit3;
					stopSteps = StopLoss3;
				}
			}
			else if (perceptron1 < 0m)
			{
				signal = -1m;
				takeSteps = TakeProfit2;
				stopSteps = StopLoss2;
			}

			return (signal, takeSteps, stopSteps);
		}

		if (PassMode == 3)
		{
			var perceptron2 = CalculatePerceptron(closePrice, Perceptron2Period, _perceptron2W1, _perceptron2W2, _perceptron2W3, _perceptron2W4);
			if (perceptron2 is null)
			return (0m, 0m, 0m);

			if (perceptron2 > 0m)
			{
				signal = 1m;
				takeSteps = TakeProfit3;
				stopSteps = StopLoss3;
			}

			return (signal, takeSteps, stopSteps);
		}

		if (PassMode == 2)
		{
			var perceptron1 = CalculatePerceptron(closePrice, Perceptron1Period, _perceptron1W1, _perceptron1W2, _perceptron1W3, _perceptron1W4);
			if (perceptron1 is null)
			return (0m, 0m, 0m);

			if (perceptron1 < 0m)
			{
				signal = -1m;
				takeSteps = TakeProfit2;
				stopSteps = StopLoss2;
			}

			return (signal, takeSteps, stopSteps);
		}

		return (signal, takeSteps, stopSteps);
	}

	private decimal? CalculatePerceptron(decimal closePrice, int period, decimal w1, decimal w2, decimal w3, decimal w4)
	{
		if (period <= 0)
		return null;

		var len = _openBuffer.Length;
		var maxShift = period * 4;
		if (_barIndex <= maxShift)
		return null;

		var open1 = GetOpen(period, len);
		var open2 = GetOpen(period * 2, len);
		var open3 = GetOpen(period * 3, len);
		var open4 = GetOpen(period * 4, len);

		var a1 = closePrice - open1;
		var a2 = open1 - open2;
		var a3 = open2 - open3;
		var a4 = open3 - open4;

		return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;
	}

	private decimal GetOpen(int shift, int len)
	{
		var index = (_barIndex - 1 - shift) % len;
		if (index < 0)
		index += len;
		return _openBuffer[index];
	}

	private void EnterPosition(bool isLong, decimal referencePrice, decimal takeSteps, decimal stopSteps)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		CancelActiveOrders();

		decimal resultingPosition;
		if (isLong)
		{
			var volumeToBuy = volume + Math.Max(0m, -Position);
			if (volumeToBuy <= 0m)
			return;

			resultingPosition = Position + volumeToBuy;
			BuyMarket(volumeToBuy);
		}
		else
		{
			var volumeToSell = volume + Math.Max(0m, Position);
			if (volumeToSell <= 0m)
			return;

			resultingPosition = Position - volumeToSell;
			SellMarket(volumeToSell);
		}

		ApplyProtection(referencePrice, resultingPosition, takeSteps, stopSteps);
	}

	private void ApplyProtection(decimal referencePrice, decimal resultingPosition, decimal takeSteps, decimal stopSteps)
	{
		var takeOffset = GetPriceOffset(takeSteps);
		var stopOffset = GetPriceOffset(stopSteps);

		if (takeOffset > 0m)
		SetTakeProfit(takeOffset, referencePrice, resultingPosition);

		if (stopOffset > 0m)
		SetStopLoss(stopOffset, referencePrice, resultingPosition);
	}

	private decimal GetPriceOffset(decimal distanceInPoints)
	{
		if (distanceInPoints <= 0m)
		return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return distanceInPoints;

		return distanceInPoints * step;
	}
}

