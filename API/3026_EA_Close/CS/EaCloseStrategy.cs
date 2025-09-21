using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy translated from the "EA Close" MQL5 expert advisor combining CCI, WMA, and Stochastic signals.
/// </summary>
public class EaCloseStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _stochasticLevelUp;
	private readonly StrategyParam<decimal> _stochasticLevelDown;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousCci;
	private decimal _previousWma;
	private decimal _previousStochastic;
	private decimal _previousOpen;
	private decimal _previousClose;
	private bool _hasPreviousValues;
	private decimal _pipStep;

	/// <summary>
	/// Trade volume used for market orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Averaging period for the Commodity Channel Index indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Absolute threshold that defines CCI extremes.
	/// </summary>
	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	/// <summary>
	/// Length of the weighted moving average trend filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period of the stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Smoothing applied to the %K stochastic line.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing applied to the %D stochastic line.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level for the stochastic %K line.
	/// </summary>
	public decimal StochasticLevelUp
	{
		get => _stochasticLevelUp.Value;
		set => _stochasticLevelUp.Value = value;
	}

	/// <summary>
	/// Oversold level for the stochastic %K line.
	/// </summary>
	public decimal StochasticLevelDown
	{
		get => _stochasticLevelDown.Value;
		set => _stochasticLevelDown.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EaCloseStrategy"/> class.
	/// </summary>
	public EaCloseStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for entries.", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 35)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance measured in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 75)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take-profit distance measured in pips.", "Risk");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Averaging period for the CCI indicator.", "Indicators");

		_cciLevel = Param(nameof(CciLevel), 120m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Level", "Absolute threshold to detect CCI extremes.", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Length of the weighted moving average filter.", "Indicators");

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Lookback period for the stochastic oscillator.", "Indicators");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K Smoothing", "Smoothing factor applied to the %K line.", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D Smoothing", "Smoothing factor applied to the %D line.", "Indicators");

		_stochasticLevelUp = Param(nameof(StochasticLevelUp), 70m)
			.SetDisplay("Stochastic Upper Level", "Overbought threshold for %K.", "Indicators");

		_stochasticLevelDown = Param(nameof(StochasticLevelDown), 30m)
			.SetDisplay("Stochastic Lower Level", "Oversold threshold for %K.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculations.", "Data");
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

		_previousCci = 0m;
		_previousWma = 0m;
		_previousStochastic = 0m;
		_previousOpen = 0m;
		_previousClose = 0m;
		_hasPreviousValues = false;
		_pipStep = 1m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		var wma = new WeightedMovingAverage
		{
			Length = MaPeriod
		};

		var stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cci, wma, stochastic, ProcessCandle)
			.Start();

		_pipStep = CalculatePipStep();

		StartProtection(
			takeProfit: new Unit(TakeProfitPips * _pipStep, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPips * _pipStep, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawIndicator(area, wma);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue, IIndicatorValue wmaValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!cciValue.IsFinal || !wmaValue.IsFinal || !stochasticValue.IsFinal)
			return;

		var stochasticTyped = (StochasticOscillatorValue)stochasticValue;
		if (stochasticTyped.K is not decimal currentStochastic)
			return;

		var currentCci = cciValue.ToDecimal();
		var currentWma = wmaValue.ToDecimal();

		if (_hasPreviousValues && IsFormedAndOnlineAndAllowTrading() && Volume > 0)
		{
			var shouldBuy = _previousCci < -CciLevel
				&& _previousStochastic < StochasticLevelDown
				&& _previousOpen > _previousWma;

			var shouldSell = _previousCci > CciLevel
				&& _previousStochastic > StochasticLevelUp
				&& _previousClose < _previousWma;

			if (shouldBuy && Position <= 0)
			{
				var volume = Volume + Math.Max(0m, -Position);
				if (volume > 0)
					BuyMarket(volume);
			}
			else if (shouldSell && Position >= 0)
			{
				var volume = Volume + Math.Max(0m, Position);
				if (volume > 0)
					SellMarket(volume);
			}
		}

		_previousCci = currentCci;
		_previousWma = currentWma;
		_previousStochastic = currentStochastic;
		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
		_hasPreviousValues = true;
	}

	private decimal CalculatePipStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 31;
	}
}
