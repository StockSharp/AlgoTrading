using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Trader strategy combining price SMA crossover with RSI trend confirmation.
/// Buy when short SMA crosses above long SMA with RSI above 50.
/// Sell when short SMA crosses below long SMA with RSI below 50.
/// </summary>
public class RsiTraderAlignedAveragesStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShort;
	private decimal _prevLong;
	private bool _hasPrev;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int ShortMaPeriod { get => _shortMaPeriod.Value; set => _shortMaPeriod.Value = value; }
	public int LongMaPeriod { get => _longMaPeriod.Value; set => _longMaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiTraderAlignedAveragesStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators");

		_shortMaPeriod = Param(nameof(ShortMaPeriod), 9)
			.SetDisplay("Short MA", "Short moving average period", "Indicators");

		_longMaPeriod = Param(nameof(LongMaPeriod), 26)
			.SetDisplay("Long MA", "Long moving average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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

		_prevShort = 0m;
		_prevLong = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var shortMa = new SimpleMovingAverage { Length = ShortMaPeriod };
		var longMa = new SimpleMovingAverage { Length = LongMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, shortMa, longMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal shortMa, decimal longMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevShort = shortMa;
			_prevLong = longMa;
			_hasPrev = true;
			return;
		}

		var bullCross = _prevShort <= _prevLong && shortMa > longMa;
		var bearCross = _prevShort >= _prevLong && shortMa < longMa;

		if (Position <= 0 && bullCross && rsiValue > 50)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (Position >= 0 && bearCross && rsiValue < 50)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevShort = shortMa;
		_prevLong = longMa;
	}
}
