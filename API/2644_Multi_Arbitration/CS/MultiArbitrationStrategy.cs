using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-direction arbitration strategy adapted from MetaTrader logic.
/// </summary>
public class MultiArbitrationStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitForClose;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maxOpenPositions;
	private readonly StrategyParam<DataType> _candleType;

	private bool _initialOrderPlaced;
	private decimal _entryPrice;
	private Sides? _currentSide;

	/// <summary>
	/// Target profit that triggers a full position exit.
	/// </summary>
	public decimal ProfitForClose
	{
		get => _profitForClose.Value;
		set => _profitForClose.Value = value;
	}

	/// <summary>
	/// Volume used when sending market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous positions allowed before forcing a flatten.
	/// </summary>
	public int MaxOpenPositions
	{
		get => _maxOpenPositions.Value;
		set => _maxOpenPositions.Value = value;
	}

	/// <summary>
	/// Candle type used for synchronization and decision making.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiArbitrationStrategy"/> class.
	/// </summary>
	public MultiArbitrationStrategy()
	{
		_profitForClose = Param(nameof(ProfitForClose), 300m)
			.SetDisplay("Profit Threshold", "Profit required before flattening all positions.", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume used when opening new positions.", "Trading");

		_maxOpenPositions = Param(nameof(MaxOpenPositions), 15)
			.SetGreaterThanZero()
			.SetDisplay("Max Open Positions", "Maximum simultaneous positions allowed before closing everything.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used to synchronize trading decisions.", "Data");
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

		_initialOrderPlaced = false;
		_entryPrice = 0m;
		_currentSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialOrderPlaced)
		{
			OpenLong(candle);
			_initialOrderPlaced = true;
		}

		var longCount = _currentSide == Sides.Buy ? 1 : 0;
		var shortCount = _currentSide == Sides.Sell ? 1 : 0;

		var longProfit = _currentSide == Sides.Buy ? (candle.ClosePrice - _entryPrice) * Volume : 0m;
		var shortProfit = _currentSide == Sides.Sell ? (_entryPrice - candle.ClosePrice) * Volume : 0m;

		if (longCount + shortCount < MaxOpenPositions)
		{
			if (longProfit < shortProfit && _currentSide != Sides.Buy)
			{
				OpenLong(candle);
			}
			else if (shortProfit < longProfit && _currentSide != Sides.Sell)
			{
				OpenShort(candle);
			}
			else if (longProfit == 0m && shortProfit == 0m && Position == 0 && _currentSide is null)
			{
				OpenLong(candle);
			}
		}
		else if (PnL > 0m && Position != 0)
		{
			FlattenPosition(candle);
		}

		if (PnL > ProfitForClose && Position != 0)
		{
			FlattenPosition(candle);
		}
	}

	private void OpenLong(ICandleMessage candle)
	{
		var volumeToBuy = Volume;

		if (Position < 0)
		{
			volumeToBuy += Math.Abs(Position);
		}
		else if (Position > 0)
		{
			// Already holding a long position, so only refresh the entry reference.
			_entryPrice = candle.ClosePrice;
			_currentSide = Sides.Buy;
			return;
		}

		BuyMarket(volumeToBuy);
		_entryPrice = candle.ClosePrice;
		_currentSide = Sides.Buy;
	}

	private void OpenShort(ICandleMessage candle)
	{
		var volumeToSell = Volume;

		if (Position > 0)
		{
			volumeToSell += Position;
		}
		else if (Position < 0)
		{
			// Already holding a short position, so only refresh the entry reference.
			_entryPrice = candle.ClosePrice;
			_currentSide = Sides.Sell;
			return;
		}

		SellMarket(volumeToSell);
		_entryPrice = candle.ClosePrice;
		_currentSide = Sides.Sell;
	}

	private void FlattenPosition(ICandleMessage candle)
	{
		if (_currentSide is null)
			return;

		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_currentSide = null;
		_entryPrice = 0m;
	}
}
