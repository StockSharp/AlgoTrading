using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe RSI strategy.
/// </summary>
public class MtfOscillatorFrameworkStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;

	private decimal? _baseRsi;
	private decimal? _higherRsi;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MtfOscillatorFrameworkStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Higher Candle Type", "Type of higher timeframe candles", "General");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI calculation length", "Indicators")
			.SetCanOptimize(true);
		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Overbought threshold", "Indicators")
			.SetCanOptimize(true);
		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Oversold threshold", "Indicators")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Overbought threshold.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Oversold threshold.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var baseRsi = new RelativeStrengthIndex { Length = RsiLength };
		var higherRsi = new RelativeStrengthIndex { Length = RsiLength };

		var baseSub = SubscribeCandles(CandleType);
		baseSub.Bind(baseRsi, OnBaseRsi).Start();

		var higherSub = SubscribeCandles(HigherCandleType);
		higherSub.Bind(higherRsi, OnHigherRsi).Start();

		StartProtection();
	}

	private void OnBaseRsi(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_baseRsi = rsi;
		ProcessSignal();
	}

	private void OnHigherRsi(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherRsi = rsi;
		ProcessSignal();
	}

	private void ProcessSignal()
	{
		if (_baseRsi is not decimal baseRsi || _higherRsi is not decimal higherRsi)
			return;

		if (baseRsi > Overbought && higherRsi > Overbought && Position <= 0)
			SellMarket();
		else if (baseRsi < Oversold && higherRsi < Oversold && Position >= 0)
			BuyMarket();
	}
}
