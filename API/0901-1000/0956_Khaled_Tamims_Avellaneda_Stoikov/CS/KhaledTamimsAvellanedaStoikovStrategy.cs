using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Khaled Tamim's Avellaneda-Stoikov strategy.
/// </summary>
public class KhaledTamimsAvellanedaStoikovStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<decimal> _sigma;
	private readonly StrategyParam<decimal> _t;
	private readonly StrategyParam<decimal> _k;
	private readonly StrategyParam<decimal> _m;
	private readonly StrategyParam<decimal> _fee;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private bool _isFirst = true;
	private int _entriesExecuted;
	private int _barsSinceSignal;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public KhaledTamimsAvellanedaStoikovStrategy()
	{
		_gamma = Param("Gamma", 2m).SetDisplay("Gamma", "Gamma", "General");
		_sigma = Param("Sigma", 8m).SetDisplay("Sigma", "Sigma", "General");
		_t = Param("T", 0.0833m).SetDisplay("T", "T", "General");
		_k = Param("K", 5m).SetDisplay("K", "K", "General");
		_m = Param("M", 0.5m).SetDisplay("M", "M", "General");
		_fee = Param("Fee", 0m).SetDisplay("Fee", "Fee", "General");
		_maxEntries = Param("Max Entries", 45).SetDisplay("Max Entries", "Maximum entries per run", "Risk");
		_cooldownBars = Param("Cooldown Bars", 12000).SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk");
		_candleType = Param("Candle type", TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Candle Type", "General");
	}

	public decimal Gamma { get => _gamma.Value; set => _gamma.Value = value; }
	public decimal Sigma { get => _sigma.Value; set => _sigma.Value = value; }
	public decimal T { get => _t.Value; set => _t.Value = value; }
	public decimal K { get => _k.Value; set => _k.Value = value; }
	public decimal M { get => _m.Value; set => _m.Value = value; }
	public decimal Fee { get => _fee.Value; set => _fee.Value = value; }
	public int MaxEntries { get => _maxEntries.Value; set => _maxEntries.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
		_isFirst = true;
		_entriesExecuted = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entriesExecuted = 0;
		_barsSinceSignal = CooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (_isFirst)
		{
			_prevClose = candle.ClosePrice;
			_isFirst = false;
			return;
		}

		var midPrice = (candle.ClosePrice + _prevClose) / 2m;
		_prevClose = candle.ClosePrice;

		var sqrtTerm = Gamma * Sigma * Sigma * T;
		var bidQuote = midPrice - K * sqrtTerm - (midPrice * Fee);
		var askQuote = midPrice + K * sqrtTerm + (midPrice * Fee);

		var longCondition = candle.ClosePrice < bidQuote - M;
		var shortCondition = candle.ClosePrice > askQuote + M;

		if (_entriesExecuted >= MaxEntries || _barsSinceSignal < CooldownBars)
			return;

		if (longCondition && Position <= 0)
		{
			BuyMarket();
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket();
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
	}
}

