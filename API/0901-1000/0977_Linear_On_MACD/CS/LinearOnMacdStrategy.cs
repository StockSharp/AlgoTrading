using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Linear On MACD strategy.
/// One MACD runs on the close and another on candle volume, and a Lookback linear regression projects the price. A long opens when
/// both MACDs are above their signal lines and the regression price lies between the candle open and close; a short opens when both
/// are below their signals under the same regression condition. With RiskHigh enabled one MACD agreeing is enough. Opposite signals
/// reverse the position.
/// </summary>
public class LinearOnMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<bool> _riskHigh;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _volumeMacd;

	/// <summary>
	/// Fast EMA period of both MACDs.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period of both MACDs.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line period of both MACDs.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Linear regression lookback.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Accept a signal when only one of the two MACDs agrees.
	/// </summary>
	public bool RiskHigh
	{
		get => _riskHigh.Value;
		set => _riskHigh.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LinearOnMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period of both MACDs", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period of both MACDs", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal line period of both MACDs", "MACD");

		_lookback = Param(nameof(Lookback), 21)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Linear regression lookback", "Regression");

		_riskHigh = Param(nameof(RiskHigh), false)
			.SetDisplay("Risk High", "Accept a signal when only one MACD agrees", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
		return new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var priceMacd = CreateMacd();
		_volumeMacd = CreateMacd();
		var regression = new LinearReg { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(priceMacd, regression, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, regression);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, priceMacd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue priceMacdValue, IIndicatorValue regressionValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeMacdValue = _volumeMacd.Process(new DecimalIndicatorValue(_volumeMacd, candle.TotalVolume, candle.OpenTime) { IsFinal = true });

		if (!priceMacdValue.IsFormed || !regressionValue.IsFormed || !volumeMacdValue.IsFormed)
			return;

		if (priceMacdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal priceMacd, Signal: decimal priceSignal } ||
			volumeMacdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal volumeMacd, Signal: decimal volumeSignal })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var predicted = regressionValue.GetValue<decimal>();
		var bodyLow = Math.Min(candle.OpenPrice, candle.ClosePrice);
		var bodyHigh = Math.Max(candle.OpenPrice, candle.ClosePrice);
		var inBody = predicted >= bodyLow && predicted <= bodyHigh;

		var priceUp = priceMacd > priceSignal;
		var volumeUp = volumeMacd > volumeSignal;
		var priceDown = priceMacd < priceSignal;
		var volumeDown = volumeMacd < volumeSignal;

		var bullish = RiskHigh ? priceUp || volumeUp : priceUp && volumeUp;
		var bearish = RiskHigh ? priceDown || volumeDown : priceDown && volumeDown;

		if (inBody && bullish && !bearish && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (inBody && bearish && !bullish && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
