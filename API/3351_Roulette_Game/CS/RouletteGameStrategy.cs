namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Roulette inspired strategy that randomly selects trade direction and scales stake size after losses.
/// The approach mirrors the original MetaTrader game by keeping track of a virtual bankroll and betting rounds.
/// </summary>
public class RouletteGameStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _lossMultiplier;
	private readonly StrategyParam<decimal> _maxMultiplier;
	private readonly StrategyParam<int> _roundCooldown;
	private readonly StrategyParam<int> _maxLosingStreak;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _currentMultiplier;
	private decimal _entryPrice;
	private int _cooldownCounter;
	private int _losingStreak;
	private bool _betIsLong;
	private bool _hasOpenBet;
	private readonly Random _random = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="RouletteGameStrategy"/> class.
	/// </summary>
	public RouletteGameStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base volume", "Initial number of contracts traded for the first bet.", "Trading");

		_lossMultiplier = Param(nameof(LossMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Loss multiplier", "Multiplier applied to the stake after a losing round.", "Money management");

		_maxMultiplier = Param(nameof(MaxMultiplier), 16m)
			.SetGreaterThanZero()
			.SetDisplay("Max multiplier", "Ceiling for the stake multiplier to limit risk.", "Money management");

		_roundCooldown = Param(nameof(RoundCooldown), 1)
			.SetNotNegative()
			.SetDisplay("Round cooldown", "Number of finished candles to wait before opening the next bet.", "Timing");

		_maxLosingStreak = Param(nameof(MaxLosingStreak), 5)
			.SetNotNegative()
			.SetDisplay("Max losing streak", "Number of consecutive losing rounds allowed before a reset.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe controlling when roulette rounds are evaluated.", "General");
	}

	/// <summary>
	/// Base volume traded on the first round.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the stake after a losing round.
	/// </summary>
	public decimal LossMultiplier
	{
		get => _lossMultiplier.Value;
		set => _lossMultiplier.Value = value;
	}

	/// <summary>
	/// Upper cap for the stake multiplier.
	/// </summary>
	public decimal MaxMultiplier
	{
		get => _maxMultiplier.Value;
		set => _maxMultiplier.Value = value;
	}

	/// <summary>
	/// Number of candles to skip between rounds.
	/// </summary>
	public int RoundCooldown
	{
		get => _roundCooldown.Value;
		set => _roundCooldown.Value = value;
	}

	/// <summary>
	/// Maximum allowed consecutive losses before the stake resets.
	/// </summary>
	public int MaxLosingStreak
	{
		get => _maxLosingStreak.Value;
		set => _maxLosingStreak.Value = value;
	}

	/// <summary>
	/// Candle type used to schedule rounds.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentMultiplier = 1m;
		_entryPrice = 0m;
		_cooldownCounter = 0;
		_losingStreak = 0;
		_hasOpenBet = false;

		// Enable built-in protection once at start so accidental manual positions are handled.
		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_hasOpenBet)
		{
			EvaluateRound(candle);
			return;
		}

		if (_cooldownCounter > 0)
		{
			_cooldownCounter--;
			return;
		}

		StartNewRound(candle);
	}

	private void StartNewRound(ICandleMessage candle)
	{
		// Decide trade direction randomly to imitate roulette color selection.
		_betIsLong = _random.Next(0, 2) == 0;

		var volume = BaseVolume * _currentMultiplier;
		var step = Security?.VolumeStep;
		if (step != null && step.Value > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step.Value, MidpointRounding.AwayFromZero));
			volume = steps * step.Value;
		}

		if (volume <= 0m)
			return;

		if (_betIsLong)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		_hasOpenBet = true;
	}

	private void EvaluateRound(ICandleMessage candle)
	{
		if (Position == 0m)
			return; // Wait until the market position is opened.

		var isWinningRound = _betIsLong
			? candle.ClosePrice >= _entryPrice
			: candle.ClosePrice <= _entryPrice;

		CloseCurrentPosition();

		if (isWinningRound)
		{
			// Reset stake after a win just like the original roulette balance animation.
			_currentMultiplier = 1m;
			_losingStreak = 0;
		}
		else
		{
			_losingStreak++;
			_currentMultiplier = Math.Min(_currentMultiplier * LossMultiplier, MaxMultiplier);

			if (MaxLosingStreak > 0 && _losingStreak >= MaxLosingStreak)
			{
				_currentMultiplier = 1m;
				_losingStreak = 0;
			}
		}

		_hasOpenBet = false;
		_cooldownCounter = RoundCooldown;
	}

	private void CloseCurrentPosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}
}
