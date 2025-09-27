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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Single-shot momentum strategy converted from the "_HPCS_Inter5" MetaTrader script.
/// Places one market buy when the close price from five bars ago exceeds the most recent close.
/// </summary>
public class HpcsInter5Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _tradeVolume;

	private readonly decimal?[] _recentCloses = new decimal?[6];
	private decimal _pipSize;
	private bool _tradePlaced;

	/// <summary>
	/// Candle type used to evaluate the closing prices.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader-style pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader-style pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trade volume submitted with the market entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HpcsInter5Strategy"/> class.
	/// </summary>
	public HpcsInter5Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for the close comparison", "General");

		_stopLossPips = Param(nameof(StopLossPips), 10)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10)
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk Management");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume submitted with the market entry", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_recentCloses, 0, _recentCloses.Length);
		_pipSize = 0m;
		_tradePlaced = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (Security is null)
			throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		base.OnStarted(time);

		InitializePipSize();
		Volume = TradeVolume;

		var stopLoss = StopLossPips > 0 && _pipSize > 0m
			? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute)
			: null;

		var takeProfit = TakeProfitPips > 0 && _pipSize > 0m
			? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute)
			: null;

		if (stopLoss != null || takeProfit != null)
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ShiftCloses(candle.ClosePrice);

		if (_tradePlaced)
			return;

		if (_recentCloses[1] is not decimal lastClose || _recentCloses[5] is not decimal olderClose)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (olderClose <= lastClose)
			return;

		// Enter long once when the five-bars-ago close beats the latest close.
		BuyMarket();
		_tradePlaced = true;
	}

	private void ShiftCloses(decimal close)
	{
		for (var i = _recentCloses.Length - 1; i > 0; i--)
			_recentCloses[i] = _recentCloses[i - 1];

		_recentCloses[0] = close;
	}

	private void InitializePipSize()
	{
		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
			step = Security.Step;

		if (step <= 0m)
		{
			_pipSize = 0m;
			return;
		}

		var pipFactor = Security.Decimals is 3 or 5 ? 10m : 1m;
		_pipSize = step * pipFactor;
	}
}

