using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams Trader strategy based on Alligator and Fractals.
/// Buys when price breaks above an upper fractal above the Alligator teeth.
/// Sells when price breaks below a lower fractal below the Alligator teeth.
/// Exits when price crosses the Alligator lips in the opposite direction.
/// </summary>
public class BillWilliamsTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private readonly decimal[] _highBuffer = new decimal[5];
	private readonly decimal[] _lowBuffer = new decimal[5];

	private decimal? _upFractal;
	private decimal? _downFractal;

	/// <summary>
	/// Jaw SMMA period.
	/// </summary>
	public int JawLength
	{
		get => _jawLength.Value;
		set => _jawLength.Value = value;
	}

	/// <summary>
	/// Teeth SMMA period.
	/// </summary>
	public int TeethLength
	{
		get => _teethLength.Value;
		set => _teethLength.Value = value;
	}

	/// <summary>
	/// Lips SMMA period.
	/// </summary>
	public int LipsLength
	{
		get => _lipsLength.Value;
		set => _lipsLength.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BillWilliamsTraderStrategy"/>.
	/// </summary>
	public BillWilliamsTraderStrategy()
	{
		_jawLength = Param(nameof(JawLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Length", "Alligator jaw period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_teethLength = Param(nameof(TeethLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Length", "Alligator teeth period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_lipsLength = Param(nameof(LipsLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Length", "Alligator lips period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		Array.Clear(_highBuffer);
		Array.Clear(_lowBuffer);
		_upFractal = null;
		_downFractal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jaw = new SmoothedMovingAverage { Length = JawLength };
		_teeth = new SmoothedMovingAverage { Length = TeethLength };
		_lips = new SmoothedMovingAverage { Length = LipsLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawVal = _jaw.Process(new DecimalIndicatorValue(_jaw, median, candle.ServerTime));
		var teethVal = _teeth.Process(new DecimalIndicatorValue(_teeth, median, candle.ServerTime));
		var lipsVal = _lips.Process(new DecimalIndicatorValue(_lips, median, candle.ServerTime));

		// shift buffers for fractal detection
		for (var i = 0; i < 4; i++)
		{
			_highBuffer[i] = _highBuffer[i + 1];
			_lowBuffer[i] = _lowBuffer[i + 1];
		}
		_highBuffer[4] = candle.HighPrice;
		_lowBuffer[4] = candle.LowPrice;

		if (candle.State != CandleStates.Finished || !jawVal.IsFormed || !teethVal.IsFormed || !lipsVal.IsFormed)
			return;

		// detect fractals using the middle bar
		var h2 = _highBuffer[2];
		if (h2 > _highBuffer[0] && h2 > _highBuffer[1] && h2 > _highBuffer[3] && h2 > _highBuffer[4])
			_upFractal = h2;

		var l2 = _lowBuffer[2];
		if (l2 < _lowBuffer[0] && l2 < _lowBuffer[1] && l2 < _lowBuffer[3] && l2 < _lowBuffer[4])
			_downFractal = l2;

		var jaw = jawVal.ToDecimal();
		var teeth = teethVal.ToDecimal();
		var lips = lipsVal.ToDecimal();

		// entry conditions
		if (_upFractal is decimal up && candle.ClosePrice > up && up > teeth && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_upFractal = null;
		}
		else if (_downFractal is decimal down && candle.ClosePrice < down && down < teeth && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_downFractal = null;
		}

		// exit conditions based on lips cross
		if (Position > 0 && candle.ClosePrice < lips)
			SellMarket(Position);
		else if (Position < 0 && candle.ClosePrice > lips)
			BuyMarket(-Position);
	}
}
