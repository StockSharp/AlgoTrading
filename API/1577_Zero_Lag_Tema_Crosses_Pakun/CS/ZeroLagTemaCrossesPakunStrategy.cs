using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on zero-lag TEMA crossovers with stop and target from recent extremes.
/// </summary>
public class ZeroLagTemaCrossesPakunStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private TripleExponentialMovingAverage _fastTema = null!;
	private TripleExponentialMovingAverage _slowTema = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _entryPlaced;

	/// <summary>
	/// Lookback period for stop calculation.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Fast TEMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow TEMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Risk-reward ratio for exits.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZeroLagTemaCrossesPakunStrategy"/>.
	/// </summary>
	public ZeroLagTemaCrossesPakunStrategy()
	{
		_lookback = Param(nameof(Lookback), 20).SetDisplay("Lookback", "Lookback period", "Indicators");
		_fastPeriod = Param(nameof(FastPeriod), 69).SetDisplay("Fast Period", "Fast TEMA length", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 130).SetDisplay("Slow Period", "Slow TEMA length", "Indicators");
		_riskReward = Param(nameof(RiskReward), 1.5m).SetDisplay("Risk/Reward", "Take profit ratio", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_fastTema = new TripleExponentialMovingAverage { Length = FastPeriod };
		_slowTema = new TripleExponentialMovingAverage { Length = SlowPeriod };
		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastTema, _slowTema, _highest, _lowest, ProcessCandle)
			.Start();

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		_prevFast = fast;
		_prevSlow = slow;

		var price = candle.ClosePrice;

		if (!_entryPlaced)
		{
			if (crossUp && Position <= 0)
			{
			    CancelActiveOrders();
			    var volume = Volume + Math.Abs(Position);
			    BuyMarket(volume);
			    _entryPlaced = true;
			    _stopPrice = lowest;
			    _takeProfitPrice = price + (price - _stopPrice) * RiskReward;
			}
			else if (crossDown && Position >= 0)
			{
			    CancelActiveOrders();
			    var volume = Volume + Math.Abs(Position);
			    SellMarket(volume);
			    _entryPlaced = true;
			    _stopPrice = highest;
			    _takeProfitPrice = price - (_stopPrice - price) * RiskReward;
			}
		}
		else
		{
			if (Position > 0)
			{
			    if (candle.LowPrice <= _stopPrice)
			    {
			        SellMarket(Position);
			        _entryPlaced = false;
			    }
			    else if (candle.HighPrice >= _takeProfitPrice)
			    {
			        SellMarket(Position);
			        _entryPlaced = false;
			    }
			}
			else if (Position < 0)
			{
			    if (candle.HighPrice >= _stopPrice)
			    {
			        BuyMarket(Math.Abs(Position));
			        _entryPlaced = false;
			    }
			    else if (candle.LowPrice <= _takeProfitPrice)
			    {
			        BuyMarket(Math.Abs(Position));
			        _entryPlaced = false;
			    }
			}
		}
	}
}
