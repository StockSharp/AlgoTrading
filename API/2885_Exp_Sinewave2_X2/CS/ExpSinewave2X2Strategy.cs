using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ehlers Sinewave X2 strategy (simplified). Uses CCI oscillator for entries.
/// </summary>
public class ExpSinewave2X2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<int> _emaLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public ExpSinewave2X2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_cciLength = Param(nameof(CciLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "CCI period", "Indicators");

		_emaLength = Param(nameof(EmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend filter EMA", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		decimal prevCci = 0;
		bool hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ema, (ICandleMessage candle, decimal cciValue, decimal emaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevCci = cciValue;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevCci = cciValue;
					return;
				}

				// CCI crosses above -100 with price above EMA
				if (prevCci < -100 && cciValue >= -100 && candle.ClosePrice > emaValue && Position <= 0)
				{
					BuyMarket();
				}
				// CCI crosses below 100 with price below EMA
				else if (prevCci > 100 && cciValue <= 100 && candle.ClosePrice < emaValue && Position >= 0)
				{
					SellMarket();
				}

				prevCci = cciValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
