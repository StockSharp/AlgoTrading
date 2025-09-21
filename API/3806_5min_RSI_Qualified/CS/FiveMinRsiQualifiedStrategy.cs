using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Five-minute RSI qualified swing strategy converted from MetaTrader.
/// Opens contrarian positions after RSI stays in an extreme zone for a fixed number of candles.
/// Trailing stops follow the previous candle's close, matching the original EA behaviour.
/// </summary>
public class FiveMinRsiQualifiedStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _qualificationLength;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _initialStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly RelativeStrengthIndex _rsi = new();

	private int _overboughtCount;
	private int _oversoldCount;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// RSI lookback length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of consecutive candles required for a qualified signal.
	/// </summary>
	public int QualificationLength
	{
		get => _qualificationLength.Value;
		set => _qualificationLength.Value = value;
	}

	/// <summary>
	/// RSI level that qualifies a bearish setup.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// RSI level that qualifies a bullish setup.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initial protective stop distance expressed in MetaTrader points.
	/// </summary>
	public decimal InitialStopPoints
	{
		get => _initialStopPoints.Value;
		set => _initialStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialise <see cref="FiveMinRsiQualifiedStrategy"/>.
	/// </summary>
	public FiveMinRsiQualifiedStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of candles used for RSI", "Indicator")
			.SetCanOptimize(true);

		_qualificationLength = Param(nameof(QualificationLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Qualification Length", "Consecutive candles required for a qualified signal", "Indicator")
			.SetCanOptimize(true);

		_upperThreshold = Param(nameof(UpperThreshold), 55m)
			.SetDisplay("Upper Threshold", "RSI level that triggers short setups", "Indicator")
			.SetCanOptimize(true);

		_lowerThreshold = Param(nameof(LowerThreshold), 45m)
			.SetDisplay("Lower Threshold", "RSI level that triggers long setups", "Indicator")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 21m)
			.SetDisplay("Trailing Stop (points)", "Distance in MetaTrader points used for trailing stops", "Risk")
			.SetCanOptimize(true);

		_initialStopPoints = Param(nameof(InitialStopPoints), 11m)
			.SetDisplay("Initial Stop (points)", "Initial protective stop distance in MetaTrader points", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for RSI evaluation", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi.Length = RsiPeriod;
		_rsi.Reset();
		_overboughtCount = 0;
		_oversoldCount = 0;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi.Length = RsiPeriod;

		StartProtection();

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
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (UpdateTrailingStops(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateQualificationCounters(rsiValue);

		if (Position != 0m)
			return;

		if (_overboughtCount >= QualificationLength && rsiValue >= UpperThreshold)
		{
			SellMarket();
			return;
		}

		if (_oversoldCount >= QualificationLength && rsiValue <= LowerThreshold)
			BuyMarket();
	}

	private void UpdateQualificationCounters(decimal rsiValue)
	{
		if (rsiValue >= UpperThreshold)
		{
			_overboughtCount = Math.Min(_overboughtCount + 1, QualificationLength);
		}
		else
		{
			_overboughtCount = 0;
		}

		if (rsiValue <= LowerThreshold)
		{
			_oversoldCount = Math.Min(_oversoldCount + 1, QualificationLength);
		}
		else
		{
			_oversoldCount = 0;
		}
	}

	private bool UpdateTrailingStops(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var distance = ToPrice(StopLossPoints);
			if (distance > 0m)
			{
				var candidate = RoundPrice(candle.ClosePrice - distance);
				if (_longStopPrice.HasValue)
				{
					if (candidate > _longStopPrice.Value)
						_longStopPrice = candidate;
				}
				else
				{
					_longStopPrice = candidate;
				}
			}

			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Position);
				ClearStops();
				return true;
			}
		}
		else if (Position < 0m)
		{
			var distance = ToPrice(StopLossPoints);
			if (distance > 0m)
			{
				var candidate = RoundPrice(candle.ClosePrice + distance);
				if (_shortStopPrice.HasValue)
				{
					if (candidate < _shortStopPrice.Value)
						_shortStopPrice = candidate;
				}
				else
				{
					_shortStopPrice = candidate;
				}
			}

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ClearStops();
				return true;
			}
		}
		else
		{
			if (_longStopPrice.HasValue || _shortStopPrice.HasValue)
				ClearStops();
		}

		return false;
	}

	private void ClearStops()
	{
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null || order.Security != Security)
			return;

		var price = trade.Trade.Price;

		if (order.Direction == Sides.Buy)
		{
			_longStopPrice = CreateInitialLongStop(price);
			_shortStopPrice = null;
			_overboughtCount = 0;
			_oversoldCount = 0;
		}
		else if (order.Direction == Sides.Sell)
		{
			_shortStopPrice = CreateInitialShortStop(price);
			_longStopPrice = null;
			_overboughtCount = 0;
			_oversoldCount = 0;
		}
	}

	private decimal? CreateInitialLongStop(decimal entryPrice)
	{
		var distance = ToPrice(InitialStopPoints);
		if (distance <= 0m)
			return null;

		return RoundPrice(entryPrice - distance);
	}

	private decimal? CreateInitialShortStop(decimal entryPrice)
	{
		var distance = ToPrice(InitialStopPoints);
		if (distance <= 0m)
			return null;

		return RoundPrice(entryPrice + distance);
	}

	private decimal ToPrice(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var security = Security;
		var step = security?.PriceStep ?? security?.MinStep;
		if (step == null || step.Value <= 0m)
			return points;

		return points * step.Value;
	}

	private decimal RoundPrice(decimal price)
	{
		var security = Security;
		var step = security?.PriceStep ?? security?.MinStep;
		if (step == null || step.Value <= 0m)
			return price;

		return Math.Round(price / step.Value, 0, MidpointRounding.AwayFromZero) * step.Value;
	}
}
