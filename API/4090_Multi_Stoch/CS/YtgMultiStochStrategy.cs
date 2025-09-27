using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor ytg_Multi_Stoch.
/// Trades up to four securities using stochastic crossovers with oversold and overbought filters.
/// Applies independent stop-loss and take-profit targets expressed in pips for every instrument.
/// </summary>
public class YtgMultiStochStrategy : Strategy
{
	private readonly StrategyParam<bool> _useSymbol1;
	private readonly StrategyParam<bool> _useSymbol2;
	private readonly StrategyParam<bool> _useSymbol3;
	private readonly StrategyParam<bool> _useSymbol4;
	private readonly StrategyParam<Security> _symbol1;
	private readonly StrategyParam<Security> _symbol2;
	private readonly StrategyParam<Security> _symbol3;
	private readonly StrategyParam<Security> _symbol4;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<SymbolContext> _contexts = new();

	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;

	/// <summary>
	/// Initializes a new instance of <see cref="YtgMultiStochStrategy"/>.
	/// </summary>
	public YtgMultiStochStrategy()
	{
		_useSymbol1 = Param(nameof(UseSymbol1), true)
			.SetDisplay("Use Symbol #1", "Enable trading for the first configured security", "Instruments");

		_useSymbol2 = Param(nameof(UseSymbol2), true)
			.SetDisplay("Use Symbol #2", "Enable trading for the second configured security", "Instruments");

		_useSymbol3 = Param(nameof(UseSymbol3), true)
			.SetDisplay("Use Symbol #3", "Enable trading for the third configured security", "Instruments");

		_useSymbol4 = Param(nameof(UseSymbol4), true)
			.SetDisplay("Use Symbol #4", "Enable trading for the fourth configured security", "Instruments");

		_symbol1 = Param<Security>(nameof(Symbol1))
			.SetDisplay("Symbol #1", "Security traded in the first slot", "Instruments");

		_symbol2 = Param<Security>(nameof(Symbol2))
			.SetDisplay("Symbol #2", "Security traded in the second slot", "Instruments");

		_symbol3 = Param<Security>(nameof(Symbol3))
			.SetDisplay("Symbol #3", "Security traded in the third slot", "Instruments");

		_symbol4 = Param<Security>(nameof(Symbol4))
			.SetDisplay("Symbol #4", "Security traded in the fourth slot", "Instruments");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base volume used for market entries", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetRange(0m, 1000m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetRange(0m, 1000m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk Management");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetRange(0m, 100m)
			.SetDisplay("Oversold Level", "Stochastic threshold that enables long trades", "Signals");

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetRange(0m, 100m)
			.SetDisplay("Overbought Level", "Stochastic threshold that enables short trades", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to calculate signals", "General");
	}

	/// <summary>
	/// Enable trading for the first configured security.
	/// </summary>
	public bool UseSymbol1
	{
		get => _useSymbol1.Value;
		set => _useSymbol1.Value = value;
	}

	/// <summary>
	/// Enable trading for the second configured security.
	/// </summary>
	public bool UseSymbol2
	{
		get => _useSymbol2.Value;
		set => _useSymbol2.Value = value;
	}

	/// <summary>
	/// Enable trading for the third configured security.
	/// </summary>
	public bool UseSymbol3
	{
		get => _useSymbol3.Value;
		set => _useSymbol3.Value = value;
	}

	/// <summary>
	/// Enable trading for the fourth configured security.
	/// </summary>
	public bool UseSymbol4
	{
		get => _useSymbol4.Value;
		set => _useSymbol4.Value = value;
	}

	/// <summary>
	/// Security traded in the first slot.
	/// </summary>
	public Security Symbol1
	{
		get => _symbol1.Value;
		set => _symbol1.Value = value;
	}

	/// <summary>
	/// Security traded in the second slot.
	/// </summary>
	public Security Symbol2
	{
		get => _symbol2.Value;
		set => _symbol2.Value = value;
	}

	/// <summary>
	/// Security traded in the third slot.
	/// </summary>
	public Security Symbol3
	{
		get => _symbol3.Value;
		set => _symbol3.Value = value;
	}

	/// <summary>
	/// Security traded in the fourth slot.
	/// </summary>
	public Security Symbol4
	{
		get => _symbol4.Value;
		set => _symbol4.Value = value;
	}

	/// <summary>
	/// Base volume used when opening new market positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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
	/// Stochastic oversold threshold used to trigger long entries.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Stochastic overbought threshold used to trigger short entries.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (CandleType == null)
			yield break;

		var seen = new HashSet<Security>();

		foreach (var security in EnumerateEnabledSecurities())
		{
			if (seen.Add(security))
				yield return (security, CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_contexts.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_contexts.Clear();

		foreach (var security in EnumerateEnabledSecurities())
		{
			var stochastic = CreateStochastic();
			var context = new SymbolContext(security)
			{
				Stochastic = stochastic,
				PipSize = CalculatePipSize(security)
			};

			_contexts.Add(context);

			SubscribeCandles(CandleType, security)
				.BindEx(stochastic, (candle, value) => ProcessCandle(context, candle, value))
				.Start();
		}

		StartProtection();
	}

	private IEnumerable<Security> EnumerateEnabledSecurities()
	{
		if (UseSymbol1 && Symbol1 != null)
			yield return Symbol1;

		if (UseSymbol2 && Symbol2 != null)
			yield return Symbol2;

		if (UseSymbol3 && Symbol3 != null)
			yield return Symbol3;

		if (UseSymbol4 && Symbol4 != null)
			yield return Symbol4;
	}

	private static StochasticOscillator CreateStochastic()
	{
		return new StochasticOscillator
		{
			Length = 5,
			Smooth = 3,
			K = { Length = 3 },
			D = { Length = 3 }
		};
	}

	private void ProcessCandle(SymbolContext context, ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not StochasticOscillatorValue stochValue)
			return;

		if (stochValue.K is not decimal currentK || stochValue.D is not decimal currentD)
			return;

		var position = GetPosition(context.Security);

		if (position > 0m)
		{
			context.Direction = TradeDirection.Long;

			if (TryCloseLongByTargets(context, candle, position))
			{
				context.PreviousK = currentK;
				context.PreviousD = currentD;
				return;
			}
		}
		else if (position < 0m)
		{
			context.Direction = TradeDirection.Short;

			if (TryCloseShortByTargets(context, candle, position))
			{
				context.PreviousK = currentK;
				context.PreviousD = currentD;
				return;
			}
		}
		else
		{
			context.Direction = TradeDirection.Flat;

			if (context.StopLossPrice.HasValue || context.TakeProfitPrice.HasValue)
				context.ResetTargets();
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			context.PreviousK = currentK;
			context.PreviousD = currentD;
			return;
		}

		if (context.Direction == TradeDirection.Flat && position == 0m && context.PreviousK is decimal prevK && context.PreviousD is decimal prevD)
		{
			var buySignal = currentK < OversoldLevel && prevK < prevD && currentK > currentD;
			var sellSignal = currentK > OverboughtLevel && prevK > prevD && currentK < currentD;

			if (buySignal && TradeVolume > 0m)
			{
				BuyMarket(TradeVolume, context.Security);

				var pip = context.PipSize > 0m ? context.PipSize : CalculatePipSize(context.Security);
				context.PipSize = pip;
				context.EntryPrice = candle.ClosePrice;
				context.StopLossPrice = StopLossPips > 0m && pip > 0m ? candle.ClosePrice - StopLossPips * pip : null;
				context.TakeProfitPrice = TakeProfitPips > 0m && pip > 0m ? candle.ClosePrice + TakeProfitPips * pip : null;
				context.Direction = TradeDirection.Long;
			}
			else if (sellSignal && TradeVolume > 0m)
			{
				SellMarket(TradeVolume, context.Security);

				var pip = context.PipSize > 0m ? context.PipSize : CalculatePipSize(context.Security);
				context.PipSize = pip;
				context.EntryPrice = candle.ClosePrice;
				context.StopLossPrice = StopLossPips > 0m && pip > 0m ? candle.ClosePrice + StopLossPips * pip : null;
				context.TakeProfitPrice = TakeProfitPips > 0m && pip > 0m ? candle.ClosePrice - TakeProfitPips * pip : null;
				context.Direction = TradeDirection.Short;
			}
		}

		context.PreviousK = currentK;
		context.PreviousD = currentD;
	}

	private bool TryCloseLongByTargets(SymbolContext context, ICandleMessage candle, decimal position)
	{
		var volume = Math.Abs(position);

		if (context.StopLossPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(volume, context.Security);
			context.ResetTargets();
			return true;
		}

		if (context.TakeProfitPrice is decimal take && candle.HighPrice >= take)
		{
			SellMarket(volume, context.Security);
			context.ResetTargets();
			return true;
		}

		return false;
	}

	private bool TryCloseShortByTargets(SymbolContext context, ICandleMessage candle, decimal position)
	{
		var volume = Math.Abs(position);

		if (context.StopLossPrice is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(volume, context.Security);
			context.ResetTargets();
			return true;
		}

		if (context.TakeProfitPrice is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(volume, context.Security);
			context.ResetTargets();
			return true;
		}

		return false;
	}

	private decimal GetPosition(Security security)
	{
		return security == null ? 0m : GetPositionValue(security, Portfolio) ?? 0m;
	}

	private decimal CalculatePipSize(Security security)
	{
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0m;

		if (step <= 0m)
		{
			var decimals = security.Decimals;

			if (decimals != null)
			{
				var value = 1m;
				for (var i = 0; i < decimals.Value; i++)
					value /= 10m;

				return value;
			}

			return 0.0001m;
		}

		var digits = 0;
		var current = step;

		while (current < 1m && digits < 10)
		{
			current *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
			return step * 10m;

		return step;
	}

	private enum TradeDirection
	{
		Flat,
		Long,
		Short
	}

	private sealed class SymbolContext
	{
		public SymbolContext(Security security)
		{
			Security = security;
		}

		public Security Security { get; }
		public StochasticOscillator Stochastic { get; set; }
		public decimal PipSize { get; set; }
		public decimal? PreviousK { get; set; }
		public decimal? PreviousD { get; set; }
		public decimal? StopLossPrice { get; set; }
		public decimal? TakeProfitPrice { get; set; }
		public decimal? EntryPrice { get; set; }
		public TradeDirection Direction { get; set; } = TradeDirection.Flat;

		public void ResetTargets()
		{
			StopLossPrice = null;
			TakeProfitPrice = null;
			EntryPrice = null;
			Direction = TradeDirection.Flat;
		}
	}
}
