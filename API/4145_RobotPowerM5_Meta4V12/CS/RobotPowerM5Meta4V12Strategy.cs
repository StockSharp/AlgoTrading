using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "RobotPowerM5_meta4V12".
/// Uses Bulls Power and Bears Power readings to decide whether to buy or sell after every completed candle.
/// Recreates the fixed stop-loss, take-profit, and trailing stop management from the original script.
/// </summary>
public class RobotPowerM5Meta4V12Strategy : Strategy
{
	private readonly StrategyParam<int> _bullBearPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private BearsPower _bearsPower;
	private BullsPower _bullsPower;

	private decimal? _previousSum;
	private decimal _pointSize;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private bool _longExitRequested;
	private bool _shortExitRequested;
	private decimal _highestPriceSinceEntry;
	private decimal _lowestPriceSinceEntry;

	/// <summary>
	/// Initializes a new instance of the <see cref="RobotPowerM5Meta4V12Strategy"/> class.
	/// </summary>
	public RobotPowerM5Meta4V12Strategy()
	{
		_bullBearPeriod = Param(nameof(BullBearPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Bulls/Bears Period", "Number of bars used by the Bulls Power and Bears Power indicators.", "Indicators")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Lot size requested for every entry.", "Trading")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 45m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Initial protective stop distance expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Profit target distance expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 15m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Trailing distance maintained once the trade moves into profit.", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for signal calculations.", "Data");

		_pointSize = 0m;
	}

	/// <summary>
	/// Bulls/Bears Power calculation period.
	/// </summary>
	public int BullBearPeriod
	{
		get => _bullBearPeriod.Value;
		set => _bullBearPeriod.Value = value;
	}

	/// <summary>
	/// Lot size used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance in MetaTrader points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bearsPower = null;
		_bullsPower = null;
		_previousSum = null;
		_pointSize = 0m;
		ResetRiskLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();

		_bearsPower = new BearsPower { Length = BullBearPeriod };
		_bullsPower = new BullsPower { Length = BullBearPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bullsPower, _bearsPower, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bullsPower);
			DrawIndicator(area, _bearsPower);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m && delta > 0m)
		{
			// A new long position has been established.
			_highestPriceSinceEntry = PositionPrice;
			_longStopPrice = StopLossPoints > 0m ? PositionPrice - StopLossPoints * _pointSize : null;
			_longTakePrice = TakeProfitPoints > 0m ? PositionPrice + TakeProfitPoints * _pointSize : null;
			_longExitRequested = false;
			_shortExitRequested = false;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else if (Position < 0m && delta < 0m)
		{
			// A new short position has been established.
			_lowestPriceSinceEntry = PositionPrice;
			_shortStopPrice = StopLossPoints > 0m ? PositionPrice + StopLossPoints * _pointSize : null;
			_shortTakePrice = TakeProfitPoints > 0m ? PositionPrice - TakeProfitPoints * _pointSize : null;
			_longExitRequested = false;
			_shortExitRequested = false;
			_longStopPrice = null;
			_longTakePrice = null;
		}
		else if (Position == 0m)
		{
			// All trades have been closed; reset cached risk levels.
			ResetRiskLevels();
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bullsPower, decimal bearsPower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage trailing logic and exit conditions before evaluating new entries.
		ManageActivePosition(candle);

		var combinedPower = bullsPower + bearsPower;

		if (_previousSum is not decimal previousSum)
		{
			_previousSum = combinedPower;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousSum = combinedPower;
			return;
		}

		var volume = Volume;
		if (volume <= 0m)
		{
			_previousSum = combinedPower;
			return;
		}

		if (Position == 0m)
		{
			if (previousSum > 0m)
			{
				// Bulls dominate: open a long position.
				BuyMarket(volume);
			}
			else if (previousSum < 0m)
			{
				// Bears dominate: open a short position.
				SellMarket(volume);
			}
		}

		_previousSum = combinedPower;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			_highestPriceSinceEntry = _highestPriceSinceEntry == 0m
				? candle.HighPrice
				: Math.Max(_highestPriceSinceEntry, candle.HighPrice);

			if (_longTakePrice is decimal takePrice && candle.HighPrice >= takePrice)
			{
				// Take-profit level reached by the candle high.
				RequestCloseLong();
				return;
			}

			if (TrailingStopPoints > 0m && _pointSize > 0m)
			{
				var trailDistance = TrailingStopPoints * _pointSize;
				var referencePrice = Math.Max(candle.ClosePrice, candle.HighPrice);

				// Activate the trailing stop only after the price moves in favour of the trade.
				if (referencePrice - PositionPrice > trailDistance)
				{
					var desiredStop = referencePrice - trailDistance;

					if (_longStopPrice is not decimal currentStop || desiredStop > currentStop)
					{
						_longStopPrice = desiredStop;
					}
				}
			}

			if (_longStopPrice is decimal stopPrice && candle.LowPrice <= stopPrice)
			{
				// Trailing stop or initial stop triggered by the candle low.
				RequestCloseLong();
			}
		}
		else if (Position < 0m)
		{
			_lowestPriceSinceEntry = _lowestPriceSinceEntry == 0m
				? candle.LowPrice
				: Math.Min(_lowestPriceSinceEntry, candle.LowPrice);

			if (_shortTakePrice is decimal takePrice && candle.LowPrice <= takePrice)
			{
				// Take-profit level reached by the candle low.
				RequestCloseShort();
				return;
			}

			if (TrailingStopPoints > 0m && _pointSize > 0m)
			{
				var trailDistance = TrailingStopPoints * _pointSize;
				var referencePrice = Math.Min(candle.ClosePrice, candle.LowPrice);

				if (PositionPrice - referencePrice > trailDistance)
				{
					var desiredStop = referencePrice + trailDistance;

					if (_shortStopPrice is not decimal currentStop || desiredStop < currentStop)
					{
						_shortStopPrice = desiredStop;
					}
				}
			}

			if (_shortStopPrice is decimal stopPrice && candle.HighPrice >= stopPrice)
			{
				// Trailing stop or initial stop triggered by the candle high.
				RequestCloseShort();
			}
		}
		else
		{
			// No position, keep all risk levels cleared.
			ResetRiskLevels();
		}
	}

	private void RequestCloseLong()
	{
		if (_longExitRequested || Position <= 0m)
			return;

		_longExitRequested = true;
		SellMarket(Position);
	}

	private void RequestCloseShort()
	{
		if (_shortExitRequested || Position >= 0m)
			return;

		_shortExitRequested = true;
		BuyMarket(Math.Abs(Position));
	}

	private void ResetRiskLevels()
	{
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;
		_highestPriceSinceEntry = 0m;
		_lowestPriceSinceEntry = 0m;
	}

	private decimal CalculatePointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}
}
