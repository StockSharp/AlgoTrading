using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JSatl Digit System based on Jurik Moving Average slope.
/// Opens long when JMA rises and price is above the average.
/// Opens short when JMA falls and price is below the average.
/// </summary>
public class JsAtlDigitSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isFirstValue = true;
	private decimal _prevJma;

	/// <summary>
	/// Period length for Jurik Moving Average.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="JsAtlDigitSystemStrategy"/>.
	/// </summary>
	public JsAtlDigitSystemStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 14);
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)));
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var jma = new JurikMovingAverage { Length = JmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirstValue)
		{
			_prevJma = jmaValue;
			_isFirstValue = false;
			return;
		}

		var price = candle.ClosePrice;
		var slope = jmaValue - _prevJma;

		if (slope > 0m && price > jmaValue)
		{
			// JMA rising and price above average -> open long or close short
			if (Position <= 0m)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (slope < 0m && price < jmaValue)
		{
			// JMA falling and price below average -> open short or close long
			if (Position >= 0m)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevJma = jmaValue;
	}
}
