using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Consistent momentum strategy that trades the primary instrument when both medium-term and long-term momentum are aligned versus a benchmark.
/// </summary>
public class ConsistentMomentumStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _mediumMomentumLength;
	private readonly StrategyParam<int> _longMomentumLength;
	private readonly StrategyParam<decimal> _entryMargin;
	private readonly StrategyParam<decimal> _exitMargin;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _primaryMediumMomentum = null!;
	private RateOfChange _primaryLongMomentum = null!;
	private RateOfChange _benchmarkMediumMomentum = null!;
	private RateOfChange _benchmarkLongMomentum = null!;
	private bool _primaryUpdated;
	private bool _benchmarkUpdated;
	private decimal _primaryMediumValue;
	private decimal _primaryLongValue;
	private decimal _benchmarkMediumValue;
	private decimal _benchmarkLongValue;
	private int _cooldownRemaining;

	/// <summary>
	/// Benchmark security identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Medium-term momentum length.
	/// </summary>
	public int MediumMomentumLength
	{
		get => _mediumMomentumLength.Value;
		set => _mediumMomentumLength.Value = value;
	}

	/// <summary>
	/// Long-term momentum length.
	/// </summary>
	public int LongMomentumLength
	{
		get => _longMomentumLength.Value;
		set => _longMomentumLength.Value = value;
	}

	/// <summary>
	/// Minimum relative edge required to open a position.
	/// </summary>
	public decimal EntryMargin
	{
		get => _entryMargin.Value;
		set => _entryMargin.Value = value;
	}

	/// <summary>
	/// Relative edge threshold used to close a position.
	/// </summary>
	public decimal ExitMargin
	{
		get => _exitMargin.Value;
		set => _exitMargin.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type used for both instruments.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ConsistentMomentumStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_mediumMomentumLength = Param(nameof(MediumMomentumLength), 18)
			.SetRange(5, 80)
			.SetDisplay("Medium Momentum Length", "Medium-term momentum length", "Indicators");

		_longMomentumLength = Param(nameof(LongMomentumLength), 60)
			.SetRange(20, 200)
			.SetDisplay("Long Momentum Length", "Long-term momentum length", "Indicators");

		_entryMargin = Param(nameof(EntryMargin), 1.5m)
			.SetRange(0.1m, 20m)
			.SetDisplay("Entry Margin", "Minimum relative edge required to open a position", "Signals");

		_exitMargin = Param(nameof(ExitMargin), 0.4m)
			.SetRange(0m, 10m)
			.SetDisplay("Exit Margin", "Relative edge threshold used to close a position", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetRange(0, 100)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for both instruments", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (!Security2Id.IsEmpty())
			yield return (new Security { Id = Security2Id }, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_benchmark = null!;
		_primaryMediumMomentum = null!;
		_primaryLongMomentum = null!;
		_benchmarkMediumMomentum = null!;
		_benchmarkLongMomentum = null!;
		_primaryUpdated = false;
		_benchmarkUpdated = false;
		_primaryMediumValue = 0m;
		_primaryLongValue = 0m;
		_benchmarkMediumValue = 0m;
		_benchmarkLongValue = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Benchmark security identifier is not specified.");

		_benchmark = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };
		_primaryMediumMomentum = new RateOfChange { Length = MediumMomentumLength };
		_primaryLongMomentum = new RateOfChange { Length = LongMomentumLength };
		_benchmarkMediumMomentum = new RateOfChange { Length = MediumMomentumLength };
		_benchmarkLongMomentum = new RateOfChange { Length = LongMomentumLength };

		var primarySubscription = SubscribeCandles(CandleType, security: Security);
		var benchmarkSubscription = SubscribeCandles(CandleType, security: _benchmark);

		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		benchmarkSubscription
			.Bind(ProcessBenchmarkCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawCandles(area, benchmarkSubscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent));
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var mediumValue = _primaryMediumMomentum.Process(candle);
		var longValue = _primaryLongMomentum.Process(candle);

		if (!mediumValue.IsEmpty && !longValue.IsEmpty && _primaryMediumMomentum.IsFormed && _primaryLongMomentum.IsFormed)
		{
			_primaryMediumValue = mediumValue.ToDecimal();
			_primaryLongValue = longValue.ToDecimal();
			_primaryUpdated = true;
			TryProcessSignal();
		}
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var mediumValue = _benchmarkMediumMomentum.Process(candle);
		var longValue = _benchmarkLongMomentum.Process(candle);

		if (!mediumValue.IsEmpty && !longValue.IsEmpty && _benchmarkMediumMomentum.IsFormed && _benchmarkLongMomentum.IsFormed)
		{
			_benchmarkMediumValue = mediumValue.ToDecimal();
			_benchmarkLongValue = longValue.ToDecimal();
			_benchmarkUpdated = true;
			TryProcessSignal();
		}
	}

	private void TryProcessSignal()
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var mediumEdge = _primaryMediumValue - _benchmarkMediumValue;
		var longEdge = _primaryLongValue - _benchmarkLongValue;
		var bullishConsistent = mediumEdge >= EntryMargin && longEdge >= EntryMargin;
		var bearishConsistent = mediumEdge <= -EntryMargin && longEdge <= -EntryMargin;
		var bullishExit = mediumEdge <= ExitMargin || longEdge <= ExitMargin;
		var bearishExit = mediumEdge >= -ExitMargin || longEdge >= -ExitMargin;

		if (_cooldownRemaining == 0 && Position == 0)
		{
			if (bullishConsistent)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (bearishConsistent)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position > 0 && bullishExit)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && bearishExit)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
