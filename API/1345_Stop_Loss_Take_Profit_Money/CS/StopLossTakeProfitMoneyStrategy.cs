using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA crossover strategy with take profit and stop loss in money.
/// </summary>
public class StopLossTakeProfitMoneyStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitMoney;
	private readonly StrategyParam<decimal> _stopLossMoney;
	private readonly StrategyParam<DataType> _candleType;
	
	private SMA _fastMa;
	private SMA _slowMa;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;
	private decimal _entryPrice;
	
	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	
	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	
	/// <summary>
	/// Take profit amount in money.
	/// </summary>
	public decimal TakeProfitMoney
	{
		get => _takeProfitMoney.Value;
		set => _takeProfitMoney.Value = value;
	}
	
	/// <summary>
	/// Stop loss amount in money.
	/// </summary>
	public decimal StopLossMoney
	{
		get => _stopLossMoney.Value;
		set => _stopLossMoney.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="StopLossTakeProfitMoneyStrategy"/>.
	/// </summary>
	public StopLossTakeProfitMoneyStrategy()
	{
		_fastLength = Param(nameof(FastLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMA", "Fast SMA length", "Parameters");
		
		_slowLength = Param(nameof(SlowLength), 28)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMA", "Slow SMA length", "Parameters");
		
		_takeProfitMoney = Param(nameof(TakeProfitMoney), 200m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit $", "Take profit in money", "Risk Management");
		
		_stopLossMoney = Param(nameof(StopLossMoney), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss $", "Stop loss in money", "Risk Management");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_isInitialized = false;
		_entryPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_fastMa = new SMA { Length = FastLength };
		_slowMa = new SMA { Length = SlowLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!_isInitialized)
		{
			if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;
			
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_isInitialized = true;
			return;
		}
		
		var wasFastBelow = _prevFast < _prevSlow;
		var isFastBelow = fastValue < slowValue;
		
		if (wasFastBelow && !isFastBelow)
		{
			if (Position <= 0)
			{
				_entryPrice = candle.ClosePrice;
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
		else if (!wasFastBelow && isFastBelow)
		{
			if (Position >= 0)
			{
				_entryPrice = candle.ClosePrice;
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		
		if (Position != 0 && _entryPrice != 0m)
		CheckTargets(candle.ClosePrice);
		
		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
	
	private void CheckTargets(decimal currentPrice)
	{
		var priceStep = Security.PriceStep ?? 1m;
		var stepPrice = Security.StepPrice ?? 1m;
		if (priceStep == 0m || stepPrice == 0m)
		return;
		
		var diff = currentPrice - _entryPrice;
		var steps = diff / priceStep;
		var pnl = steps * stepPrice * Position;
		
		if (pnl >= TakeProfitMoney)
		{
			if (Position > 0)
			SellMarket(Math.Abs(Position));
			else
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0m;
		}
		else if (pnl <= -StopLossMoney)
		{
			if (Position > 0)
			SellMarket(Math.Abs(Position));
			else
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0m;
		}
	}
}
