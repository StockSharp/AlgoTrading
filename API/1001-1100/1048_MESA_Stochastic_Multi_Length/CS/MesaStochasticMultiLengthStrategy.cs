using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MesaStochasticMultiLengthStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = new();
	private decimal _prevStoch1;
	private decimal _prevStoch2;
	private int _barsFromSignal;

	public int Length1 { get => _length1.Value; set => _length1.Value = value; }
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }
	public decimal UpperLevel { get => _upperLevel.Value; set => _upperLevel.Value = value; }
	public decimal LowerLevel { get => _lowerLevel.Value; set => _lowerLevel.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MesaStochasticMultiLengthStrategy()
	{
		_length1 = Param(nameof(Length1), 50)
			.SetGreaterThanZero()
			.SetDisplay("Length 1", "Primary stochastic length", "General");
		_length2 = Param(nameof(Length2), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length 2", "Secondary stochastic length", "General");
		_upperLevel = Param(nameof(UpperLevel), 0.60m)
			.SetDisplay("Upper Level", "Upper signal level", "General");
		_lowerLevel = Param(nameof(LowerLevel), 0.40m)
			.SetDisplay("Lower Level", "Lower signal level", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prices.Clear();
		_prevStoch1 = 0.5m;
		_prevStoch2 = 0.5m;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_prices.Clear();
		_prevStoch1 = 0.5m;
		_prevStoch2 = 0.5m;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;
		_prices.Add(price);

		var maxLen = Math.Max(Length1, Length2);
		if (_prices.Count > maxLen + 10)
			_prices.RemoveAt(0);

		if (_prices.Count < maxLen)
			return;

		var stoch1 = CalcStochastic(_prices, Length1);
		var stoch2 = CalcStochastic(_prices, Length2);

		var up = stoch1 > UpperLevel && stoch2 > UpperLevel && _prevStoch1 <= UpperLevel;
		var down = stoch1 < LowerLevel && stoch2 < LowerLevel && _prevStoch1 >= LowerLevel;
		_barsFromSignal++;

		if (_barsFromSignal >= SignalCooldownBars && up && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (_barsFromSignal >= SignalCooldownBars && down && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}

		_prevStoch1 = stoch1;
		_prevStoch2 = stoch2;
	}

	private static decimal CalcStochastic(List<decimal> prices, int length)
	{
		var count = prices.Count;
		if (count < length) return 0.5m;

		var high = decimal.MinValue;
		var low = decimal.MaxValue;
		for (int i = count - length; i < count; i++)
		{
			if (prices[i] > high) high = prices[i];
			if (prices[i] < low) low = prices[i];
		}

		if (high == low) return 0.5m;
		return (prices[count - 1] - low) / (high - low);
	}
}
