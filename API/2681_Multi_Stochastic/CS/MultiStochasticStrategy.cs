namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// StockSharp port of the Multi Stochastic MT5 expert advisor.
/// </summary>
public class MultiStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private readonly StrategyParam<bool> _useSymbol1;
	private readonly StrategyParam<bool> _useSymbol2;
	private readonly StrategyParam<bool> _useSymbol3;
	private readonly StrategyParam<bool> _useSymbol4;

	private readonly StrategyParam<Security> _symbol1;
	private readonly StrategyParam<Security> _symbol2;
	private readonly StrategyParam<Security> _symbol3;
	private readonly StrategyParam<Security> _symbol4;

	private Security? _resolvedSymbol1;
	private Security? _resolvedSymbol2;
	private Security? _resolvedSymbol3;
	private Security? _resolvedSymbol4;

	private StochasticOscillator? _stochastic1;
	private StochasticOscillator? _stochastic2;
	private StochasticOscillator? _stochastic3;
	private StochasticOscillator? _stochastic4;

	private decimal? _prevK1;
	private decimal? _prevD1;
	private decimal? _prevK2;
	private decimal? _prevD2;
	private decimal? _prevK3;
	private decimal? _prevD3;
	private decimal? _prevK4;
	private decimal? _prevD4;

	private decimal? _stopPrice1;
	private decimal? _takePrice1;
	private decimal? _stopPrice2;
	private decimal? _takePrice2;
	private decimal? _stopPrice3;
	private decimal? _takePrice3;
	private decimal? _stopPrice4;
	private decimal? _takePrice4;

	private decimal _pipValue1;
	private decimal _pipValue2;
	private decimal _pipValue3;
	private decimal _pipValue4;

	/// <summary>
	/// Candle series used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base length for the Stochastic Oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// %K smoothing period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Oversold threshold for long signals.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought threshold for short signals.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable trading for the first symbol slot.
	/// </summary>
	public bool UseSymbol1
	{
		get => _useSymbol1.Value;
		set => _useSymbol1.Value = value;
	}

	/// <summary>
	/// Enable trading for the second symbol slot.
	/// </summary>
	public bool UseSymbol2
	{
		get => _useSymbol2.Value;
		set => _useSymbol2.Value = value;
	}

	/// <summary>
	/// Enable trading for the third symbol slot.
	/// </summary>
	public bool UseSymbol3
	{
		get => _useSymbol3.Value;
		set => _useSymbol3.Value = value;
	}

	/// <summary>
	/// Enable trading for the fourth symbol slot.
	/// </summary>
	public bool UseSymbol4
	{
		get => _useSymbol4.Value;
		set => _useSymbol4.Value = value;
	}

	/// <summary>
	/// Security used in the first slot.
	/// </summary>
	public Security? Symbol1
	{
		get => _symbol1.Value;
		set => _symbol1.Value = value;
	}

	/// <summary>
	/// Security used in the second slot.
	/// </summary>
	public Security? Symbol2
	{
		get => _symbol2.Value;
		set => _symbol2.Value = value;
	}

	/// <summary>
	/// Security used in the third slot.
	/// </summary>
	public Security? Symbol3
	{
		get => _symbol3.Value;
		set => _symbol3.Value = value;
	}

	/// <summary>
	/// Security used in the fourth slot.
	/// </summary>
	public Security? Symbol4
	{
		get => _symbol4.Value;
		set => _symbol4.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MultiStochasticStrategy"/>.
	/// </summary>
	public MultiStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame applied to every symbol", "Data");

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Base period for Stochastic", "Indicators");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Smoothing period for %K", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Smoothing period for %D", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Oversold Level", "Threshold for long entries", "Signals");

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetGreaterThanZero()
			.SetDisplay("Overbought Level", "Threshold for short entries", "Signals");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 100m, 5m);

		_useSymbol1 = Param(nameof(UseSymbol1), true)
			.SetDisplay("Use symbol #1", "Enable trading for the first slot", "Symbols");

		_useSymbol2 = Param(nameof(UseSymbol2), true)
			.SetDisplay("Use symbol #2", "Enable trading for the second slot", "Symbols");

		_useSymbol3 = Param(nameof(UseSymbol3), true)
			.SetDisplay("Use symbol #3", "Enable trading for the third slot", "Symbols");

		_useSymbol4 = Param(nameof(UseSymbol4), true)
			.SetDisplay("Use symbol #4", "Enable trading for the fourth slot", "Symbols");

		_symbol1 = Param<Security>(nameof(Symbol1))
			.SetDisplay("Symbol #1", "Security assigned to the first slot", "Symbols");

		_symbol2 = Param<Security>(nameof(Symbol2))
			.SetDisplay("Symbol #2", "Security assigned to the second slot", "Symbols");

		_symbol3 = Param<Security>(nameof(Symbol3))
			.SetDisplay("Symbol #3", "Security assigned to the third slot", "Symbols");

		_symbol4 = Param<Security>(nameof(Symbol4))
			.SetDisplay("Symbol #4", "Security assigned to the fourth slot", "Symbols");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var symbol1 = UseSymbol1 ? Symbol1 ?? Security : null;
		var symbol2 = UseSymbol2 ? Symbol2 : null;
		var symbol3 = UseSymbol3 ? Symbol3 : null;
		var symbol4 = UseSymbol4 ? Symbol4 : null;

		if (symbol1 != null)
			yield return (symbol1, CandleType);

		if (symbol2 != null)
			yield return (symbol2, CandleType);

		if (symbol3 != null)
			yield return (symbol3, CandleType);

		if (symbol4 != null)
			yield return (symbol4, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevK1 = _prevD1 = null;
		_prevK2 = _prevD2 = null;
		_prevK3 = _prevD3 = null;
		_prevK4 = _prevD4 = null;

		_stopPrice1 = _takePrice1 = null;
		_stopPrice2 = _takePrice2 = null;
		_stopPrice3 = _takePrice3 = null;
		_stopPrice4 = _takePrice4 = null;

		_pipValue1 = _pipValue2 = _pipValue3 = _pipValue4 = 0m;
		_resolvedSymbol1 = _resolvedSymbol2 = _resolvedSymbol3 = _resolvedSymbol4 = null;
		_stochastic1 = _stochastic2 = _stochastic3 = _stochastic4 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_resolvedSymbol1 = UseSymbol1 ? Symbol1 ?? Security : null;
		_resolvedSymbol2 = UseSymbol2 ? Symbol2 : null;
		_resolvedSymbol3 = UseSymbol3 ? Symbol3 : null;
		_resolvedSymbol4 = UseSymbol4 ? Symbol4 : null;

		StartForSymbol(_resolvedSymbol1, ref _stochastic1, ref _pipValue1, ProcessSymbol1);
		StartForSymbol(_resolvedSymbol2, ref _stochastic2, ref _pipValue2, ProcessSymbol2);
		StartForSymbol(_resolvedSymbol3, ref _stochastic3, ref _pipValue3, ProcessSymbol3);
		StartForSymbol(_resolvedSymbol4, ref _stochastic4, ref _pipValue4, ProcessSymbol4);
	}

	private void StartForSymbol(Security? security, ref StochasticOscillator? indicator, ref decimal pipValue, Action<ICandleMessage, IIndicatorValue> handler)
	{
		if (security == null)
			return;

		indicator = CreateStochastic();
		pipValue = CalculatePipValue(security);

		if (pipValue <= 0m)
			LogWarning($"Unable to detect pip size for {security.Id}. Protective levels will be disabled.");

		var subscription = SubscribeCandles(CandleType, security: security);
		subscription
			.BindEx(indicator, handler)
			.Start();
	}

	private StochasticOscillator CreateStochastic()
	{
		return new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticKPeriod },
			D = { Length = StochasticDPeriod }
		};
	}

	private void ProcessSymbol1(ICandleMessage candle, IIndicatorValue stochValue)
	{
		HandleSymbol(candle, stochValue, _resolvedSymbol1, ref _prevK1, ref _prevD1, ref _stopPrice1, ref _takePrice1, _pipValue1);
	}

	private void ProcessSymbol2(ICandleMessage candle, IIndicatorValue stochValue)
	{
		HandleSymbol(candle, stochValue, _resolvedSymbol2, ref _prevK2, ref _prevD2, ref _stopPrice2, ref _takePrice2, _pipValue2);
	}

	private void ProcessSymbol3(ICandleMessage candle, IIndicatorValue stochValue)
	{
		HandleSymbol(candle, stochValue, _resolvedSymbol3, ref _prevK3, ref _prevD3, ref _stopPrice3, ref _takePrice3, _pipValue3);
	}

	private void ProcessSymbol4(ICandleMessage candle, IIndicatorValue stochValue)
	{
		HandleSymbol(candle, stochValue, _resolvedSymbol4, ref _prevK4, ref _prevD4, ref _stopPrice4, ref _takePrice4, _pipValue4);
	}

	private void HandleSymbol(
		ICandleMessage candle,
		IIndicatorValue stochValue,
		Security? security,
		ref decimal? prevK,
		ref decimal? prevD,
		ref decimal? stopPrice,
		ref decimal? takePrice,
		decimal pipValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal)
			return;

		if (security == null)
			return;

		var typed = (StochasticOscillatorValue)stochValue;
		var currentK = typed.K;
		var currentD = typed.D;

		var position = GetPositionVolume(security);

		if (ManageRisk(candle, security, ref stopPrice, ref takePrice, position))
		{
			prevK = currentK;
			prevD = currentD;
			return;
		}

		if (prevK is null || prevD is null)
		{
			prevK = currentK;
			prevD = currentD;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			prevK = currentK;
			prevD = currentD;
			return;
		}

		var longSignal = currentK < OversoldLevel && prevK.Value < prevD.Value && currentK > currentD;
		var shortSignal = currentK > OverboughtLevel && prevK.Value > prevD.Value && currentK < currentD;

		position = GetPositionVolume(security);

		if (position == 0m)
		{
			var volume = Volume;

			if (longSignal && volume > 0m)
			{
				// Enter long position after bullish crossover in oversold zone.
				BuyMarket(volume, security);
				stopPrice = StopLossPips > 0m && pipValue > 0m ? candle.ClosePrice - StopLossPips * pipValue : null;
				takePrice = TakeProfitPips > 0m && pipValue > 0m ? candle.ClosePrice + TakeProfitPips * pipValue : null;
			}
			else if (shortSignal && volume > 0m)
			{
				// Enter short position after bearish crossover in overbought zone.
				SellMarket(volume, security);
				stopPrice = StopLossPips > 0m && pipValue > 0m ? candle.ClosePrice + StopLossPips * pipValue : null;
				takePrice = TakeProfitPips > 0m && pipValue > 0m ? candle.ClosePrice - TakeProfitPips * pipValue : null;
			}
		}

		prevK = currentK;
		prevD = currentD;
	}

	private bool ManageRisk(ICandleMessage candle, Security security, ref decimal? stopPrice, ref decimal? takePrice, decimal position)
	{
		if (position > 0m)
		{
			// Close long positions on protective levels.
			if (stopPrice.HasValue && candle.LowPrice <= stopPrice.Value)
			{
				SellMarket(position, security);
				stopPrice = takePrice = null;
				return true;
			}

			if (takePrice.HasValue && candle.HighPrice >= takePrice.Value)
			{
				SellMarket(position, security);
				stopPrice = takePrice = null;
				return true;
			}
		}
		else if (position < 0m)
		{
			// Close short positions on protective levels.
			var volume = Math.Abs(position);

			if (stopPrice.HasValue && candle.HighPrice >= stopPrice.Value)
			{
				BuyMarket(volume, security);
				stopPrice = takePrice = null;
				return true;
			}

			if (takePrice.HasValue && candle.LowPrice <= takePrice.Value)
			{
				BuyMarket(volume, security);
				stopPrice = takePrice = null;
				return true;
			}
		}
		else
		{
			// Reset protective levels when no position is active.
			stopPrice = takePrice = null;
		}

		return false;
	}

	private decimal GetPositionVolume(Security security)
	{
		return GetPositionValue(security, Portfolio) ?? 0m;
	}

	private decimal CalculatePipValue(Security security)
	{
		var step = security.PriceStep ?? 0m;

		if (step <= 0m)
			return 0m;

		var decimals = security.Decimals ?? 0;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;

		return step * multiplier;
	}
}
