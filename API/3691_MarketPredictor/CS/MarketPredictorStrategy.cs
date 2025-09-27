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

	private readonly Random _random = new();

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
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_initialBeta = Param(nameof(InitialBeta), 0.1m)
			.SetDisplay("Initial Beta", "Fractal weight placeholder", "Prediction")
			.SetCanOptimize(false);

		_initialGamma = Param(nameof(InitialGamma), 0.1m)
			.SetDisplay("Initial Gamma", "Fractal damping placeholder", "Prediction")
			.SetCanOptimize(false);

		_kappa = Param(nameof(Kappa), 1.0m)
			.SetDisplay("Kappa", "Sigmoid sensitivity placeholder", "Prediction")
			.SetCanOptimize(false);

		_initialMu = Param(nameof(InitialMu), 1.0m)
			.SetDisplay("Initial Mu", "Fallback mean price", "Prediction")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2.0m, 0.25m);

		_sigma = Param(nameof(Sigma), 10.0m)
			.SetGreaterThanZero()
			.SetDisplay("Sigma", "Deviation threshold for trades", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 30.0m, 1.0m);

		_monteCarloSimulations = Param(nameof(MonteCarloSimulations), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Monte Carlo Simulations", "Number of simulations per candle", "Prediction")
			.SetCanOptimize(true)
			.SetOptimize(100, 2000, 100);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_alpha = InitialAlpha;
		_mu = InitialMu;

		var sma = new SMA { Length = 14 };
		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, atr, (candle, smaValue, atrValue) => ProcessCandle(candle, sma, atr, smaValue, atrValue))
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, SMA sma, AverageTrueRange atr, decimal smaValue, decimal atrValue)
	{
		// Process only finished candles to avoid premature trading decisions.
		if (candle.State != CandleStates.Finished)
			return;

		// Confirm that the strategy is allowed to trade and all prerequisites are met.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

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
		var predictedPrice = PredictNextPrice(currentPrice, Sigma, MonteCarloSimulations);

		ExecuteTrade(currentPrice, predictedPrice);
	}

	private decimal PredictNextPrice(decimal currentPrice, decimal sigma, int simulations)
	{
		// Guard against invalid configuration.
		if (simulations <= 0)
			return currentPrice;

		decimal sum = 0m;

		for (var i = 0; i < simulations; i++)
		{
			// Generate a random variation between -0.5 and 0.5.
			var randomFactor = (decimal)_random.NextDouble() - 0.5m;

			// Calculate the simulated price around the latest close.
			var simulatedPrice = currentPrice + randomFactor * sigma;

			sum += simulatedPrice;
		}

		// The forecast equals the mean of all simulated prices.
		return sum / simulations;
	}

	private void ExecuteTrade(decimal currentPrice, decimal predictedPrice)
	{
		// Long entry when the forecast exceeds the upper threshold.
		if (predictedPrice > currentPrice + Sigma)
		{
			if (Position <= 0)
			{
				BuyMarket();
				LogInfo($"Buy signal. Close={currentPrice}, Forecast={predictedPrice}, Sigma={Sigma}, Alpha={_alpha}, Mu={_mu}");
			}

			return;
		}

		// Short entry when the forecast drops below the lower threshold.
		if (predictedPrice < currentPrice - Sigma)
		{
			if (Position >= 0)
			{
				SellMarket();
				LogInfo($"Sell signal. Close={currentPrice}, Forecast={predictedPrice}, Sigma={Sigma}, Alpha={_alpha}, Mu={_mu}");
			}
		}
	}
}

