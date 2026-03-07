// SoccerClubsArbitrageStrategy.cs
// -----------------------------------------------------------------------------
// Two share classes of the same soccer club (pair length = 2).
// Long cheaper share, short expensive when relative premium > EntryThresh;
// exit when premium shrinks below ExitThresh.
// Uses candle-based price comparison between two securities.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Arbitrage strategy for two share classes of the same soccer club.
/// </summary>
public class SoccerClubsArbitrageStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<decimal> _entry;
	private readonly StrategyParam<decimal> _exit;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Second security identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Premium threshold to enter a position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entry.Value;
		set => _entry.Value = value;
	}

	/// <summary>
	/// Premium threshold to exit a position.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exit.Value;
		set => _exit.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private Security _secondSecurity;
	private decimal _priceA;
	private decimal _priceB;
	private bool _primaryUpdated;
	private bool _secondUpdated;
	private int _cooldownRemaining;

	public SoccerClubsArbitrageStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Second Security Id", "Identifier of the second security", "General");

		_entry = Param(nameof(EntryThreshold), 0.005m)
			.SetDisplay("Entry Threshold", "Premium difference to open position", "Parameters");

		_exit = Param(nameof(ExitThreshold), 0.001m)
			.SetDisplay("Exit Threshold", "Premium difference to close position", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 5)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_secondSecurity = null;
		_priceA = 0;
		_priceB = 0;
		_primaryUpdated = false;
		_secondUpdated = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Second security identifier is not specified.");

		_secondSecurity = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };

		// Subscribe to primary security candles
		var primarySub = SubscribeCandles(CandleType, security: Security);
		primarySub
			.Bind(ProcessPrimaryCandle)
			.Start();

		// Subscribe to second security candles
		var secondSub = SubscribeCandles(CandleType, security: _secondSecurity);
		secondSub
			.Bind(ProcessSecondCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_priceA = candle.ClosePrice;
		_primaryUpdated = true;
		TryEvaluate();
	}

	private void ProcessSecondCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_priceB = candle.ClosePrice;
		_secondUpdated = true;
		TryEvaluate();
	}

	private void TryEvaluate()
	{
		if (!_primaryUpdated || !_secondUpdated)
			return;

		if (_priceA <= 0 || _priceB <= 0)
			return;

		_primaryUpdated = false;
		_secondUpdated = false;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var premium = _priceA / _priceB - 1m;

		var primaryPos = GetPositionValue(Security, Portfolio) ?? 0m;

		// Exit when premium shrinks below exit threshold
		if (Math.Abs(premium) < ExitThreshold && primaryPos != 0)
		{
			Flatten(primaryPos);
			_cooldownRemaining = CooldownBars;
			return;
		}

		// A is overpriced relative to B -> short A, long B
		if (premium > EntryThreshold && primaryPos >= 0)
		{
			if (primaryPos > 0)
				Flatten(primaryPos);

			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// B is overpriced relative to A -> long A, short B
		else if (premium < -EntryThreshold && primaryPos <= 0)
		{
			if (primaryPos < 0)
				Flatten(primaryPos);

			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}

	private void Flatten(decimal primaryPos)
	{
		if (primaryPos > 0)
			SellMarket(primaryPos);
		else if (primaryPos < 0)
			BuyMarket(Math.Abs(primaryPos));
	}
}
