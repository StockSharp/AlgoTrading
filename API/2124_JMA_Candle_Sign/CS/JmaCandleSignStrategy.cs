using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Jurik moving averages of open and close prices.
/// Goes long when JMA(close) crosses above JMA(open).
/// Goes short when JMA(close) crosses below JMA(open).
/// </summary>
public class JmaCandleSignStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private JurikMovingAverage _jmaOpen;
	private JurikMovingAverage _jmaClose;
	private decimal _prevOpenJma;
	private decimal _prevCloseJma;
	private bool _hasPrev;

	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public JmaCandleSignStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 7)
			.SetDisplay("JMA Length", "Period for Jurik moving averages", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_jmaOpen = new JurikMovingAverage { Length = JmaLength };
		_jmaClose = new JurikMovingAverage { Length = JmaLength };
		_prevOpenJma = 0;
		_prevCloseJma = 0;
		_hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openResult = _jmaOpen.Process(new DecimalIndicatorValue(_jmaOpen, candle.OpenPrice, candle.OpenTime) { IsFinal = true });
		var closeResult = _jmaClose.Process(new DecimalIndicatorValue(_jmaClose, candle.ClosePrice, candle.OpenTime) { IsFinal = true });

		if (!openResult.IsFormed || !closeResult.IsFormed)
			return;

		var openJma = openResult.ToDecimal();
		var closeJma = closeResult.ToDecimal();

		if (!_hasPrev)
		{
			_prevOpenJma = openJma;
			_prevCloseJma = closeJma;
			_hasPrev = true;
			return;
		}

		// JMA(close) crosses above JMA(open) - bullish
		if (_prevCloseJma <= _prevOpenJma && closeJma > openJma && Position <= 0)
		{
			BuyMarket();
		}
		// JMA(close) crosses below JMA(open) - bearish
		else if (_prevCloseJma >= _prevOpenJma && closeJma < openJma && Position >= 0)
		{
			SellMarket();
		}

		_prevOpenJma = openJma;
		_prevCloseJma = closeJma;
	}
}
