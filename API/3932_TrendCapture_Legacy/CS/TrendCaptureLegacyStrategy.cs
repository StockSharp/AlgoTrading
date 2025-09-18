
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Legacy Trend Capture strategy ported from MetaTrader MQL.
/// </summary>
public class TrendCaptureLegacyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _breakEvenGuard;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<DataType> _candleType;

	private ParabolicSar _sar = null!;
	private AverageDirectionalIndex _adx = null!;

	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool _breakEvenActivated;
	private Sides? _currentSide;

	private decimal _signedPosition;
	private Sides _preferredSide;
	private Sides? _lastEntrySide;
	private decimal _lastEntryPrice;

	/// <summary>
	/// SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum SAR acceleration.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// ADX averaging period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Maximum ADX level that still allows new entries.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Take profit distance in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit in points required before moving the stop to break even.
	/// </summary>
	public decimal BreakEvenGuard
	{
		get => _breakEvenGuard.Value;
		set => _breakEvenGuard.Value = value;
	}

	/// <summary>
	/// Fraction of free margin risked per trade (0.03 equals 3%).
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrendCaptureLegacyStrategy"/>.
	/// </summary>
	public TrendCaptureLegacyStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.02m)
		.SetDisplay("SAR Step", "Acceleration factor", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.2m)
		.SetDisplay("SAR Max", "Maximum acceleration", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetDisplay("ADX Period", "Average directional index period", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
		.SetDisplay("ADX Threshold", "Maximum ADX level to trade", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 180m)
		.SetDisplay("Take Profit", "Distance to take profit in points", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
		.SetDisplay("Stop Loss", "Distance to stop loss in points", "Trading");

		_breakEvenGuard = Param(nameof(BreakEvenGuard), 5m)
		.SetDisplay("Break Even Guard", "Profit in points before moving stop", "Trading");

		_maximumRisk = Param(nameof(MaximumRisk), 0.03m)
		.SetDisplay("Maximum Risk", "Fraction of margin used for position sizing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for indicator calculations", "General");
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

		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_breakEvenActivated = false;
		_currentSide = null;

		_signedPosition = 0m;
		_preferredSide = Sides.Buy;
		_lastEntrySide = null;
		_lastEntryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_sar, _adx, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sar);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!sarValue.IsFinal || !adxValue.IsFinal)
		return;

		var sar = sarValue.ToDecimal();

		if (adxValue is not AverageDirectionalIndexValue adxData)
		return;

		if (adxData.MovingAverage is not decimal adx)
		return;

		var point = Security?.PriceStep ?? 1m;

		if (ManageOpenPosition(candle, point))
		return;

		if (Position != 0)
		return;

		_currentSide = null;
		_breakEvenActivated = false;

		if (adx >= AdxThreshold)
		return;

		if (_preferredSide == Sides.Buy)
		{
			if (candle.ClosePrice <= sar)
			return;

			OpenPosition(Sides.Buy, candle.ClosePrice, point);
		}
		else
		{
			if (candle.ClosePrice >= sar)
			return;

			OpenPosition(Sides.Sell, candle.ClosePrice, point);
		}
	}

	private bool ManageOpenPosition(ICandleMessage candle, decimal point)
	{
		if (Position > 0 && _currentSide == Sides.Buy)
		{
			if (!_breakEvenActivated && BreakEvenGuard > 0m && candle.ClosePrice - _entryPrice >= BreakEvenGuard * point)
			{
				_stopPrice = _entryPrice;
				_breakEvenActivated = true;
			}

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0 && _currentSide == Sides.Sell)
		{
			if (!_breakEvenActivated && BreakEvenGuard > 0m && _entryPrice - candle.ClosePrice >= BreakEvenGuard * point)
			{
				_stopPrice = _entryPrice;
				_breakEvenActivated = true;
			}

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}

	private void OpenPosition(Sides side, decimal price, decimal point)
	{
		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		_entryPrice = price;
		_currentSide = side;
		_breakEvenActivated = false;

		if (side == Sides.Buy)
		{
			BuyMarket(volume);
			_stopPrice = StopLossPoints > 0m ? price - StopLossPoints * point : null;
			_takePrice = TakeProfitPoints > 0m ? price + TakeProfitPoints * point : null;
		}
		else
		{
			SellMarket(volume);
			_stopPrice = StopLossPoints > 0m ? price + StopLossPoints * point : null;
			_takePrice = TakeProfitPoints > 0m ? price - TakeProfitPoints * point : null;
		}
	}

	private decimal CalculateVolume()
	{
		var board = Security?.Board;
		var lotStep = board?.LotStep ?? 1m;
		if (lotStep <= 0m)
		lotStep = 1m;

		var minVolume = board?.MinVolume ?? lotStep;
		var maxVolume = board?.MaxVolume ?? decimal.MaxValue;

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		decimal volume;

		if (portfolioValue > 0m)
		{
			volume = portfolioValue * MaximumRisk / 1000m;
		}
		else
		{
			volume = Volume;
		}

		if (volume <= 0m)
		volume = 1m;

		volume = Math.Round(volume / lotStep) * lotStep;

		if (volume < minVolume)
		volume = minVolume;
		if (volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null)
		return;

		var delta = trade.Volume * (order.Side == Sides.Buy ? 1m : -1m);
		var previous = _signedPosition;
		_signedPosition += delta;

		if (previous == 0m && _signedPosition != 0m)
		{
			_lastEntrySide = order.Side;
			_lastEntryPrice = trade.Trade.Price;
		}
		else if (previous != 0m && _signedPosition == 0m && _lastEntrySide is Sides entrySide)
		{
			var exitPrice = trade.Trade.Price;
			var profit = entrySide == Sides.Buy
			? exitPrice - _lastEntryPrice
			: _lastEntryPrice - exitPrice;

			if (profit > 0m)
			{
				_preferredSide = entrySide;
			}
			else if (profit < 0m)
			{
				_preferredSide = entrySide == Sides.Buy ? Sides.Sell : Sides.Buy;
			}

			_lastEntrySide = null;
			_lastEntryPrice = 0m;
		}
	}
}
