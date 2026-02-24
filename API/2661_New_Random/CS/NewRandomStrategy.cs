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
/// Randomized entry strategy that mimics the MetaTrader "New Random" expert.
/// </summary>
public class NewRandomStrategy : Strategy
{
	/// <summary>
	/// Available direction selection modes.
	/// </summary>
	public enum RandomModes
	{
		/// <summary>Use a pseudo random generator for every entry decision.</summary>
		Generator,
		/// <summary>Alternate buy-sell-buy.</summary>
		BuySellBuy,
		/// <summary>Alternate sell-buy-sell.</summary>
		SellBuySell
	}

	private readonly StrategyParam<RandomModes> _mode;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private Random _random;
	private Sides? _sequenceLastSide;
	private Sides? _positionSide;
	private decimal _entryPrice;
	private int _candleCount;

	/// <summary>Direction selection mode.</summary>
	public RandomModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>Stop loss in price steps.</summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>Take profit in price steps.</summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>Candle type.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NewRandomStrategy()
	{
		_mode = Param(nameof(Mode), RandomModes.Generator)
			.SetDisplay("Random Mode", "Direction selection mode", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pts)", "Stop loss in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pts)", "Take profit in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_random = null;
		_sequenceLastSide = null;
		_positionSide = null;
		_entryPrice = 0m;
		_candleCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_random = Mode == RandomModes.Generator ? new Random(42) : null;

		_sequenceLastSide = Mode switch
		{
			RandomModes.BuySellBuy => Sides.Sell,
			RandomModes.SellBuySell => Sides.Buy,
			_ => null
		};

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

		_candleCount++;
		if (_candleCount < 3)
			return;

		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;
		var price = candle.ClosePrice;

		// Check SL/TP for current position
		if (Position != 0 && _entryPrice > 0)
		{
			var hit = false;

			if (_positionSide == Sides.Buy)
			{
				if (stopDistance > 0 && candle.LowPrice <= _entryPrice - stopDistance)
					hit = true;
				if (takeDistance > 0 && candle.HighPrice >= _entryPrice + takeDistance)
					hit = true;
			}
			else if (_positionSide == Sides.Sell)
			{
				if (stopDistance > 0 && candle.HighPrice >= _entryPrice + stopDistance)
					hit = true;
				if (takeDistance > 0 && candle.LowPrice <= _entryPrice - takeDistance)
					hit = true;
			}

			if (hit)
			{
				if (Position > 0)
					SellMarket(Position);
				else if (Position < 0)
					BuyMarket(Math.Abs(Position));

				_positionSide = null;
				_entryPrice = 0m;
			}
		}

		// If flat, open new random position
		if (Position == 0 && _positionSide == null)
		{
			var side = DetermineNextSide();

			if (side == Sides.Buy)
				BuyMarket(Volume);
			else
				SellMarket(Volume);

			_positionSide = side;
			_entryPrice = price;

			if (Mode != RandomModes.Generator)
				_sequenceLastSide = side;
		}
	}

	private Sides DetermineNextSide()
	{
		return Mode switch
		{
			RandomModes.Generator => (_random?.Next(2) ?? 0) == 0 ? Sides.Buy : Sides.Sell,
			RandomModes.BuySellBuy => _sequenceLastSide == Sides.Buy ? Sides.Sell : Sides.Buy,
			RandomModes.SellBuySell => _sequenceLastSide == Sides.Sell ? Sides.Buy : Sides.Sell,
			_ => Sides.Buy
		};
	}
}
