using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on slope reversals of the Triple Exponential Moving Average.
/// </summary>
public class TemaCustomSlopeStrategy : Strategy
{
	private readonly StrategyParam<int> _temaLength;
	private readonly StrategyParam<DataType> _candleType;

	private TripleExponentialMovingAverage _tema = null!;
	private decimal? _prev1;
	private decimal? _prev2;

	/// <summary>
	/// TEMA calculation length.
	/// </summary>
	public int TemaLength
	{
		get => _temaLength.Value;
		set => _temaLength.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TemaCustomSlopeStrategy"/>.
	/// </summary>
	public TemaCustomSlopeStrategy()
	{
		_temaLength = Param(nameof(TemaLength), 12)
			.SetDisplay("TEMA Length", "Length of the TEMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_tema = new TripleExponentialMovingAverage { Length = TemaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_tema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal tema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev1 is null || _prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = tema;
			return;
		}

		var falling = _prev1 < _prev2;
		var rising = _prev1 > _prev2;
		var turnedUp = falling && tema > _prev1;
		var turnedDown = rising && tema < _prev1;

		_prev2 = _prev1;
		_prev1 = tema;

		if (turnedUp && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (turnedDown && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}
}
