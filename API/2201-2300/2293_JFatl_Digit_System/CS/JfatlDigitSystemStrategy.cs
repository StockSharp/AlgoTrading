using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JFATL Digit System based on the slope of the Jurik moving average.
/// Opens long when the JMA turns upward and short when it turns downward.
/// </summary>
public class JfatlDigitSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _jmaPhase;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevJma;
	private decimal? _prevSlope;

	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	public int JmaPhase
	{
		get => _jmaPhase.Value;
		set => _jmaPhase.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public JfatlDigitSystemStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "JMA period", "Parameters")
			.SetOptimize(3, 20, 1);

		_jmaPhase = Param(nameof(JmaPhase), -100)
			.SetDisplay("JMA Phase", "JMA phase", "Parameters")
			.SetOptimize(-100, 100, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "Parameters");
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
		_prevJma = null;
		_prevSlope = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevJma = null;
		_prevSlope = null;

		var jma = new JurikMovingAverage
		{
			Length = JmaLength,
			Phase = JmaPhase
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevJma is decimal prev)
		{
			var slope = jmaValue - prev;

			if (_prevSlope is decimal prevSlope)
			{
				var turnedUp = prevSlope <= 0 && slope > 0;
				var turnedDown = prevSlope >= 0 && slope < 0;

				if (turnedUp && Position <= 0)
					BuyMarket();
				else if (turnedDown && Position >= 0)
					SellMarket();
			}

			_prevSlope = slope;
		}

		_prevJma = jmaValue;
	}
}
