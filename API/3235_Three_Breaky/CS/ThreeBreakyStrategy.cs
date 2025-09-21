
using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ThreeBreaky strategy converted from the MetaTrader expert advisor "ThreeBreaky_v1.mq4".
/// The system combines three independent breakout modules that fire on strong candle ranges,
/// Ichimoku cloud flips, and exceptional single bar bodies while using a common Parabolic SAR exit.
/// </summary>
public class ThreeBreakyStrategy : Strategy
{
	private sealed class SystemState
	{
		public bool HasPosition;
		public bool IsLong;
		public decimal Volume;
		public decimal? EntryPrice;
		public DateTimeOffset? LastLongSignal;
		public DateTimeOffset? LastShortSignal;
	}

	private readonly StrategyParam<bool> _useSystem1;
	private readonly StrategyParam<bool> _useSystem2;
	private readonly StrategyParam<bool> _useSystem3;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atrAverage = null!;
	private ParabolicSar _parabolic = null!;
	private Ichimoku _ichimoku = null!;

	private readonly Queue<decimal> _bodyHistory = new();

	private ICandleMessage? _previousCandle;
	private ICandleMessage? _secondPreviousCandle;
	private decimal? _previousAtrAverage;
	private decimal? _previousSpanA;
	private decimal? _previousSpanB;
	private decimal? _previousSar;
	private decimal? _prePreviousSar;

	private decimal _pipSize;

	private readonly SystemState _system1 = new();
	private readonly SystemState _system2 = new();
	private readonly SystemState _system3 = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ThreeBreakyStrategy"/> class.
	/// </summary>
	public ThreeBreakyStrategy()
	{
		_useSystem1 = Param(nameof(UseSystem1), true)
			.SetDisplay("Use System 1", "Enable the ATR expansion breakout module", "General")
			.SetCanOptimize();

		_useSystem2 = Param(nameof(UseSystem2), true)
			.SetDisplay("Use System 2", "Enable the Ichimoku cloud flip module", "General")
			.SetCanOptimize();

		_useSystem3 = Param(nameof(UseSystem3), true)
			.SetDisplay("Use System 3", "Enable the large body exhaustion module", "General")
			.SetCanOptimize();

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Default volume used for every entry", "Trading")
			.SetCanOptimize();

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetRange(0m, 1000m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk Management")
			.SetCanOptimize();

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetRange(0m, 2000m)
			.SetDisplay("Take Profit (pips)", "Optional take-profit distance expressed in pips", "Risk Management")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for signal generation", "Data");
	}

	/// <summary>
	/// Enable or disable the ATR expansion breakout module.
	/// </summary>
	public bool UseSystem1
	{
		get => _useSystem1.Value;
		set => _useSystem1.Value = value;
	}

	/// <summary>
	/// Enable or disable the Ichimoku cloud flip module.
	/// </summary>
	public bool UseSystem2
	{
		get => _useSystem2.Value;
		set => _useSystem2.Value = value;
	}

	/// <summary>
	/// Enable or disable the large body exhaustion module.
	/// </summary>
	public bool UseSystem3
	{
		get => _useSystem3.Value;
		set => _useSystem3.Value = value;
	}

	/// <summary>
	/// Volume applied to every market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips. A value of zero disables the target.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Timeframe used to build working candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atrAverage = new AverageTrueRange { Length = 72 };
		_parabolic = new ParabolicSar
		{
			AccelerationFactor = 0.005m,
			MaximumAccelerationFactor = 0.2m
		};
		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = 9 },
			Kijun = { Length = 26 },
			SenkouB = { Length = 52 }
		};

		_pipSize = ResolvePipSize();

		_bodyHistory.Clear();
		_previousCandle = null;
		_secondPreviousCandle = null;
		_previousAtrAverage = null;
		_previousSpanA = null;
		_previousSpanB = null;
		_previousSar = null;
		_prePreviousSar = null;

		ResetState(_system1);
		ResetState(_system2);
		ResetState(_system3);
		_system1.LastLongSignal = null;
		_system1.LastShortSignal = null;
		_system2.LastLongSignal = null;
		_system2.LastShortSignal = null;
		_system3.LastLongSignal = null;
		_system3.LastShortSignal = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrAverageValue = _atrAverage.Process(candle).ToNullableDecimal();
		var sarValue = _parabolic.Process(candle).ToNullableDecimal();
		var ichimokuValue = (IchimokuValue)_ichimoku.Process(candle);
		var spanAValue = ichimokuValue.SenkouA as decimal?;
		var spanBValue = ichimokuValue.SenkouB as decimal?;

		if (_previousCandle is null)
		{
			_previousCandle = candle;
			_previousAtrAverage = atrAverageValue;
			_previousSpanA = spanAValue;
			_previousSpanB = spanBValue;
			_previousSar = sarValue;
			return;
		}

		var previous = _previousCandle;
		var beforePrevious = _secondPreviousCandle;
		var previousRange = Math.Abs(previous.HighPrice - previous.LowPrice);
		var previousBody = Math.Abs(previous.ClosePrice - previous.OpenPrice);
		var averageRange = _previousAtrAverage;
		var spanA = _previousSpanA;
		var spanB = _previousSpanB;
		var sar1 = _previousSar;
		var sar2 = _prePreviousSar;

		var stopOffset = GetPriceOffset(StopLossPips);
		var takeOffset = GetPriceOffset(TakeProfitPips);

		if (averageRange is null || spanA is null || spanB is null)
			goto UpdateState;

		if (stopOffset > 0m || takeOffset > 0m)
		{
			ApplyRiskManagement(_system1, previous, stopOffset, takeOffset);
			ApplyRiskManagement(_system2, previous, stopOffset, takeOffset);
			ApplyRiskManagement(_system3, previous, stopOffset, takeOffset);
		}

		if (beforePrevious != null && sar1 is decimal sarOne && sar2 is decimal sarTwo)
		{
			var sarCrossDown = beforePrevious.ClosePrice > sarTwo && previous.ClosePrice < sarOne;
			var sarCrossUp = beforePrevious.ClosePrice < sarTwo && previous.ClosePrice > sarOne;

			HandleSarExit(_system1, sarCrossDown, sarCrossUp);
			HandleSarExit(_system2, sarCrossDown, sarCrossUp);
			HandleSarExit(_system3, sarCrossDown, sarCrossUp);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			goto UpdateState;

		var candleOpenTime = previous.OpenTime;

		if (OrderVolume > 0m)
		{
			if (UseSystem1 && !_system1.HasPosition)
			{
				var atrThreshold = averageRange.Value * 4m;
				if (previous.ClosePrice > previous.OpenPrice && previousRange > atrThreshold &&
					(_system1.LastLongSignal is null || _system1.LastLongSignal.Value < candleOpenTime))
				{
					OpenLong(_system1, previous.ClosePrice, candleOpenTime);
				}
				else if (previous.ClosePrice < previous.OpenPrice && previousRange > atrThreshold &&
					(_system1.LastShortSignal is null || _system1.LastShortSignal.Value < candleOpenTime))
				{
					OpenShort(_system1, previous.ClosePrice, candleOpenTime);
				}
			}

			if (UseSystem2 && !_system2.HasPosition && beforePrevious != null)
			{
				var crossedAbove = beforePrevious.ClosePrice < spanA && beforePrevious.ClosePrice < spanB &&
					previous.ClosePrice > spanA && previous.ClosePrice > spanB;
				var crossedBelow = beforePrevious.ClosePrice > spanA && beforePrevious.ClosePrice > spanB &&
					previous.ClosePrice < spanA && previous.ClosePrice < spanB;

				if (crossedAbove && (_system2.LastLongSignal is null || _system2.LastLongSignal.Value < candleOpenTime))
				{
					OpenLong(_system2, previous.ClosePrice, candleOpenTime);
				}
				else if (crossedBelow && (_system2.LastShortSignal is null || _system2.LastShortSignal.Value < candleOpenTime))
				{
					OpenShort(_system2, previous.ClosePrice, candleOpenTime);
				}
			}

			if (UseSystem3 && !_system3.HasPosition)
			{
				var maxBody = GetMaxBody();

				if (maxBody > 0m)
				{
					var buySignal = previous.ClosePrice > previous.OpenPrice && previousBody > maxBody * 3m &&
						(_system3.LastLongSignal is null || _system3.LastLongSignal.Value < candleOpenTime);
					var sellSignal = previous.ClosePrice < previous.OpenPrice && previousBody > maxBody * 3m &&
						(_system3.LastShortSignal is null || _system3.LastShortSignal.Value < candleOpenTime);

					if (buySignal)
						OpenLong(_system3, previous.ClosePrice, candleOpenTime);
					else if (sellSignal)
						OpenShort(_system3, previous.ClosePrice, candleOpenTime);
				}
			}
		}

UpdateState:
		if (_bodyHistory.Count == 20)
			_bodyHistory.Dequeue();

		_bodyHistory.Enqueue(previousBody);

		_secondPreviousCandle = previous;
		_previousCandle = candle;
		_prePreviousSar = _previousSar;
		_previousSar = sarValue;
		_previousAtrAverage = atrAverageValue;
		_previousSpanA = spanAValue;
		_previousSpanB = spanBValue;
	}

	private void ApplyRiskManagement(SystemState state, ICandleMessage candle, decimal stopOffset, decimal takeOffset)
	{
		if (!state.HasPosition || state.EntryPrice is not decimal entryPrice || state.Volume <= 0m)
			return;

		if (state.IsLong)
		{
			var stopPrice = stopOffset > 0m ? entryPrice - stopOffset : (decimal?)null;
			var takePrice = takeOffset > 0m ? entryPrice + takeOffset : (decimal?)null;

			if (stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(state.Volume);
				ResetState(state);
				return;
			}

			if (takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(state.Volume);
				ResetState(state);
			}
		}
		else
		{
			var stopPrice = stopOffset > 0m ? entryPrice + stopOffset : (decimal?)null;
			var takePrice = takeOffset > 0m ? entryPrice - takeOffset : (decimal?)null;

			if (stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(state.Volume);
				ResetState(state);
				return;
			}

			if (takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(state.Volume);
				ResetState(state);
			}
		}
	}

	private void HandleSarExit(SystemState state, bool crossDown, bool crossUp)
	{
		if (!state.HasPosition || state.Volume <= 0m)
			return;

		if (state.IsLong && crossDown)
		{
			SellMarket(state.Volume);
			ResetState(state);
		}
		else if (!state.IsLong && crossUp)
		{
			BuyMarket(state.Volume);
			ResetState(state);
		}
	}

	private void OpenLong(SystemState state, decimal referencePrice, DateTimeOffset signalTime)
	{
		var order = BuyMarket(OrderVolume);
		if (order is null)
			return;

		state.HasPosition = true;
		state.IsLong = true;
		state.Volume = OrderVolume;
		state.EntryPrice = referencePrice;
		state.LastLongSignal = signalTime;
	}

	private void OpenShort(SystemState state, decimal referencePrice, DateTimeOffset signalTime)
	{
		var order = SellMarket(OrderVolume);
		if (order is null)
			return;

		state.HasPosition = true;
		state.IsLong = false;
		state.Volume = OrderVolume;
		state.EntryPrice = referencePrice;
		state.LastShortSignal = signalTime;
	}

	private void ResetState(SystemState state)
	{
		state.HasPosition = false;
		state.IsLong = false;
		state.Volume = 0m;
		state.EntryPrice = null;
	}

	private decimal GetMaxBody()
	{
		var max = 0m;
		foreach (var value in _bodyHistory)
		{
			if (value > max)
				max = value;
		}

		return max;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (pips <= 0m || _pipSize <= 0m)
			return 0m;

		return pips * _pipSize;
	}

	private decimal ResolvePipSize()
	{
		if (Security?.Step is not decimal step || step <= 0m)
			return 0m;

		if (step == 0.00001m)
			return 0.0001m;

		if (step == 0.001m)
			return 0.01m;

		return step;
	}
}
