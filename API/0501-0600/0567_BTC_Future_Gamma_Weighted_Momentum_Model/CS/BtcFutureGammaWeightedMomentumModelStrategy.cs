using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC Future Gamma-Weighted Momentum Model strategy.
/// The gamma-weighted average price (GWAP) averages the last Length closes with weight GammaFactor^i for the close i bars ago.
/// A close above GWAP after three consecutively rising closes goes long, a close below GWAP after three consecutively falling closes
/// goes short, and the opposite signal reverses the position.
/// </summary>
public class BtcFutureGammaWeightedMomentumModelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _gammaFactor;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();

	/// <summary>
	/// Closes in the GWAP window.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Decay of the weight per bar back.
	/// </summary>
	public decimal GammaFactor
	{
		get => _gammaFactor.Value;
		set => _gammaFactor.Value = value;
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
	public BtcFutureGammaWeightedMomentumModelStrategy()
	{
		_length = Param(nameof(Length), 60)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Closes in the GWAP window", "GWAP");

		_gammaFactor = Param(nameof(GammaFactor), 0.75m)
			.SetRange(0.01m, 1m)
			.SetDisplay("Gamma Factor", "Decay of the weight per bar back", "GWAP");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_closes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closes.Clear();

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

		_closes.Add(candle.ClosePrice);
		var keep = Math.Max(Length, 3);
		while (_closes.Count > keep)
			_closes.RemoveAt(0);

		if (_closes.Count < keep)
			return;

		var last = _closes.Count - 1;

		decimal weighted = 0, weights = 0, weight = 1;
		for (var i = 0; i < Length; i++)
		{
			weighted += _closes[last - i] * weight;
			weights += weight;
			weight *= GammaFactor;
		}

		var gwap = weighted / weights;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = _closes[last];
		var rising = close > _closes[last - 1] && _closes[last - 1] > _closes[last - 2];
		var falling = close < _closes[last - 1] && _closes[last - 1] < _closes[last - 2];

		if (close > gwap && rising && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < gwap && falling && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
