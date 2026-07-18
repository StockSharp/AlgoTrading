using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades during predefined seasonal periods.
/// Enters periodically and holds for a configured number of bars.
/// </summary>
public class AdvancedMultiSeasonalityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _holdingBars;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema1;
	private ExponentialMovingAverage _ema2;
	private int _barIndex;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int HoldingBars { get => _holdingBars.Value; set => _holdingBars.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdvancedMultiSeasonalityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_holdingBars = Param(nameof(HoldingBars), 100)
			.SetGreaterThanZero()
			.SetDisplay("Holding Bars", "Bars to hold position", "General");
		_cooldownBars = Param(nameof(CooldownBars), 50)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");
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
		_ema1 = null;
		_ema2 = null;
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema1 = new ExponentialMovingAverage { Length = 10 };
		_ema2 = new ExponentialMovingAverage { Length = 30 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema1, _ema2, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (_barIndex > HoldingBars && Position > 0)
		{
			SellMarket();
			return;
		}

		if (Position == 0 && _barIndex > CooldownBars)
		{
			BuyMarket();
			_barIndex = 0;
		}
	}
}
