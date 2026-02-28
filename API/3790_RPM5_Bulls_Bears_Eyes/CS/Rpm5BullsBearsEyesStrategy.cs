using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RPM5 Bulls Bears Eyes strategy using Bull/Bear power with EMA filter.
/// Buy when bull power is positive and bear power crosses above threshold.
/// Sell when bear power is negative and bull power crosses below threshold.
/// </summary>
public class Rpm5BullsBearsEyesStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _powerPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevBull;
	private decimal _prevBear;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int PowerPeriod { get => _powerPeriod.Value; set => _powerPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Rpm5BullsBearsEyesStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 13)
			.SetDisplay("EMA Period", "EMA trend period", "Indicators");

		_powerPeriod = Param(nameof(PowerPeriod), 13)
			.SetDisplay("Power Period", "Bulls/Bears power period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var bulls = new BullPower { Length = PowerPeriod };
		var bears = new BearPower { Length = PowerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, bulls, bears, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal bullValue, decimal bearValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevBull = bullValue;
			_prevBear = bearValue;
			_hasPrev = true;
			return;
		}

		// Long: price above EMA, bull power positive, bear power crossing from negative to less negative
		var longSignal = candle.ClosePrice > emaValue && bullValue > 0 && _prevBear < 0 && bearValue > _prevBear;
		// Short: price below EMA, bear power negative, bull power crossing from positive to less positive
		var shortSignal = candle.ClosePrice < emaValue && bearValue < 0 && _prevBull > 0 && bullValue < _prevBull;

		if (Position <= 0 && longSignal)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (Position >= 0 && shortSignal)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevBull = bullValue;
		_prevBear = bearValue;
	}
}
