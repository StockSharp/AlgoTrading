namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy converted from the KWAN_CCC expert advisor.
/// Uses CCI and Momentum to detect trend transitions.
/// Enters long when CCI turns up while momentum is positive,
/// enters short when CCI turns down while momentum is negative.
/// </summary>
public class KwanCccStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;

	private decimal _prevCci;
	private decimal _prevClose;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public KwanCccStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI length", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCci = 0m;
		_prevClose = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCci = 0m;
		_prevClose = 0m;
		_initialized = false;

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(cci, (ICandleMessage candle, decimal cciValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!_initialized)
				{
					_prevCci = cciValue;
					_prevClose = candle.ClosePrice;
					_initialized = true;
					return;
				}

				var closeUp = candle.ClosePrice > _prevClose;
				var closeDown = candle.ClosePrice < _prevClose;

				// Buy when CCI crosses into positive territory and price confirms the move.
				if (_prevCci <= 0m && cciValue > 0m && closeUp && Position <= 0)
				{
					BuyMarket();
				}
				// Sell when CCI crosses into negative territory and price confirms the move.
				else if (_prevCci >= 0m && cciValue < 0m && closeDown && Position >= 0)
				{
					SellMarket();
				}

				_prevCci = cciValue;
				_prevClose = candle.ClosePrice;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}
}
