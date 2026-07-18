using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades MACD crossovers.
/// </summary>
public class MacdCrossAudusdD1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevIsMacdAboveSignal;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MacdCrossAudusdD1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevIsMacdAboveSignal = false;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdVal = macdTyped.Macd;
		var signalVal = macdTyped.Signal;

		if (macdVal is not decimal m || signalVal is not decimal s)
			return;

		var isMacdAboveSignal = m > s;

		if (!_hasPrev)
		{
			_prevIsMacdAboveSignal = isMacdAboveSignal;
			_hasPrev = true;
			return;
		}

		var crossedUp = isMacdAboveSignal && !_prevIsMacdAboveSignal;
		var crossedDown = !isMacdAboveSignal && _prevIsMacdAboveSignal;

		if (crossedUp && Position <= 0)
		{
			BuyMarket();
		}
		else if (crossedDown && Position >= 0)
		{
			SellMarket();
		}

		_prevIsMacdAboveSignal = isMacdAboveSignal;
	}
}
