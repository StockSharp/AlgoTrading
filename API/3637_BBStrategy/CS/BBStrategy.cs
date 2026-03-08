using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands breakout strategy.
/// When price breaks above outer band, waits for re-entry into inner band then goes long.
/// When price breaks below outer band, waits for re-entry into inner band then goes short.
/// </summary>
public class BBStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _innerDeviation;
	private readonly StrategyParam<decimal> _outerDeviation;
	private readonly StrategyParam<DataType> _candleType;

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal InnerDeviation
	{
		get => _innerDeviation.Value;
		set => _innerDeviation.Value = value;
	}

	public decimal OuterDeviation
	{
		get => _outerDeviation.Value;
		set => _outerDeviation.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BBStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_innerDeviation = Param(nameof(InnerDeviation), 2m)
			.SetDisplay("Inner Dev", "Inner band deviations", "Indicators");

		_outerDeviation = Param(nameof(OuterDeviation), 3m)
			.SetDisplay("Outer Dev", "Outer band deviations", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var innerBand = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = InnerDeviation
		};

		var outerBand = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = OuterDeviation
		};

		var waitDirection = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(innerBand, outerBand, (candle, innerVal, outerVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!innerBand.IsFormed || !outerBand.IsFormed)
					return;

				if (innerVal.IsEmpty || outerVal.IsEmpty)
					return;

				var innerBb = innerVal as IBollingerBandsValue;
				var outerBb = outerVal as IBollingerBandsValue;

				if (innerBb == null || outerBb == null)
					return;

				var innerUpper = innerBb.UpBand ?? 0;
				var innerLower = innerBb.LowBand ?? 0;
				var outerUpper = outerBb.UpBand ?? 0;
				var outerLower = outerBb.LowBand ?? 0;

				if (innerUpper == 0 || innerLower == 0 || outerUpper == 0 || outerLower == 0)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var price = candle.ClosePrice;
				var signal = 0;

				// Detect outer band breakout
				if (price > outerUpper)
					waitDirection = 1;
				else if (price < outerLower)
					waitDirection = -1;

				// Check re-entry into inner band
				if (waitDirection > 0 && price < innerUpper && price > innerLower)
				{
					signal = 1;
					waitDirection = 0;
				}
				else if (waitDirection < 0 && price > innerLower && price < innerUpper)
				{
					signal = -1;
					waitDirection = 0;
				}

				if (signal == 1 && Position <= 0)
					BuyMarket();
				else if (signal == -1 && Position >= 0)
					SellMarket();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, innerBand);
			DrawIndicator(area, outerBand);
			DrawOwnTrades(area);
		}
	}
}
