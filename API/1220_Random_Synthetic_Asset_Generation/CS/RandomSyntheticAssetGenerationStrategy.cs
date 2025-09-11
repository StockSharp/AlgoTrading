using System;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that generates a pseudo-random synthetic asset using the Wichmann–Hill or RS method.
/// </summary>
public class RandomSyntheticAssetGenerationStrategy : Strategy
{
	private readonly StrategyParam<int> _seed;
	private readonly StrategyParam<decimal> _intrabarVolatility;
	private readonly StrategyParam<decimal> _priceMultiplier;
	private readonly StrategyParam<RandomMethod> _method;

	private decimal _randomClose;
	private int _barIndex;
	private decimal _rsState = 1m;
	private decimal _s1;
	private decimal _s2;
	private decimal _s3;

	/// <summary>
	/// The pseudo-random generation method.
	/// </summary>
	public enum RandomMethod
	{
		/// <summary>Ricardo Santos method.</summary>
		Rs,

		/// <summary>Wichmann–Hill generator.</summary>
		WichmannHill,
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomSyntheticAssetGenerationStrategy"/> class.
	/// </summary>
	public RandomSyntheticAssetGenerationStrategy()
	{
		_seed = Param(nameof(Seed), 123456).SetCanOptimize(true);
		_intrabarVolatility = Param(nameof(IntrabarVolatility), 0.66m).SetDisplay("Intrabar Volatility").SetCanOptimize(true);
		_priceMultiplier = Param(nameof(PriceMultiplier), 30m).SetDisplay("Price Multiplier").SetCanOptimize(true);
		_method = Param(nameof(Method), RandomMethod.Rs).SetDisplay("Method").SetCanOptimize(true);
	}

	/// <summary>Seed [>= 0].</summary>
	public int Seed
	{
		get => _seed.Value;
		set => _seed.Value = value;
	}

	/// <summary>Intrabar volatility [0 to 2].</summary>
	public decimal IntrabarVolatility
	{
		get => _intrabarVolatility.Value;
		set => _intrabarVolatility.Value = value;
	}

	/// <summary>Price multiplier [> 0.00001].</summary>
	public decimal PriceMultiplier
	{
		get => _priceMultiplier.Value;
		set => _priceMultiplier.Value = value;
	}

	/// <summary>Pseudo-random generation method.</summary>
	public RandomMethod Method
	{
		get => _method.Value;
		set => _method.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_randomClose = 100m * PriceMultiplier;
		Timer.Start(TimeSpan.FromSeconds(1), GenerateCandle);
	}

	private void GenerateCandle()
	{
		var randomChange = RandomValue(Method, _barIndex + Seed);

		var randomClose = (100m + _randomClose + randomChange) * PriceMultiplier;
		var randomOpen = _randomClose * PriceMultiplier;

		var randomWickRange = RandomWick(Method, randomChange, IntrabarVolatility, _barIndex / 2 + Seed) * PriceMultiplier;
		var randomWickIntercept = Math.Abs(Math.Min(1m, RandomValue(Method, _barIndex / 3 + Seed) / 2m));

		decimal upperWick;
		decimal lowerWick;

		if (Rand(Method, 1m, _barIndex / 4 + Seed) > 0.5m)
		{
			upperWick = randomWickRange * randomWickIntercept;
			lowerWick = randomWickRange - upperWick;
		}
		else
		{
			lowerWick = randomWickRange * randomWickIntercept;
			upperWick = randomWickRange - lowerWick;
		}

		var randomHigh = Math.Max(randomClose, randomOpen) + upperWick;
		var randomLow = Math.Min(randomClose, randomOpen) - lowerWick;

		var randomVolume = Math.Abs(randomChange) * 100m;
		var randomTr = _barIndex == 0
			? randomHigh - randomLow
			: Math.Max(Math.Max(randomHigh - randomLow, Math.Abs(randomHigh - _randomClose * PriceMultiplier)), Math.Abs(randomLow - _randomClose * PriceMultiplier));

		_randomClose = randomClose / PriceMultiplier;
		_barIndex++;

		AddInfo($"O:{randomOpen:0.00} H:{randomHigh:0.00} L:{randomLow:0.00} C:{randomClose:0.00} V:{randomVolume:0.00} TR:{randomTr:0.00}");
	}

	private decimal Rand(RandomMethod method, decimal range, int seed)
	{
		if (method == RandomMethod.Rs)
		{
			var result = (decimal)Math.PI * (_rsState * _barIndex + seed);
			result %= range;
			_rsState = result;
			return result;
		}

		var germinate = seed * (_randomClose == 0 ? 1m : _randomClose) * DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		_s1 = (171m * (_s1 == 0 ? germinate : _s1)) % 30269m;
		_s2 = (172m * (_s2 == 0 ? _s1 * seed : _s2)) % 30307m;
		_s3 = (170m * (_s3 == 0 ? _s2 * seed : _s3)) % 30323m;
		return (_s1 / 30269m + _s2 / 30307m + _s3 / 30323m) % range;
	}

	private decimal RandomValue(RandomMethod method, int seed)
	{
		var rand1 = 0.1m + (decimal)Math.Pow(1 - Math.Log10(0.01 + (double)Rand(method, 10m, seed)), 2);
		var rand2 = Rand(method, 1m, seed + 1) + 1m;

		var randNormal = (decimal)Math.Sqrt(2 * Math.Log((double)(rand1 + 1m))) * (decimal)Math.Sin(2 * Math.PI * (double)rand2);

		return randNormal + (Rand(method, 0.1m, seed) - 0.05m);
	}

	private decimal RandomWick(RandomMethod method, decimal change, decimal intrabarVolatility, int seed)
	{
		var absChange = Math.Abs(change);
		var randValue = Rand(method, 1m, seed);

		var baseValue = Math.Log10((double)(10m - (absChange + 2m) / 2m * 5m));
		var wick = absChange * intrabarVolatility + (decimal)baseValue * randValue * (2m * Atanh(3m * randValue)) + absChange * Rand(method, intrabarVolatility, seed);

		return wick;
	}

	private static decimal Atanh(decimal value)
	{
		return 0.5m * (decimal)Math.Log((double)((1m + value) / (1m - value)));
	}
}