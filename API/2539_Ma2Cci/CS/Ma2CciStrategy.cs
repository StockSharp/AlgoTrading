using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with CCI confirmation and ATR based stop distance.
/// </summary>
public class Ma2CciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _minStopPoints;

	private ExponentialMovingAverage _fastMa = null!;
	private ExponentialMovingAverage _slowMa = null!;
	private CommodityChannelIndex _cci = null!;
	private AverageTrueRange _atr = null!;

	private decimal _previousFast;
	private decimal _previousSlow;
	private decimal _previousCci;
	private bool _hasPreviousValues;
	private decimal? _stopPrice;

	/// <summary>
	/// Candle type used to receive market data.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	/// <summary>
	/// ATR period for volatility based stops.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Percentage of portfolio equity risked per trade.
	/// </summary>
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <summary>
	/// Minimum stop distance expressed in price steps.
	/// </summary>
	public int MinStopPoints { get => _minStopPoints.Value; set => _minStopPoints.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Ma2CciStrategy"/> class.
	/// </summary>
	public Ma2CciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for calculations", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 37)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 39)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "CCI length", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR length for stop calculation", "Risk Management");

		_riskPercent = Param(nameof(RiskPercent), 2m)
		.SetDisplay("Risk %", "Portfolio percentage risked per entry", "Risk Management");

		_minStopPoints = Param(nameof(MinStopPoints), 15)
		.SetGreaterThanZero()
		.SetDisplay("Min Stop Points", "Minimum stop distance in price steps", "Risk Management");
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

		_previousFast = 0m;
		_previousSlow = 0m;
		_previousCci = 0m;
		_hasPreviousValues = false;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastMa, _slowMa, _cci, _atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal cciValue, decimal atrValue)
	{
		// Process only finished candles to avoid intrabar noise.
		if (candle.State != CandleStates.Finished)
		return;

		// Wait until all indicators are fully initialized before trading.
		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_cci.IsFormed || !_atr.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_hasPreviousValues)
		{
			_previousFast = fastValue;
			_previousSlow = slowValue;
			_previousCci = cciValue;
			_hasPreviousValues = true;
			return;
		}

		var fastCrossUp = _previousFast <= _previousSlow && fastValue > slowValue;
		var fastCrossDown = _previousFast >= _previousSlow && fastValue < slowValue;
		var cciCrossUp = _previousCci <= 0m && cciValue > 0m;
		var cciCrossDown = _previousCci >= 0m && cciValue < 0m;

		var stopDistance = Math.Max(atrValue, GetMinStopDistance());

		if (Position != 0m)
		{
			var exitTriggered = false;

			if (Position > 0m)
			{
				// Close long positions on stop hit or bearish crossover.
				if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
				{
					SellMarket(Position);
					exitTriggered = true;
				}
				else if (fastCrossDown)
				{
					SellMarket(Position);
					exitTriggered = true;
				}
			}
			else if (Position < 0m)
			{
				// Close short positions on stop hit or bullish crossover.
				if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
				{
					BuyMarket(-Position);
					exitTriggered = true;
				}
				else if (fastCrossUp)
				{
					BuyMarket(-Position);
					exitTriggered = true;
				}
			}

			if (exitTriggered)
			{
				_stopPrice = null;
				_previousFast = fastValue;
				_previousSlow = slowValue;
				_previousCci = cciValue;
				return;
			}
		}
		else
		{
			// Enter long when EMA and CCI confirm bullish momentum.
			if (fastCrossUp && cciCrossUp)
			{
				var volume = CalculateVolume(stopDistance);
				if (volume > 0m)
				{
					BuyMarket(volume);
					_stopPrice = NormalizePrice(candle.ClosePrice - stopDistance);
				}
			}
			// Enter short when EMA and CCI confirm bearish momentum.
			else if (fastCrossDown && cciCrossDown)
			{
				var volume = CalculateVolume(stopDistance);
				if (volume > 0m)
				{
					SellMarket(volume);
					_stopPrice = NormalizePrice(candle.ClosePrice + stopDistance);
				}
			}
		}

		_previousFast = fastValue;
		_previousSlow = slowValue;
		_previousCci = cciValue;
	}

	private decimal CalculateVolume(decimal stopDistance)
	{
		if (stopDistance <= 0m)
		return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * (RiskPercent / 100m);

		if (riskAmount <= 0m)
		return NormalizeVolume(GetBaseVolume());

		var rawVolume = riskAmount / stopDistance;
		if (rawVolume <= 0m)
		return NormalizeVolume(GetBaseVolume());

		return NormalizeVolume(rawVolume);
	}

	private decimal GetBaseVolume()
	{
		var volume = Volume;
		if (volume > 0m)
		return volume;

		var step = Security?.VolumeStep ?? 1m;
		var min = Security?.MinVolume ?? step;
		return min > 0m ? min : step;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
		return Math.Max(volume, 0m);

		var normalized = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;
		var min = Security?.MinVolume ?? step;
		if (normalized < min)
		normalized = min;

		var max = Security?.MaxVolume;
		if (max.HasValue && max.Value > 0m && normalized > max.Value)
		normalized = max.Value;

		return Math.Max(normalized, 0m);
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (!step.HasValue || step.Value <= 0m)
		return price;

		var rounded = Math.Round(price / step.Value, MidpointRounding.AwayFromZero) * step.Value;
		return rounded;
	}

	private decimal GetMinStopDistance()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step * MinStopPoints : MinStopPoints;
	}
}
