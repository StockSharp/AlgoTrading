namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive grid strategy that trades two correlated instruments using breakout levels.
/// </summary>
public class MultiArbitration11xxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _secondSecurity;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<int> _maxOpenUnits;
	private readonly StrategyParam<decimal> _secondVolume;

	private decimal _lastCloseOne;
	private decimal _lastCloseTwo;
	private decimal _lowestBuyOne;
	private decimal _highestSellOne;
	private decimal _lowestBuyTwo;
	private decimal _highestSellTwo;

	private DateTimeOffset _lastCandleTimeOne;
	private DateTimeOffset _lastCandleTimeTwo;
	private DateTimeOffset _lastProcessedTime;
	private decimal _initialPortfolioValue;

	/// <summary>
	/// Candle type used to generate trading signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Second security traded alongside the primary <see cref="Strategy.Security"/>.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	/// <summary>
	/// Profit target that triggers full liquidation of the portfolio.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Maximum combined open exposure measured in units of volume.
	/// </summary>
	public int MaxOpenUnits
	{
		get => _maxOpenUnits.Value;
		set => _maxOpenUnits.Value = value;
	}

	/// <summary>
	/// Trade volume for the second security.
	/// </summary>
	public decimal SecondVolume
	{
		get => _secondVolume.Value;
		set => _secondVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiArbitration11xxStrategy"/> class.
	/// </summary>
	public MultiArbitration11xxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Signal Candles", "Time frame used for signal generation", "General");

		_secondSecurity = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Security", "Additional instrument traded by the grid", "General")
			.SetRequired();

		_profitTarget = Param(nameof(ProfitTarget), 300m)
			.SetDisplay("Profit Target", "Profit threshold that triggers liquidation", "Risk Management");

		_maxOpenUnits = Param(nameof(MaxOpenUnits), 20)
			.SetDisplay("Max Open Units", "Maximum combined exposure across both securities", "Risk Management")
			.SetGreaterThanZero();

		_secondVolume = Param(nameof(SecondVolume), 1m)
			.SetDisplay("Second Volume", "Trade volume used for the secondary security", "Trading")
			.SetGreaterThanZero();

		Volume = 1m;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(SecondSecurity, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastCloseOne = 0m;
		_lastCloseTwo = 0m;
		_lowestBuyOne = decimal.MaxValue;
		_lowestBuyTwo = decimal.MaxValue;
		_highestSellOne = decimal.MinValue;
		_highestSellTwo = decimal.MinValue;
		_lastCandleTimeOne = default;
		_lastCandleTimeTwo = default;
		_lastProcessedTime = default;
		_initialPortfolioValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SecondSecurity == null)
		{
			throw new InvalidOperationException("Second security is not specified.");
		}

		var firstSubscription = SubscribeCandles(CandleType);
		firstSubscription
			.Bind(ProcessFirstSecurity)
			.Start();

		var secondSubscription = SubscribeCandles(CandleType, security: SecondSecurity);
		secondSubscription
			.Bind(ProcessSecondSecurity)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, firstSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessFirstSecurity(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		_lastCloseOne = candle.ClosePrice;
		_lastCandleTimeOne = candle.OpenTime;

		TryEvaluate();
	}

	private void ProcessSecondSecurity(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		_lastCloseTwo = candle.ClosePrice;
		_lastCandleTimeTwo = candle.OpenTime;

		TryEvaluate();
	}

	private void TryEvaluate()
	{
		if (_lastCloseOne <= 0m || _lastCloseTwo <= 0m)
		{
			return;
		}

		if (_lastCandleTimeOne != _lastCandleTimeTwo)
		{
			return;
		}

		if (_lastProcessedTime == _lastCandleTimeOne)
		{
			return;
		}

		_lastProcessedTime = _lastCandleTimeOne;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (_initialPortfolioValue == 0m && Portfolio?.CurrentValue != null)
		{
			_initialPortfolioValue = Portfolio.CurrentValue.Value;
		}

		var positionOne = GetPositionValue(Security);
		var positionTwo = GetPositionValue(SecondSecurity);

		if (positionOne <= 0m)
		{
			_lowestBuyOne = decimal.MaxValue;
		}

		if (positionOne >= 0m)
		{
			_highestSellOne = decimal.MinValue;
		}

		if (positionTwo <= 0m)
		{
			_lowestBuyTwo = decimal.MaxValue;
		}

		if (positionTwo >= 0m)
		{
			_highestSellTwo = decimal.MinValue;
		}

		var combinedExposure = Math.Abs(positionOne) + Math.Abs(positionTwo);

		if (combinedExposure < MaxOpenUnits)
		{
			EvaluateFirstSecurity(positionOne);
			EvaluateSecondSecurity(positionTwo);
		}
		else if (GetTotalProfit() > 0m)
		{
			CloseAllPositions();
		}

		if (GetTotalProfit() > ProfitTarget)
		{
			CloseAllPositions();
		}
	}

	private void EvaluateFirstSecurity(decimal position)
	{
		if (_lastCloseOne < _lowestBuyOne && position >= 0m)
		{
			ExecuteBuyOne();
		}
		else if (_lastCloseOne > _highestSellOne && position <= 0m)
		{
			ExecuteSellOne();
		}
		else if (position == 0m)
		{
			ExecuteBuyOne();
		}
	}

	private void EvaluateSecondSecurity(decimal position)
	{
		if (_lastCloseTwo < _lowestBuyTwo && position >= 0m)
		{
			ExecuteBuyTwo();
		}
		else if (_lastCloseTwo > _highestSellTwo && position <= 0m)
		{
			ExecuteSellTwo();
		}
		else if (position == 0m)
		{
			ExecuteBuyTwo();
		}
	}

	private void ExecuteBuyOne()
	{
		if (Volume <= 0m)
		{
			return;
		}

		BuyMarket(Volume);
		_lowestBuyOne = _lowestBuyOne == decimal.MaxValue ? _lastCloseOne : Math.Min(_lowestBuyOne, _lastCloseOne);
	}

	private void ExecuteSellOne()
	{
		if (Volume <= 0m)
		{
			return;
		}

		SellMarket(Volume);
		_highestSellOne = _highestSellOne == decimal.MinValue ? _lastCloseOne : Math.Max(_highestSellOne, _lastCloseOne);
	}

	private void ExecuteBuyTwo()
	{
		if (SecondVolume <= 0m)
		{
			return;
		}

		BuyMarket(SecondVolume, security: SecondSecurity);
		_lowestBuyTwo = _lowestBuyTwo == decimal.MaxValue ? _lastCloseTwo : Math.Min(_lowestBuyTwo, _lastCloseTwo);
	}

	private void ExecuteSellTwo()
	{
		if (SecondVolume <= 0m)
		{
			return;
		}

		SellMarket(SecondVolume, security: SecondSecurity);
		_highestSellTwo = _highestSellTwo == decimal.MinValue ? _lastCloseTwo : Math.Max(_highestSellTwo, _lastCloseTwo);
	}

	private decimal GetTotalProfit()
	{
		if (Portfolio?.CurrentValue == null || _initialPortfolioValue == 0m)
		{
			return 0m;
		}

		return Portfolio.CurrentValue.Value - _initialPortfolioValue;
	}

	private void CloseAllPositions()
	{
		var positionOne = GetPositionValue(Security);
		if (positionOne > 0m)
		{
			SellMarket(positionOne);
		}
		else if (positionOne < 0m)
		{
			BuyMarket(Math.Abs(positionOne));
		}

		var positionTwo = GetPositionValue(SecondSecurity);
		if (positionTwo > 0m)
		{
			SellMarket(positionTwo, security: SecondSecurity);
		}
		else if (positionTwo < 0m)
		{
			BuyMarket(Math.Abs(positionTwo), security: SecondSecurity);
		}

		_lowestBuyOne = decimal.MaxValue;
		_highestSellOne = decimal.MinValue;
		_lowestBuyTwo = decimal.MaxValue;
		_highestSellTwo = decimal.MinValue;
	}

	private decimal GetPositionValue(Security security)
	{
		return security is null ? 0m : GetPositionValue(security, Portfolio) ?? 0m;
	}
}
