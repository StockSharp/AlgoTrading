using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Horizontal line based entry strategy.
/// Places a market order when price crosses user defined levels.
/// Supports stop-loss, take-profit and trailing stop in pips.
/// </summary>
public class MyLineOrderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyPrice;
	private readonly StrategyParam<decimal> _sellPrice;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousClose;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _isLong;

	/// <summary>
	/// Price level to trigger long entry. Set zero to disable.
	/// </summary>
	public decimal BuyPrice
	{
	    get => _buyPrice.Value;
	    set => _buyPrice.Value = value;
	}

	/// <summary>
	/// Price level to trigger short entry. Set zero to disable.
	/// </summary>
	public decimal SellPrice
	{
	    get => _sellPrice.Value;
	    set => _sellPrice.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
	    get => _takeProfitPips.Value;
	    set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
	    get => _stopLossPips.Value;
	    set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Zero disables trailing.
	/// </summary>
	public decimal TrailingStopPips
	{
	    get => _trailingStopPips.Value;
	    set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MyLineOrderStrategy"/>.
	/// </summary>
	public MyLineOrderStrategy()
	{
	    _buyPrice = Param(nameof(BuyPrice), 0m)
	        .SetDisplay("Buy Price", "Price level to trigger buy order", "Trading");

	    _sellPrice = Param(nameof(SellPrice), 0m)
	        .SetDisplay("Sell Price", "Price level to trigger sell order", "Trading");

	    _takeProfitPips = Param(nameof(TakeProfitPips), 30m)
	        .SetGreaterThanOrEqual(0m)
	        .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

	    _stopLossPips = Param(nameof(StopLossPips), 20m)
	        .SetGreaterThanOrEqual(0m)
	        .SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

	    _trailingStopPips = Param(nameof(TrailingStopPips), 0m)
	        .SetGreaterThanOrEqual(0m)
	        .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

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
	    _previousClose = 0;
	    _entryPrice = 0;
	    _stopPrice = 0;
	    _takePrice = 0;
	    _isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    var subscription = SubscribeCandles(CandleType);
	    subscription.Bind(ProcessCandle).Start();

	    StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    var step = Security.PriceStep ?? 1m;

	    if (Position == 0)
	    {
	        if (BuyPrice > 0m && _previousClose < BuyPrice && candle.ClosePrice >= BuyPrice)
	        {
	            BuyMarket();
	            _isLong = true;
	            _entryPrice = candle.ClosePrice;
	            _stopPrice = _entryPrice - StopLossPips * step;
	            _takePrice = _entryPrice + TakeProfitPips * step;
	        }
	        else if (SellPrice > 0m && _previousClose > SellPrice && candle.ClosePrice <= SellPrice)
	        {
	            SellMarket();
	            _isLong = false;
	            _entryPrice = candle.ClosePrice;
	            _stopPrice = _entryPrice + StopLossPips * step;
	            _takePrice = _entryPrice - TakeProfitPips * step;
	        }
	    }
	    else
	    {
	        if (_isLong)
	        {
	            if (candle.LowPrice <= _stopPrice)
	            {
	                SellMarket(Math.Abs(Position));
	            }
	            else if (candle.HighPrice >= _takePrice)
	            {
	                SellMarket(Math.Abs(Position));
	            }
	            else if (TrailingStopPips > 0m)
	            {
	                var newStop = candle.ClosePrice - TrailingStopPips * step;
	                if (newStop > _stopPrice && candle.ClosePrice > _entryPrice)
	                    _stopPrice = newStop;
	            }
	        }
	        else
	        {
	            if (candle.HighPrice >= _stopPrice)
	            {
	                BuyMarket(Math.Abs(Position));
	            }
	            else if (candle.LowPrice <= _takePrice)
	            {
	                BuyMarket(Math.Abs(Position));
	            }
	            else if (TrailingStopPips > 0m)
	            {
	                var newStop = candle.ClosePrice + TrailingStopPips * step;
	                if (newStop < _stopPrice && candle.ClosePrice < _entryPrice)
	                    _stopPrice = newStop;
	            }
	        }
	    }

	    _previousClose = candle.ClosePrice;
	}
}
