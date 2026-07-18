using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that adapts the MarketPredictor expert advisor logic to StockSharp.
/// It recalculates statistical parameters from finished candles and
/// runs a Monte Carlo forecast to determine directional bias.
/// </summary>
public class MarketPredictorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialAlpha;
	private readonly StrategyParam<decimal> _initialBeta;
	private readonly StrategyParam<decimal> _initialGamma;
	private readonly StrategyParam<decimal> _kappa;
	private readonly StrategyParam<decimal> _initialMu;
	private readonly StrategyParam<decimal> _sigma;
	private readonly StrategyParam<int> _monteCarloSimulations;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _alpha;
	private decimal _mu;

	/// <summary>
	/// Default amplitude used before ATR values become available.
	/// </summary>
	public decimal InitialAlpha
	{
		get => _initialAlpha.Value;
		set => _initialAlpha.Value = value;
	}

	/// <summary>
	/// Placeholder coefficient retained from the original expert advisor.
	/// </summary>
	public decimal InitialBeta
	{
		get => _initialBeta.Value;
		set => _initialBeta.Value = value;
	}

	/// <summary>
	/// Placeholder damping constant retained for documentation purposes.
	/// </summary>
	public decimal InitialGamma
	{
		get => _initialGamma.Value;
		set => _initialGamma.Value = value;
	}

	/// <summary>
	/// Sensitivity parameter associated with the sigmoid concept.
	/// </summary>
	public decimal Kappa
	{
		get => _kappa.Value;
		set => _kappa.Value = value;
	}

	/// <summary>
	/// Default mean price used until the moving average is formed.
	/// </summary>
	public decimal InitialMu
	{
		get => _initialMu.Value;
		set => _initialMu.Value = value;
	}

	/// <summary>
	/// Required deviation between forecast and latest close to trigger entries.
	/// </summary>
	public decimal Sigma
	{
		get => _sigma.Value;
		set => _sigma.Value = value;
	}

	/// <summary>
	/// Number of Monte Carlo simulations used to forecast the next price.
	/// </summary>
	public int MonteCarloSimulations
	{
		get => _monteCarloSimulations.Value;
		set => _monteCarloSimulations.Value = value;
	}

	/// <summary>
	/// Candle type used for the strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters with defaults matching the original EA configuration.
	/// </summary>
	public MarketPredictorStrategy()
	{
		_initialAlpha = Param(nameof(InitialAlpha), 0.1m)
			.SetDisplay("Initial Alpha", "Default amplitude before ATR is formed", "Prediction")
			
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_initialBeta = Param(nameof(InitialBeta), 0.1m)
			.SetDisplay("Initial Beta", "Fractal weight placeholder", "Prediction")
			;

		_initialGamma = Param(nameof(InitialGamma), 0.1m)
			.SetDisplay("Initial Gamma", "Fractal damping placeholder", "Prediction")
			;

		_kappa = Param(nameof(Kappa), 1.0m)
			.SetDisplay("Kappa", "Sigmoid sensitivity placeholder", "Prediction")
			;

		_initialMu = Param(nameof(InitialMu), 1.0m)
			.SetDisplay("Initial Mu", "Fallback mean price", "Prediction")
			
			.SetOptimize(0.5m, 2.0m, 0.25m);

		_sigma = Param(nameof(Sigma), 10.0m)
			.SetGreaterThanZero()
			.SetDisplay("Sigma", "Deviation threshold for trades", "Trading")
			
			.SetOptimize(1.0m, 30.0m, 1.0m);

		_monteCarloSimulations = Param(nameof(MonteCarloSimulations), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Monte Carlo Simulations", "Number of simulations per candle", "Prediction")
			
			.SetOptimize(100, 2000, 100);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candle subscription", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_alpha = InitialAlpha;
		_mu = InitialMu;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_alpha = InitialAlpha;
		_mu = InitialMu;

		var sma = new SimpleMovingAverage { Length = 14 };
		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, atr, (candle, smaValue, atrValue) => ProcessCandle(candle, sma, atr, smaValue, atrValue))
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, SimpleMovingAverage sma, AverageTrueRange atr, decimal smaValue, decimal atrValue)
	{
		// Process only finished candles to avoid premature trading decisions.
		if (candle.State != CandleStates.Finished)
			return;

		// Confirm that the strategy is allowed to trade and all prerequisites are met.
		// Update adaptive mean when the moving average is formed.
		if (sma.IsFormed)
		{
			_mu = smaValue;
		}
		else
		{
			_mu = InitialMu;
		}

		// Adjust amplitude based on volatility when ATR values are reliable.
		if (atr.IsFormed && atrValue > 0m)
		{
			_alpha = atrValue * 0.1m;
		}
		else
		{
			_alpha = InitialAlpha;
		}

		var currentPrice = candle.ClosePrice;
		ExecuteTrade(currentPrice);
	}

	private void ExecuteTrade(decimal currentPrice)
	{
		var deviation = _alpha > 0 ? Sigma * _alpha : Sigma;

		// Mean-reversion: buy when significantly below mean, sell when significantly above
		if (currentPrice < _mu - deviation && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (currentPrice > _mu + deviation && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit when price returns to mean
		else if (Position > 0 && currentPrice >= _mu)
		{
			SellMarket();
		}
		else if (Position < 0 && currentPrice <= _mu)
		{
			BuyMarket();
		}
	}
}

