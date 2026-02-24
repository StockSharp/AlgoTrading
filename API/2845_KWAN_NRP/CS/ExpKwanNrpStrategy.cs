namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy replicating the Exp KWAN NRP expert advisor by combining RSI and Momentum signals.
/// Enters long when RSI crosses above 50 and momentum is positive,
/// enters short when RSI crosses below 50 and momentum is negative.
/// </summary>
public class ExpKwanNrpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _momentumPeriod;

	private decimal _prevRsi;
	private decimal _prevMom;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public ExpKwanNrpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI length", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum length", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = 0;
		_prevMom = 0;
		_initialized = false;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, momentum, (ICandleMessage candle, decimal rsiValue, decimal momValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!_initialized)
				{
					_prevRsi = rsiValue;
					_prevMom = momValue;
					_initialized = true;
					return;
				}

				var rsiUp = rsiValue > _prevRsi;
				var rsiDown = rsiValue < _prevRsi;
				var momUp = momValue > _prevMom;
				var momDown = momValue < _prevMom;

				// Buy when both RSI and Momentum are turning up
				if (rsiUp && momUp && Position <= 0)
				{
					BuyMarket();
				}
				// Sell when both RSI and Momentum are turning down
				else if (rsiDown && momDown && Position >= 0)
				{
					SellMarket();
				}

				_prevRsi = rsiValue;
				_prevMom = momValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
}
