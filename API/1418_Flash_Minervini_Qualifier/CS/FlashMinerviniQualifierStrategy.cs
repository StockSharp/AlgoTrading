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
/// Flash strategy with Minervini stage analysis filter.
/// Uses EMA crossover, RSI momentum and MA alignment.
/// </summary>
public class FlashMinerviniQualifierStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prev50;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiThreshold { get => _rsiThreshold.Value; set => _rsiThreshold.Value = value; }
	public int EmaFastLength { get => _emaFastLength.Value; set => _emaFastLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FlashMinerviniQualifierStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 10)
			.SetDisplay("RSI Length", "Length for RSI", "Parameters")
			.SetGreaterThanZero();
		_rsiThreshold = Param(nameof(RsiThreshold), 60m)
			.SetDisplay("RSI Threshold", "RSI threshold for momentum", "Parameters");
		_emaFastLength = Param(nameof(EmaFastLength), 12)
			.SetDisplay("EMA Fast Length", "Fast EMA period", "Parameters")
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
		_prevFast = 0m;
		_prev50 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
		var ema50 = new ExponentialMovingAverage { Length = 50 };
		var ema150 = new ExponentialMovingAverage { Length = 150 };
		var ema200 = new ExponentialMovingAverage { Length = 200 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, emaFast, ema50, ema150, ema200, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal fastVal, decimal ema50Val, decimal ema150Val, decimal ema200Val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check exits first
		if (Position > 0 && (fastVal < ema50Val || rsiVal < 40))
		{
			SellMarket();
			_prevFast = fastVal;
			_prev50 = ema50Val;
			return;
		}
		else if (Position < 0 && (fastVal > ema50Val || rsiVal < 40))
		{
			BuyMarket();
			_prevFast = fastVal;
			_prev50 = ema50Val;
			return;
		}

		// Minervini stage conditions
		bool longStage = candle.ClosePrice > ema150Val && ema50Val > ema150Val && ema150Val > ema200Val;
		bool shortStage = candle.ClosePrice < ema150Val && ema50Val < ema150Val && ema150Val < ema200Val;

		// Entry signals: EMA cross + RSI momentum + stage alignment
		if (Position == 0 && _prevFast != 0 && _prev50 != 0)
		{
			bool fastCrossUp = _prevFast <= _prev50 && fastVal > ema50Val;
			bool fastCrossDown = _prevFast >= _prev50 && fastVal < ema50Val;

			if (fastCrossUp && longStage && rsiVal > RsiThreshold)
				BuyMarket();
			else if (fastCrossDown && shortStage && rsiVal > RsiThreshold)
				SellMarket();
		}

		_prevFast = fastVal;
		_prev50 = ema50Val;
	}
}
