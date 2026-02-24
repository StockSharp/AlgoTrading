using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exp BlauHLM strategy: uses smoothed HLM (High-Low-Median) oscillator concept.
/// Approximated using Momentum + EMA smoothing to detect trend changes.
/// </summary>
public class ExpBlauHlmStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _smoothLength;

	private decimal _prevMom;
	private decimal _prevSmooth;
	private bool _hasPrev;

	/// <summary>
	/// Constructor.
	/// </summary>
	public ExpBlauHlmStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_momentumLength = Param(nameof(MomentumLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Momentum period", "Indicators");

		_smoothLength = Param(nameof(SmoothLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "EMA smoothing period", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var momentum = new Momentum { Length = MomentumLength };
		var ema = new EMA { Length = SmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// HLM approximation: momentum as oscillator, ema as trend
		// Buy when momentum crosses above 100 and price above EMA
		// Sell when momentum crosses below 100 and price below EMA

		if (_hasPrev)
		{
			if (_prevMom <= 100 && momValue > 100 && candle.ClosePrice > emaValue && Position <= 0)
			{
				BuyMarket();
			}
			else if (_prevMom >= 100 && momValue < 100 && candle.ClosePrice < emaValue && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevMom = momValue;
		_prevSmooth = emaValue;
		_hasPrev = true;
	}
}
