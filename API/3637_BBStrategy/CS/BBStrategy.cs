namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Converted Bollinger Bands breakout strategy from the MetaTrader expert "BBStrategy".
/// </summary>
public class BBStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _innerDeviation;
	private readonly StrategyParam<decimal> _outerDeviation;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private int _waitDirection;
	private decimal _pointValue;

	/// <summary>
	/// Initializes a new instance of <see cref="BBStrategy"/>.
	/// </summary>
	public BBStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Desired volume for each position", "Trading");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators");

		_innerDeviation = Param(nameof(InnerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Inner Deviation", "Standard deviations for the inner band", "Indicators");

		_outerDeviation = Param(nameof(OuterDeviation), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Outer Deviation", "Standard deviations for the outer band", "Indicators");

		_stopLossPoints = Param(nameof(StopLossPoints), 220m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss Points", "Protective stop distance measured in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 220m)
			.SetNotNegative()
			.SetDisplay("Take-Profit Points", "Take-profit distance measured in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for the Bollinger calculations", "General");
	}

	/// <summary>
	/// Desired order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for the inner Bollinger band.
	/// </summary>
	public decimal InnerDeviation
	{
		get => _innerDeviation.Value;
		set => _innerDeviation.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for the outer Bollinger band.
	/// </summary>
	public decimal OuterDeviation
	{
		get => _outerDeviation.Value;
		set => _outerDeviation.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_waitDirection = 0;
		_pointValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_pointValue = CalculatePointValue();

		var takeProfit = ToAbsoluteUnit(TakeProfitPoints);
		var stopLoss = ToAbsoluteUnit(StopLossPoints);

		StartProtection(
			takeProfit: takeProfit,
			stopLoss: stopLoss,
			useMarketOrders: true);

		var innerBand = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = InnerDeviation
		};

		var outerBand = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = OuterDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(innerBand, outerBand, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, innerBand);
			DrawIndicator(area, outerBand);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal innerMiddle, decimal innerUpper, decimal innerLower,
		decimal outerMiddle, decimal outerUpper, decimal outerLower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		var signal = 0;

		if (price > outerUpper)
		{
			signal = 1;
		}
		else if (price < outerLower)
		{
			signal = -1;
		}

		if (signal == 1 || _waitDirection > 0)
		{
			// Wait for the price to re-enter the inner band before going long.
			if (price < innerUpper && price > innerLower)
			{
				signal = 1;
				_waitDirection = 0;
			}
			else
			{
				signal = 0;
				_waitDirection = 1;
			}
		}

		if (signal == -1 || _waitDirection < 0)
		{
			// Wait for the price to re-enter the inner band before going short.
			if (price > innerLower && price < innerUpper)
			{
				signal = -1;
				_waitDirection = 0;
			}
			else
			{
				signal = 0;
				_waitDirection = -1;
			}
		}

		if (Position != 0m)
		{
			_waitDirection = 0;
		}

		if (signal == 1 && Position <= 0m && !HasActiveOrders())
		{
			var volume = Volume;
			if (Position < 0m)
			{
				volume += Math.Abs(Position);
			}

			if (volume > 0m)
			{
				BuyMarket(volume);
			}
		}
		else if (signal == -1 && Position >= 0m && !HasActiveOrders())
		{
			var volume = Volume;
			if (Position > 0m)
			{
				volume += Math.Abs(Position);
			}

			if (volume > 0m)
			{
				SellMarket(volume);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_waitDirection = 0;
		}
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var decimals = security.Decimals;
		var adjust = decimals is 3 or 5 ? 10m : 1m;
		return step * adjust;
	}

	private Unit ToAbsoluteUnit(decimal points)
	{
		if (points <= 0m || _pointValue <= 0m)
			return null;

		return new Unit(points * _pointValue, UnitTypes.Absolute);
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}
}

