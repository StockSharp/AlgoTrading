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
/// Stochastic crossover strategy with envelope-based exits converted from the "EA Vishal EURGBP H4" MQL expert advisor.
/// </summary>
public class EaVishalEurgbpH4Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<decimal> _envelopeDeviationPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;

	private decimal? _prevK1;
	private decimal? _prevD1;
	private decimal? _prevK2;
	private decimal? _prevD2;

	private decimal? _prevUpper1;
	private decimal? _prevUpper2;
	private decimal? _prevLower1;
	private decimal? _prevLower2;

	private decimal? _prevOpen1;
	private decimal? _prevHigh1;
	private decimal? _prevLow1;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Default trade volume in lots.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips (0 disables hard stop-loss).
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips (0 disables the target).
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables virtual trailing stop management when a stop-loss is defined.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing factor.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Moving average length used as the envelope basis.
	/// </summary>
	public int EnvelopePeriod
	{
		get => _envelopePeriod.Value;
		set => _envelopePeriod.Value = value;
	}

	/// <summary>
	/// Envelope deviation expressed as a percentage.
	/// </summary>
	public decimal EnvelopeDeviationPercent
	{
		get => _envelopeDeviationPercent.Value;
		set => _envelopeDeviationPercent.Value = value;
	}

	/// <summary>
	/// Candle type that drives the strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EaVishalEurgbpH4Strategy"/>.
	/// </summary>
	public EaVishalEurgbpH4Strategy()
	{
		_volume = Param(nameof(Volume), 0.5m)
			.SetDisplay("Volume", "Default trade volume", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 0)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 22)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetNotNegative();

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Enable virtual trailing stop management", "Risk");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 6)
			.SetDisplay("Stochastic %K", "%K lookback period", "Indicators")
			.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("Stochastic %D", "%D smoothing period", "Indicators")
			.SetGreaterThanZero();

		_stochasticSlowing = Param(nameof(StochasticSlowing), 1)
			.SetDisplay("Stochastic Slowing", "%K slowing value", "Indicators")
			.SetGreaterThanZero();

		_envelopePeriod = Param(nameof(EnvelopePeriod), 32)
			.SetDisplay("Envelope Period", "Moving average period for envelopes", "Indicators")
			.SetGreaterThanZero();

		_envelopeDeviationPercent = Param(nameof(EnvelopeDeviationPercent), 0.3m)
			.SetDisplay("Envelope Deviation %", "Envelope deviation in percent", "Indicators")
			.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");
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

		_pipSize = 0m;

		_prevK1 = null;
		_prevD1 = null;
		_prevK2 = null;
		_prevD2 = null;

		_prevUpper1 = null;
		_prevUpper2 = null;
		_prevLower1 = null;
		_prevLower2 = null;

		_prevOpen1 = null;
		_prevHigh1 = null;
		_prevLow1 = null;

		ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();

		var stochastic = new StochasticOscillator
		{
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod,
			Smooth = StochasticSlowing
		};

		var basis = new SimpleMovingAverage
		{
			Length = EnvelopePeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, basis, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, basis);

			var oscArea = CreateChartArea();
			DrawIndicator(oscArea, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue, IIndicatorValue basisValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stochasticValue is not StochasticOscillatorValue stoch ||
			stoch.K is not decimal currentK ||
			stoch.D is not decimal currentD)
		{
			return;
		}

		var sma = basisValue.ToDecimal();
		if (sma == 0m)
			return;

		if (_pipSize <= 0m)
			_pipSize = GetPipSize();

		var deviation = EnvelopeDeviationPercent / 100m;
		var currentUpper = sma * (1m + deviation);
		var currentLower = sma * (1m - deviation);

		var currentOpen = candle.OpenPrice;

		if (Position > 0)
		{
			if (TryExitLongByEnvelope(currentOpen))
			{
				ResetProtection();
			}
			else
			{
				UpdateTrailingStop(PositionSide.Long, candle);
				if (TryExitLongByRisk(candle))
				{
					ResetProtection();
				}
			}
		}
		else if (Position < 0)
		{
			if (TryExitShortByEnvelope(currentOpen))
			{
				ResetProtection();
			}
			else
			{
				UpdateTrailingStop(PositionSide.Short, candle);
				if (TryExitShortByRisk(candle))
				{
					ResetProtection();
				}
			}
		}

		if (Position == 0)
		{
			if (_prevK1 is decimal prevK &&
				_prevD1 is decimal prevD &&
				_prevK2 is decimal prevK2 &&
				_prevD2 is decimal prevD2)
			{
				var longSignal = prevK < prevD && prevK2 > prevD2;
				var shortSignal = prevK > prevD && prevK2 < prevD2;

				if (longSignal && !shortSignal)
				{
					EnterLong(candle.ClosePrice);
				}
				else if (shortSignal && !longSignal)
				{
					EnterShort(candle.ClosePrice);
				}
			}
		}

		_prevK2 = _prevK1;
		_prevD2 = _prevD1;
		_prevK1 = currentK;
		_prevD1 = currentD;

		_prevUpper2 = _prevUpper1;
		_prevLower2 = _prevLower1;
		_prevUpper1 = currentUpper;
		_prevLower1 = currentLower;

		_prevOpen1 = currentOpen;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;
	}

	private bool TryExitLongByEnvelope(decimal currentOpen)
	{
		if (_prevUpper1 is decimal previousUpper &&
			_prevUpper2 is decimal twoAgoUpper &&
			_prevOpen1 is decimal previousOpen)
		{
			if (currentOpen > previousUpper && previousOpen < twoAgoUpper)
			{
				SellMarket(Position);
				return true;
			}
		}

		return false;
	}

	private bool TryExitShortByEnvelope(decimal currentOpen)
	{
		if (_prevLower1 is decimal previousLower &&
			_prevLower2 is decimal twoAgoLower &&
			_prevOpen1 is decimal previousOpen)
		{
			if (currentOpen < previousLower && previousOpen > twoAgoLower)
			{
				BuyMarket(-Position);
				return true;
			}
		}

		return false;
	}

	private bool TryExitLongByRisk(ICandleMessage candle)
	{
		var closed = false;

		if (!closed && _longTakePrice is decimal take && candle.HighPrice >= take)
		{
			closed = true;
		}

		if (!closed && _longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			closed = true;
		}

		if (closed)
		{
			SellMarket(Position);
		}

		return closed;
	}

	private bool TryExitShortByRisk(ICandleMessage candle)
	{
		var closed = false;

		if (!closed && _shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			closed = true;
		}

		if (!closed && _shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			closed = true;
		}

		if (closed)
		{
			BuyMarket(-Position);
		}

		return closed;
	}

	private void EnterLong(decimal entryPrice)
	{
		if (Volume <= 0m)
			return;

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
		_longTakePrice = takeDistance > 0m ? entryPrice + takeDistance : null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		BuyMarket(Volume);
	}

	private void EnterShort(decimal entryPrice)
	{
		if (Volume <= 0m)
			return;

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
		_shortTakePrice = takeDistance > 0m ? entryPrice - takeDistance : null;
		_longStopPrice = null;
		_longTakePrice = null;

		SellMarket(Volume);
	}

	private void UpdateTrailingStop(PositionSide side, ICandleMessage candle)
	{
		if (!UseTrailingStop || StopLossPips <= 0 || _pipSize <= 0m)
			return;

		var distance = StopLossPips * _pipSize;

		switch (side)
		{
			case PositionSide.Long when _prevHigh1 is decimal prevHigh:
			{
				var desiredStop = prevHigh - distance;
				if (_longStopPrice is not decimal currentStop || desiredStop > currentStop)
				{
					_longStopPrice = desiredStop;
				}
				break;
			}
			case PositionSide.Short when _prevLow1 is decimal prevLow:
			{
				var desiredStop = prevLow + distance;
				if (_shortStopPrice is not decimal currentStop || desiredStop < currentStop)
				{
					_shortStopPrice = desiredStop;
				}
				break;
			}
		}
	}

	private void ResetProtection()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals;

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private enum PositionSide
	{
		Long,
		Short
	}
}

