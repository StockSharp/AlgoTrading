using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RMI Trend Sync strategy combines RSI and MFI momentum with a SuperTrend trailing stop.
/// </summary>
public class RmiTrendSyncStrategy : Strategy
{
	private readonly StrategyParam<int> _rmiLength;
	private readonly StrategyParam<decimal> _positiveThreshold;
	private readonly StrategyParam<decimal> _negativeThreshold;
	private readonly StrategyParam<int> _superTrendLength;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsiMfi;
	private decimal _prevEma;
	private bool _hasPrev;
	private bool _isPositive;
	private bool _isNegative;
	private decimal? _stopPrice;

	/// <summary>
	/// Period for RSI and MFI.
	/// </summary>
	public int RmiLength { get => _rmiLength.Value; set => _rmiLength.Value = value; }

	/// <summary>
	/// Momentum upper level.
	/// </summary>
	public decimal PositiveThreshold { get => _positiveThreshold.Value; set => _positiveThreshold.Value = value; }

	/// <summary>
	/// Momentum lower level.
	/// </summary>
	public decimal NegativeThreshold { get => _negativeThreshold.Value; set => _negativeThreshold.Value = value; }

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int SuperTrendLength { get => _superTrendLength.Value; set => _superTrendLength.Value = value; }

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }

	/// <summary>
	/// Allowed direction.
	/// </summary>
	public TradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// Type of candles.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize the RMI Trend Sync strategy.
	/// </summary>
	public RmiTrendSyncStrategy()
	{
		_rmiLength = Param(nameof(RmiLength), 21)
			.SetDisplay("RMI Length", "Period for RSI and MFI", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5)
			.SetGreaterThanZero();

		_positiveThreshold = Param(nameof(PositiveThreshold), 70m)
			.SetDisplay("Positive Threshold", "Momentum upper level", "Parameters");

		_negativeThreshold = Param(nameof(NegativeThreshold), 30m)
			.SetDisplay("Negative Threshold", "Momentum lower level", "Parameters");

		_superTrendLength = Param(nameof(SuperTrendLength), 10)
			.SetDisplay("SuperTrend Length", "ATR period for SuperTrend", "SuperTrend")
			.SetGreaterThanZero();

		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 3.5m)
			.SetDisplay("SuperTrend Mult", "ATR multiplier for SuperTrend", "SuperTrend")
			.SetGreaterThanZero();

		_direction = Param(nameof(Direction), TradeDirection.Both)
			.SetDisplay("Trade Direction", "Allowed direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevRsiMfi = 0m;
		_prevEma = 0m;
		_hasPrev = false;
		_isPositive = false;
		_isNegative = false;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var rsi = new RelativeStrengthIndex { Length = RmiLength };
		var mfi = new MoneyFlowIndex { Length = RmiLength };
		var ema = new ExponentialMovingAverage { Length = 5 };
		var superTrend = new SuperTrend { Length = SuperTrendLength, Multiplier = SuperTrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, mfi, ema, superTrend, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		var momentumArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, superTrend);
			DrawOwnTrades(priceArea);
		}

		if (momentumArea != null)
		{
			DrawIndicator(momentumArea, rsi);
			DrawIndicator(momentumArea, mfi);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue rsiVal,
		IIndicatorValue mfiVal,
		IIndicatorValue emaVal,
		IIndicatorValue stVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiVal.IsFinal || !mfiVal.IsFinal || !emaVal.IsFinal || stVal is not SuperTrendIndicatorValue st)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiVal.GetValue<decimal>();
		var mfi = mfiVal.GetValue<decimal>();
		var ema = emaVal.GetValue<decimal>();
		var rsiMfi = (rsi + mfi) / 2m;
		var emaSlope = ema - _prevEma;

		var positiveMomentum = _hasPrev && _prevRsiMfi < PositiveThreshold && rsiMfi > PositiveThreshold && rsiMfi > NegativeThreshold && emaSlope > 0;
		var negativeMomentum = rsiMfi < NegativeThreshold && emaSlope < 0;

		var longCondition = positiveMomentum && !_isPositive;
		var shortCondition = negativeMomentum && !_isNegative;

		if (longCondition && (Direction == TradeDirection.Long || Direction == TradeDirection.Both) && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = st.Value;
		}
		else if (shortCondition && (Direction == TradeDirection.Short || Direction == TradeDirection.Both) && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = st.Value;
		}

		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_stopPrice = null;
			}
			else
			{
				_stopPrice = st.Value;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = null;
			}
			else
			{
				_stopPrice = st.Value;
			}
		}

		_isPositive = positiveMomentum ? true : negativeMomentum ? false : _isPositive;
		_isNegative = negativeMomentum ? true : positiveMomentum ? false : _isNegative;

		_prevRsiMfi = rsiMfi;
		_prevEma = ema;
		_hasPrev = true;
	}
}
