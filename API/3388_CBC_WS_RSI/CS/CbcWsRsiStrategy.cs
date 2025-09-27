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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy inspired by the "3 Black Crows / 3 White Soldiers" candlestick pattern confirmed by RSI.
/// The strategy detects strong reversal formations and requires RSI confirmation before entering a trade.
/// Additionally, RSI threshold crossovers are used to close existing positions.
/// </summary>
public class CbcWsRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _longConfirmationLevel;
	private readonly StrategyParam<decimal> _shortConfirmationLevel;
	private readonly StrategyParam<decimal> _lowerExitLevel;
	private readonly StrategyParam<decimal> _upperExitLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private ICandleMessage _firstCandle;
	private ICandleMessage _secondCandle;
	private ICandleMessage _currentCandle;

	private decimal? _previousRsi;

	/// <summary>
	/// Candle type and timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the RSI indicator used for confirmation.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI value that confirms long trades.
	/// </summary>
	public decimal LongConfirmationLevel
	{
		get => _longConfirmationLevel.Value;
		set => _longConfirmationLevel.Value = value;
	}

	/// <summary>
	/// RSI value that confirms short trades.
	/// </summary>
	public decimal ShortConfirmationLevel
	{
		get => _shortConfirmationLevel.Value;
		set => _shortConfirmationLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI level used for exit logic.
	/// </summary>
	public decimal LowerExitLevel
	{
		get => _lowerExitLevel.Value;
		set => _lowerExitLevel.Value = value;
	}

	/// <summary>
	/// Upper RSI level used for exit logic.
	/// </summary>
	public decimal UpperExitLevel
	{
		get => _upperExitLevel.Value;
		set => _upperExitLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage for protection logic.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percentage for protection logic.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CbcWsRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for the processed candle series", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 37)
			.SetRange(5, 100)
			.SetDisplay("RSI Period", "Number of candles for RSI calculation", "Indicators")
			.SetCanOptimize(true);

		_longConfirmationLevel = Param(nameof(LongConfirmationLevel), 40m)
			.SetRange(10m, 60m)
			.SetDisplay("Long Confirmation", "Maximum RSI value to approve long entries", "Signals")
			.SetCanOptimize(true);

		_shortConfirmationLevel = Param(nameof(ShortConfirmationLevel), 60m)
			.SetRange(40m, 90m)
			.SetDisplay("Short Confirmation", "Minimum RSI value to approve short entries", "Signals")
			.SetCanOptimize(true);

		_lowerExitLevel = Param(nameof(LowerExitLevel), 30m)
			.SetRange(10m, 50m)
			.SetDisplay("Lower Exit", "RSI level used for closing short trades", "Signals")
			.SetCanOptimize(true);

		_upperExitLevel = Param(nameof(UpperExitLevel), 70m)
			.SetRange(50m, 90m)
			.SetDisplay("Upper Exit", "RSI level used for closing long trades", "Signals")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetRange(0m, 10m)
			.SetDisplay("Stop Loss %", "Stop-loss level in percent", "Risk")
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetRange(0m, 20m)
			.SetDisplay("Take Profit %", "Take-profit level in percent", "Risk")
			.SetCanOptimize(true);
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

		_firstCandle = null;
		_secondCandle = null;
		_currentCandle = null;
		_previousRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var stopLoss = StopLossPercent > 0m ? new Unit(StopLossPercent, UnitTypes.Percent) : null;
		var takeProfit = TakeProfitPercent > 0m ? new Unit(TakeProfitPercent, UnitTypes.Percent) : null;

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(takeProfit, stopLoss, false);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_firstCandle = _secondCandle;
		_secondCandle = _currentCandle;
		_currentCandle = candle;

		var previousRsi = _previousRsi;
		_previousRsi = rsiValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_firstCandle == null || _secondCandle == null || _currentCandle == null)
			return;

		if (previousRsi.HasValue && HandleExits(rsiValue, previousRsi.Value))
			return;

		HandleEntries(rsiValue);
	}

	private bool HandleExits(decimal currentRsi, decimal previousRsi)
	{
		var closedPosition = false;

		if (Position > 0)
		{
			if (CrossDown(previousRsi, currentRsi, UpperExitLevel) || CrossDown(previousRsi, currentRsi, LowerExitLevel))
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					SellMarket(volume);
					closedPosition = true;
				}
			}
		}
		else if (Position < 0)
		{
			if (CrossUp(previousRsi, currentRsi, LowerExitLevel) || CrossUp(previousRsi, currentRsi, UpperExitLevel))
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					BuyMarket(volume);
					closedPosition = true;
				}
			}
		}

		return closedPosition;
	}

	private void HandleEntries(decimal currentRsi)
	{
		if (IsThreeWhiteSoldiers() && currentRsi <= LongConfirmationLevel)
		{
			if (Position < 0)
			{
				var coveringVolume = Math.Abs(Position);
				if (coveringVolume > 0)
					BuyMarket(coveringVolume);
			}

			if (Position == 0)
			{
				BuyMarket(Volume);
			}
		}
		else if (IsThreeBlackCrows() && currentRsi >= ShortConfirmationLevel)
		{
			if (Position > 0)
			{
				var closingVolume = Math.Abs(Position);
				if (closingVolume > 0)
					SellMarket(closingVolume);
			}

			if (Position == 0)
			{
				SellMarket(Volume);
			}
		}
	}

	private bool IsThreeWhiteSoldiers()
	{
		if (_firstCandle == null || _secondCandle == null || _currentCandle == null)
			return false;

		if (!IsBullish(_firstCandle) || !IsBullish(_secondCandle) || !IsBullish(_currentCandle))
			return false;

		if (_secondCandle.ClosePrice <= _firstCandle.ClosePrice || _currentCandle.ClosePrice <= _secondCandle.ClosePrice)
			return false;

		var secondOpenInside = _secondCandle.OpenPrice >= _firstCandle.OpenPrice && _secondCandle.OpenPrice <= _firstCandle.ClosePrice;
		var thirdOpenInside = _currentCandle.OpenPrice >= _secondCandle.OpenPrice && _currentCandle.OpenPrice <= _secondCandle.ClosePrice;

		if (!secondOpenInside || !thirdOpenInside)
			return false;

		return BodySize(_firstCandle) > 0 && BodySize(_secondCandle) > 0 && BodySize(_currentCandle) > 0;
	}

	private bool IsThreeBlackCrows()
	{
		if (_firstCandle == null || _secondCandle == null || _currentCandle == null)
			return false;

		if (!IsBearish(_firstCandle) || !IsBearish(_secondCandle) || !IsBearish(_currentCandle))
			return false;

		if (_secondCandle.ClosePrice >= _firstCandle.ClosePrice || _currentCandle.ClosePrice >= _secondCandle.ClosePrice)
			return false;

		var secondOpenInside = _secondCandle.OpenPrice <= _firstCandle.OpenPrice && _secondCandle.OpenPrice >= _firstCandle.ClosePrice;
		var thirdOpenInside = _currentCandle.OpenPrice <= _secondCandle.OpenPrice && _currentCandle.OpenPrice >= _secondCandle.ClosePrice;

		if (!secondOpenInside || !thirdOpenInside)
			return false;

		return BodySize(_firstCandle) > 0 && BodySize(_secondCandle) > 0 && BodySize(_currentCandle) > 0;
	}

	private static bool IsBullish(ICandleMessage candle)
	{
		return candle.ClosePrice > candle.OpenPrice;
	}

	private static bool IsBearish(ICandleMessage candle)
	{
		return candle.ClosePrice < candle.OpenPrice;
	}

	private static decimal BodySize(ICandleMessage candle)
	{
		return Math.Abs(candle.ClosePrice - candle.OpenPrice);
	}

	private static bool CrossUp(decimal previous, decimal current, decimal level)
	{
		return previous < level && current >= level;
	}

	private static bool CrossDown(decimal previous, decimal current, decimal level)
	{
		return previous > level && current <= level;
	}
}

