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

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI Expert strategy converted from MetaTrader 5.
/// Trades RSI threshold breakouts with optional stop, take profit, and trailing stop measured in pips.
/// </summary>
public class RsiExpertStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;

	private RelativeStrengthIndex _rsiIndicator = null!;

	private decimal? _previousRsi;
	private decimal? _previousPreviousRsi;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Candle data type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought threshold.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// RSI oversold threshold.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum step before trailing stop moves again.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RsiExpertStrategy"/>.
	/// </summary>
	public RsiExpertStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for RSI calculation", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Lookback period for RSI", "RSI")
			.SetCanOptimize(true);

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 60m)
			.SetDisplay("RSI Upper", "Overbought threshold", "RSI")
			.SetCanOptimize(true);

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 20m)
			.SetDisplay("RSI Lower", "Oversold threshold", "RSI")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 60)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 0)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimum move before trailing stop adjusts", "Risk");
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

		_previousRsi = null;
		_previousPreviousRsi = null;
		ResetTrailing();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsiIndicator = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_rsiIndicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);

			var rsiArea = CreateChartArea();
			DrawIndicator(rsiArea, _rsiIndicator);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (UpdateRiskManagement(candle))
		{
			StoreRsi(rsiValue);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StoreRsi(rsiValue);
			return;
		}

		if (_previousRsi is null || _previousPreviousRsi is null)
		{
			StoreRsi(rsiValue);
			return;
		}

		var crossedUp = _previousRsi > RsiLowerLevel && _previousPreviousRsi < RsiLowerLevel;
		var crossedDown = _previousRsi < RsiUpperLevel && _previousPreviousRsi > RsiUpperLevel;

		if (crossedUp && Position <= 0 && TradeVolume > 0m)
		{
			var targetPosition = TradeVolume;
			var delta = targetPosition - Position;

			if (delta > 0m)
			{
				BuyMarket(delta);
				ResetTrailing();
			}
		}
		else if (crossedDown && Position >= 0 && TradeVolume > 0m)
		{
			var targetPosition = -TradeVolume;
			var delta = targetPosition - Position;

			if (delta < 0m)
			{
				SellMarket(-delta);
				ResetTrailing();
			}
		}

		StoreRsi(rsiValue);
	}

	private bool UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var entry = PositionPrice;
			if (entry <= 0m)
				return false;

			if (StopLossPips > 0)
			{
				var stopDist = GetPriceOffset(StopLossPips);
				if (stopDist > 0m)
				{
					var stopPrice = entry - stopDist;

					if (candle.LowPrice <= stopPrice)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}

			if (TakeProfitPips > 0)
			{
				var takeDist = GetPriceOffset(TakeProfitPips);
				if (takeDist > 0m)
				{
					var takePrice = entry + takeDist;

					if (candle.HighPrice >= takePrice)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}

			if (TrailingStopPips > 0)
			{
				var trailingDistance = GetPriceOffset(TrailingStopPips);
				if (trailingDistance > 0m)
				{
					var trailingStep = GetPriceOffset(TrailingStepPips);
					var profit = candle.ClosePrice - entry;

					if (profit > trailingDistance + trailingStep)
					{
						var desiredStop = candle.ClosePrice - trailingDistance;
						var minimalImprovement = trailingStep > 0m ? trailingStep : (Security?.PriceStep ?? 0m);

						if (_longTrailingStop is null || desiredStop - _longTrailingStop.Value >= minimalImprovement)
							_longTrailingStop = desiredStop;
					}

					if (_longTrailingStop is decimal trail && candle.LowPrice <= trail)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}
		}
		else if (Position < 0)
		{
			var entry = PositionPrice;
			if (entry <= 0m)
				return false;

			if (StopLossPips > 0)
			{
				var stopDist = GetPriceOffset(StopLossPips);
				if (stopDist > 0m)
				{
					var stopPrice = entry + stopDist;

					if (candle.HighPrice >= stopPrice)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}

			if (TakeProfitPips > 0)
			{
				var takeDist = GetPriceOffset(TakeProfitPips);
				if (takeDist > 0m)
				{
					var takePrice = entry - takeDist;

					if (candle.LowPrice <= takePrice)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}

			if (TrailingStopPips > 0)
			{
				var trailingDistance = GetPriceOffset(TrailingStopPips);
				if (trailingDistance > 0m)
				{
					var trailingStep = GetPriceOffset(TrailingStepPips);
					var profit = entry - candle.ClosePrice;

					if (profit > trailingDistance + trailingStep)
					{
						var desiredStop = candle.ClosePrice + trailingDistance;
						var minimalImprovement = trailingStep > 0m ? trailingStep : (Security?.PriceStep ?? 0m);

						if (_shortTrailingStop is null || _shortTrailingStop.Value - desiredStop >= minimalImprovement)
							_shortTrailingStop = desiredStop;
					}

					if (_shortTrailingStop is decimal trail && candle.HighPrice >= trail)
					{
						ClosePosition();
						ResetTrailing();
						return true;
					}
				}
			}
		}
		else
		{
			ResetTrailing();
		}

		return false;
	}

	private decimal GetPriceOffset(int pips)
	{
		if (pips <= 0)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		return pips * step;
	}

	private void StoreRsi(decimal rsiValue)
	{
		if (_previousRsi is null)
		{
			_previousRsi = rsiValue;
			return;
		}

		_previousPreviousRsi = _previousRsi;
		_previousRsi = rsiValue;
	}

	private void ResetTrailing()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}
}