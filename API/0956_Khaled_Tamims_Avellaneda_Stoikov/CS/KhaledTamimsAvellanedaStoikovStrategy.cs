using System;

using StockSharp.Algo.Strategies;
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
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private bool _isFirst = true;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public KhaledTamimsAvellanedaStoikovStrategy()
	{
		_gamma = Param("Gamma", 2m).SetDisplay("Gamma").SetCanOptimize(true);
		_sigma = Param("Sigma", 8m).SetDisplay("Sigma").SetCanOptimize(true);
		_t = Param("T", 0.0833m).SetDisplay("T").SetCanOptimize(true);
		_k = Param("K", 5m).SetDisplay("K").SetCanOptimize(true);
		_m = Param("M", 0.5m).SetDisplay("M").SetCanOptimize(true);
		_fee = Param("Fee", 0m).SetDisplay("Fee").SetCanOptimize(true);
		_candleType = Param("Candle type", TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type").SetCanOptimize(false);
	}

	public decimal Gamma { get => _gamma.Value; set => _gamma.Value = value; }
	public decimal Sigma { get => _sigma.Value; set => _sigma.Value = value; }
	public decimal T { get => _t.Value; set => _t.Value = value; }
	public decimal K { get => _k.Value; set => _k.Value = value; }
	public decimal M { get => _m.Value; set => _m.Value = value; }
	public decimal Fee { get => _fee.Value; set => _fee.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

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

		if (longCondition && Position <= 0)
			BuyMarket();
		else if (shortCondition && Position >= 0)
			SellMarket();
	}
}

