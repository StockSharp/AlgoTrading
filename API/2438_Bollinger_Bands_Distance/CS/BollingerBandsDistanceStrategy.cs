namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy trading reversals from Bollinger Bands with extra distance.
/// </summary>
public class BollingerBandsDistanceStrategy : Strategy
{
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<decimal> _bandDistance;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _lossLimit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int BollingerPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}

	public decimal BandDistance
	{
		get => _bandDistance.Value;
		set => _bandDistance.Value = value;
	}

	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	public decimal LossLimit
	{
		get => _lossLimit.Value;
		set => _lossLimit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BollingerBandsDistanceStrategy()
	{
		_bbPeriod = Param(nameof(BollingerPeriod), 4)
			.SetDisplay("BB Period", "Bollinger Bands length", "Parameters");

		_bbDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Deviation", "Bollinger Bands deviation", "Parameters");

		_bandDistance = Param(nameof(BandDistance), 3m)
			.SetDisplay("Band Distance", "Extra distance from bands in price steps", "Parameters");

		_profitTarget = Param(nameof(ProfitTarget), 3m)
			.SetDisplay("Profit Target", "Take profit in price steps", "Risk");

		_lossLimit = Param(nameof(LossLimit), 20m)
			.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bb, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var step = Security?.PriceStep ?? 1m;
		var distance = BandDistance * step;
		var profit = ProfitTarget * step;
		var loss = LossLimit * step;

		if (Position > 0)
		{
			var current = close - _entryPrice;

			if ((ProfitTarget > 0 && current >= profit) ||
				(LossLimit > 0 && current <= -loss))
				ClosePosition();
		}
		else if (Position < 0)
		{
			var current = _entryPrice - close;

			if ((ProfitTarget > 0 && current >= profit) ||
				(LossLimit > 0 && current <= -loss))
				ClosePosition();
		}
		else
		{
			if (close > upper + distance)
			{
				SellMarket();
				_entryPrice = close;
			}
			else if (close < lower - distance)
			{
				BuyMarket();
				_entryPrice = close;
			}
		}
	}
}
