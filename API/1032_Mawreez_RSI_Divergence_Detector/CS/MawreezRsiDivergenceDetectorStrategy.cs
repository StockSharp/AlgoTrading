using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MawreezRsiDivergenceDetectorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _minDivLength;
	private readonly StrategyParam<int> _maxDivLength;
	private readonly StrategyParam<decimal> _minPriceMovePercent;
	private readonly StrategyParam<decimal> _minRsiMove;
	private readonly StrategyParam<int> _signalCooldownBars;

	private RelativeStrengthIndex _rsi;
	private decimal[] _priceHistory;
	private decimal[] _rsiHistory;
	private int _index;
	private int _barsFromSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int MinDivLength { get => _minDivLength.Value; set => _minDivLength.Value = value; }
	public int MaxDivLength { get => _maxDivLength.Value; set => _maxDivLength.Value = value; }
	public decimal MinPriceMovePercent { get => _minPriceMovePercent.Value; set => _minPriceMovePercent.Value = value; }
	public decimal MinRsiMove { get => _minRsiMove.Value; set => _minRsiMove.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public MawreezRsiDivergenceDetectorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General");
		_minDivLength = Param(nameof(MinDivLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Min Div Length", "Minimum divergence length", "General");
		_maxDivLength = Param(nameof(MaxDivLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Max Div Length", "Maximum divergence length", "General");
		_minPriceMovePercent = Param(nameof(MinPriceMovePercent), 0.35m)
			.SetGreaterThanZero()
			.SetDisplay("Min Price Move %", "Minimum price distance for divergence", "General");
		_minRsiMove = Param(nameof(MinRsiMove), 6m)
			.SetGreaterThanZero()
			.SetDisplay("Min RSI Move", "Minimum RSI distance for divergence", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsi = null;
		_priceHistory = null;
		_rsiHistory = null;
		_index = 0;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_priceHistory = new decimal[MaxDivLength + 1];
		_rsiHistory = new decimal[MaxDivLength + 1];
		_index = 0;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_rsi.IsFormed)
			return;

		var price = candle.ClosePrice;

		var pos = _index % _priceHistory.Length;
		_priceHistory[pos] = price;
		_rsiHistory[pos] = rsi;
		_index++;

		if (_index <= MaxDivLength)
			return;

		_barsFromSignal++;
		if (_barsFromSignal < SignalCooldownBars)
			return;

		int winner = 0;

		for (var l = MinDivLength; l <= MaxDivLength; l++)
		{
			var idx = (_index - l - 1) % _priceHistory.Length;
			if (idx < 0) idx += _priceHistory.Length;
			var pastPrice = _priceHistory[idx];
			var pastRsi = _rsiHistory[idx];

			var dsrc = price - pastPrice;
			var dosc = rsi - pastRsi;
			var priceMovePercent = price > 0m ? Math.Abs(dsrc) / price * 100m : 0m;
			var rsiMove = Math.Abs(dosc);

			if (priceMovePercent < MinPriceMovePercent || rsiMove < MinRsiMove)
				continue;

			if (Math.Sign(dsrc) == Math.Sign(dosc))
				continue;

			if (winner == 0)
			{
				if (dsrc < 0 && dosc > 0)
					winner = 1;
				else if (dsrc > 0 && dosc < 0)
					winner = -1;
			}
		}

		if (winner > 0 && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (winner < 0 && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}
	}
}
