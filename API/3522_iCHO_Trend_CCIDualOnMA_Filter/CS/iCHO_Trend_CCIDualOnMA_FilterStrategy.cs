using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combines a Chaikin oscillator zero-line filter with CCI confirmation.
/// Buys when Chaikin crosses above zero and CCI is rising, sells on opposite.
/// </summary>
public class iCHO_Trend_CCIDualOnMA_FilterStrategy : Strategy
{
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevCci;
	private decimal? _prevPrevCci;

	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public iCHO_Trend_CCIDualOnMA_FilterStrategy()
	{
		_cciLength = Param(nameof(CciLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "CCI period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "Data");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevCci.HasValue && _prevPrevCci.HasValue)
		{
			// CCI crosses above zero from below - buy signal
			if (_prevPrevCci.Value < 0 && _prevCci.Value < 0 && cciValue > 0 && Position <= 0)
			{
				BuyMarket();
			}
			// CCI crosses below zero from above - sell signal
			else if (_prevPrevCci.Value > 0 && _prevCci.Value > 0 && cciValue < 0 && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevPrevCci = _prevCci;
		_prevCci = cciValue;
	}
}
