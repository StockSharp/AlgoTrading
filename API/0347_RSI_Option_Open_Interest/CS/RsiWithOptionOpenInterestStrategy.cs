using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI strategy filtered by deterministic option open-interest spikes.
/// </summary>
public class RsiWithOptionOpenInterestStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _oiPeriod;
	private readonly StrategyParam<decimal> _oiDeviationFactor;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi = null!;
	private SimpleMovingAverage _callOiSma = null!;
	private SimpleMovingAverage _putOiSma = null!;
	private StandardDeviation _callOiStdDev = null!;
	private StandardDeviation _putOiStdDev = null!;

	private decimal _currentCallOi;
	private decimal _currentPutOi;
	private decimal _avgCallOi;
	private decimal _avgPutOi;
	private decimal _stdDevCallOi;
	private decimal _stdDevPutOi;
	private decimal? _prevRsi;
	private bool _prevCallOiSpike;
	private bool _prevPutOiSpike;
	private int _cooldownRemaining;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Open interest averaging period.
	/// </summary>
	public int OiPeriod
	{
		get => _oiPeriod.Value;
		set => _oiPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for OI threshold.
	/// </summary>
	public decimal OiDeviationFactor
	{
		get => _oiDeviationFactor.Value;
		set => _oiDeviationFactor.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public RsiWithOptionOpenInterestStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_oiPeriod = Param(nameof(OiPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("OI Period", "Period for open interest averaging", "Options");

		_oiDeviationFactor = Param(nameof(OiDeviationFactor), 2.5m)
			.SetRange(1m, 4m)
			.SetDisplay("OI StdDev Factor", "Standard deviation multiplier for OI threshold", "Options");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetRange(1m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 18)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General");
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

		_rsi?.Reset();
		_callOiSma?.Reset();
		_putOiSma?.Reset();
		_callOiStdDev?.Reset();
		_putOiStdDev?.Reset();

		_rsi = null!;
		_callOiSma = null!;
		_putOiSma = null!;
		_callOiStdDev = null!;
		_putOiStdDev = null!;

		_currentCallOi = 0m;
		_currentPutOi = 0m;
		_avgCallOi = 0m;
		_avgPutOi = 0m;
		_stdDevCallOi = 0m;
		_stdDevPutOi = 0m;
		_prevRsi = null;
		_prevCallOiSpike = false;
		_prevPutOiSpike = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_callOiSma = new SimpleMovingAverage
		{
			Length = OiPeriod
		};

		_callOiStdDev = new StandardDeviation
		{
			Length = OiPeriod
		};

		_putOiSma = new SimpleMovingAverage
		{
			Length = OiPeriod
		};

		_putOiStdDev = new StandardDeviation
		{
			Length = OiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		SimulateOptionOi(candle);

		var callOiValueSma = _callOiSma.Process(new DecimalIndicatorValue(_callOiSma, _currentCallOi, candle.OpenTime) { IsFinal = true });
		var putOiValueSma = _putOiSma.Process(new DecimalIndicatorValue(_putOiSma, _currentPutOi, candle.OpenTime) { IsFinal = true });
		var callOiValueStdDev = _callOiStdDev.Process(new DecimalIndicatorValue(_callOiStdDev, _currentCallOi, candle.OpenTime) { IsFinal = true });
		var putOiValueStdDev = _putOiStdDev.Process(new DecimalIndicatorValue(_putOiStdDev, _currentPutOi, candle.OpenTime) { IsFinal = true });

		if (!_callOiSma.IsFormed || !_putOiSma.IsFormed || !_callOiStdDev.IsFormed || !_putOiStdDev.IsFormed ||
			callOiValueSma.IsEmpty || putOiValueSma.IsEmpty || callOiValueStdDev.IsEmpty || putOiValueStdDev.IsEmpty)
		{
			_prevRsi = rsi;
			return;
		}

		_avgCallOi = callOiValueSma.ToDecimal();
		_avgPutOi = putOiValueSma.ToDecimal();
		_stdDevCallOi = callOiValueStdDev.ToDecimal();
		_stdDevPutOi = putOiValueStdDev.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsi;
			return;
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var callOiThreshold = _avgCallOi + (OiDeviationFactor * _stdDevCallOi);
		var putOiThreshold = _avgPutOi + (OiDeviationFactor * _stdDevPutOi);
		var callOiSpike = _currentCallOi > callOiThreshold;
		var putOiSpike = _currentPutOi > putOiThreshold;
		var callOiSpikeTransition = !_prevCallOiSpike && callOiSpike;
		var putOiSpikeTransition = !_prevPutOiSpike && putOiSpike;
		var oversoldCross = _prevRsi is decimal previousRsi && previousRsi >= 35m && rsi < 35m;
		var overboughtCross = _prevRsi is decimal previousRsi2 && previousRsi2 <= 65m && rsi > 65m;

		if (_cooldownRemaining == 0 && oversoldCross && callOiSpikeTransition && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (_cooldownRemaining == 0 && overboughtCross && putOiSpikeTransition && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position > 0 && rsi >= 52m)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && rsi <= 48m)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevRsi = rsi;
		_prevCallOiSpike = callOiSpike;
		_prevPutOiSpike = putOiSpike;
	}

	private void SimulateOptionOi(ICandleMessage candle)
	{
		var range = Math.Max(candle.HighPrice - candle.LowPrice, 1m);
		var body = candle.ClosePrice - candle.OpenPrice;
		var bodyRatio = Math.Abs(body) / range;
		var rangeRatio = range / Math.Max(candle.OpenPrice, 1m);
		var baseOi = Math.Max(candle.TotalVolume, 1m);
		var spikeFactor = 1m + Math.Min(0.75m, (bodyRatio * 0.5m) + (rangeRatio * 20m));

		if (body >= 0)
		{
			_currentCallOi = baseOi * spikeFactor;
			_currentPutOi = baseOi * (0.75m + (1m - bodyRatio) * 0.25m);
		}
		else
		{
			_currentCallOi = baseOi * (0.75m + (1m - bodyRatio) * 0.25m);
			_currentPutOi = baseOi * spikeFactor;
		}
	}
}
