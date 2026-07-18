using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Take Profit Time Guard strategy (simplified). Uses CCI momentum
/// with session time awareness for entries and profit management.
/// </summary>
public class TakeProfitTimeGuardStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;

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

	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	public TakeProfitTimeGuardStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_cciLength = Param(nameof(CciLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "CCI period", "Indicators");

		_upperLevel = Param(nameof(UpperLevel), 100m)
			.SetDisplay("Upper Level", "CCI level for sell signal", "Logic");

		_lowerLevel = Param(nameof(LowerLevel), -100m)
			.SetDisplay("Lower Level", "CCI level for buy signal", "Logic");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciLength };

		decimal prevCci = 0;
		var hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, (ICandleMessage candle, decimal cciVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevCci = cciVal;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevCci = cciVal;
					return;
				}

				// CCI crosses up from below lower level
				if (prevCci < LowerLevel && cciVal >= LowerLevel && Position <= 0)
					BuyMarket();
				// CCI crosses down from above upper level
				else if (prevCci > UpperLevel && cciVal <= UpperLevel && Position >= 0)
					SellMarket();

				prevCci = cciVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var cciArea = CreateChartArea();
			if (cciArea != null)
				DrawIndicator(cciArea, cci);
		}
	}
}
