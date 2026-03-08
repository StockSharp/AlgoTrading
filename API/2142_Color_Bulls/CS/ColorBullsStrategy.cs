using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Color Bulls indicator.
/// Opens long when bulls value switches from rising to falling.
/// Opens short when value switches from falling to rising.
/// </summary>
public class ColorBullsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevValue;
	private int _prevColor;

	/// <summary>
	/// Period for moving average applied to high prices.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Period for smoothing bulls value.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public ColorBullsStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Period of high price moving average", "Indicator")
			.SetOptimize(5, 20, 1);

		_smoothLength = Param(nameof(SmoothLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "Period of smoothing moving average", "Indicator")
			.SetOptimize(3, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevValue = 0m;
		_prevColor = 1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highMa = new ExponentialMovingAverage { Length = FastLength };
		var bullsMa = new ExponentialMovingAverage { Length = SmoothLength };

		Indicators.Add(highMa);
		Indicators.Add(bullsMa);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var highInput = new DecimalIndicatorValue(highMa, candle.HighPrice, candle.OpenTime) { IsFinal = true };
			var maValue = highMa.Process(highInput);
			if (!highMa.IsFormed)
				return;

			var bulls = candle.HighPrice - maValue.ToDecimal();
			var bullsInput = new DecimalIndicatorValue(bullsMa, bulls, candle.OpenTime) { IsFinal = true };
			var smooth = bullsMa.Process(bullsInput).ToDecimal();
			if (!bullsMa.IsFormed)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var color = smooth > _prevValue ? 0 : smooth < _prevValue ? 2 : 1;

			if (_prevColor == 0 && color == 2)
			{
				if (Position < 0)
					BuyMarket();
				if (Position <= 0)
					BuyMarket();
			}
			else if (_prevColor == 2 && color == 0)
			{
				if (Position > 0)
					SellMarket();
				if (Position >= 0)
					SellMarket();
			}

			_prevColor = color;
			_prevValue = smooth;
		}
	}
}
