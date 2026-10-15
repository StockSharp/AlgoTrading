using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Explosive strategy that enters positions on strong price moves.
/// Goes long on sharp increases and short on sharp decreases.
/// </summary>
public class LongExplosiveV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _priceIncreasePercent;
	private readonly StrategyParam<decimal> _priceDecreasePercent;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _previousClose;
	private int _barsSinceSignal;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal PriceIncreasePercent
	{
		get => _priceIncreasePercent.Value;
		set => _priceIncreasePercent.Value = value;
	}

	public decimal PriceDecreasePercent
	{
		get => _priceDecreasePercent.Value;
		set => _priceDecreasePercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public LongExplosiveV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_priceIncreasePercent = Param(nameof(PriceIncreasePercent), 0.5m)
			.SetDisplay("Price increase (%)", "Percentage increase to go long", "General");
		_priceDecreasePercent = Param(nameof(PriceDecreasePercent), 0.5m)
			.SetDisplay("Price decrease (%)", "Percentage decrease to go short", "General");
		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousClose = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousClose = 0;
		_barsSinceSignal = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (_previousClose == 0)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var change = (candle.ClosePrice - _previousClose) / _previousClose * 100m;
		_previousClose = candle.ClosePrice;

		if (_barsSinceSignal < CooldownBars)
			return;

		// Strong increase -> go long
		if (change > PriceIncreasePercent && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
		// Strong decrease -> go short
		else if (change < -PriceDecreasePercent && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
	}
}
