using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual smoothed moving average and RSI hedge strategy converted from Improve.mq5.
/// </summary>
public class ImproveMaRsiHedgeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<Security> _hedgeSecurity;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _fastMa = null!;
	private SmoothedMovingAverage _slowMa = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal _baseLastClose;
	private decimal _hedgeLastClose;
	private decimal _baseEntryPrice;
	private decimal _hedgeEntryPrice;
	private bool _hasBaseClose;
	private bool _hasHedgeClose;
	private int _pairDirection;

	/// <summary>
	/// Order volume for each instrument.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Profit target across both legs expressed in money.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Second instrument traded alongside the primary security.
	/// </summary>
	public Security HedgeSecurity
	{
		get => _hedgeSecurity.Value;
		set => _hedgeSecurity.Value = value;
	}

	/// <summary>
	/// Smoothed moving average period for the fast line.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Smoothed moving average period for the slow line.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI oversold threshold.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// RSI overbought threshold.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ImproveMaRsiHedgeStrategy"/> class.
	/// </summary>
	public ImproveMaRsiHedgeStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for each symbol", "Trading");

		_profitTarget = Param(nameof(ProfitTarget), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")
			.SetCanOptimize(true);

		_hedgeSecurity = Param<Security>(nameof(HedgeSecurity))
			.SetDisplay("Hedge Security", "Secondary instrument to trade", "General");

		_fastPeriod = Param(nameof(FastMaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast smoothed MA period", "Indicators")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowMaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow smoothed MA period", "Indicators")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI", "Indicators")
			.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold", "RSI oversold threshold", "Indicators")
			.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought", "RSI overbought threshold", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculations", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (HedgeSecurity != null)
			yield return (HedgeSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa = null!;
		_slowMa = null!;
		_rsi = null!;
		_baseLastClose = 0m;
		_hedgeLastClose = 0m;
		_baseEntryPrice = 0m;
		_hedgeEntryPrice = 0m;
		_hasBaseClose = false;
		_hasHedgeClose = false;
		_pairDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security must be specified.");

		if (HedgeSecurity == null)
			throw new InvalidOperationException("Hedge security must be specified.");

		if (FastMaPeriod >= SlowMaPeriod)
			throw new InvalidOperationException("Fast MA period must be less than slow MA period.");

		_fastMa = new SmoothedMovingAverage { Length = FastMaPeriod };
		_slowMa = new SmoothedMovingAverage { Length = SlowMaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription
			.Bind(_fastMa, _slowMa, _rsi, ProcessBaseCandle)
			.Start();

		var hedgeSubscription = SubscribeCandles(CandleType, false, HedgeSecurity);
		hedgeSubscription
			.Bind(ProcessHedgeCandle)
			.Start();
	}

	private void ProcessBaseCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_baseLastClose = candle.ClosePrice;
		_hasBaseClose = true;

		CheckProfitTarget();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_rsi.IsFormed)
			return;

		if (_pairDirection != 0)
			return;

		if (!_hasHedgeClose)
			return;

		if (slowValue > fastValue && rsiValue <= OversoldLevel)
		{
			OpenPair(1);
		}
		else if (slowValue < fastValue && rsiValue >= OverboughtLevel)
		{
			OpenPair(-1);
		}
	}

	private void ProcessHedgeCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_hedgeLastClose = candle.ClosePrice;
		_hasHedgeClose = true;

		CheckProfitTarget();
	}

	private void OpenPair(int direction)
	{
		if (direction == 0)
			return;

		var basePos = GetPositionValue(Security, Portfolio) ?? 0m;
		var hedgePos = GetPositionValue(HedgeSecurity, Portfolio) ?? 0m;

		if (basePos != 0m || hedgePos != 0m)
			return;

		var volume = Volume;

		if (direction > 0)
		{
			BuyMarket(volume, Security);
			BuyMarket(volume, HedgeSecurity);
		}
		else
		{
			SellMarket(volume, Security);
			SellMarket(volume, HedgeSecurity);
		}

		_pairDirection = direction;
		_baseEntryPrice = _baseLastClose;
		_hedgeEntryPrice = _hedgeLastClose;
	}

	private void CheckProfitTarget()
	{
		if (_pairDirection == 0 || !_hasBaseClose || !_hasHedgeClose)
			return;

		var baseProfit = _pairDirection > 0
			? (_baseLastClose - _baseEntryPrice) * Volume
			: (_baseEntryPrice - _baseLastClose) * Volume;

		var hedgeProfit = _pairDirection > 0
			? (_hedgeLastClose - _hedgeEntryPrice) * Volume
			: (_hedgeEntryPrice - _hedgeLastClose) * Volume;

		var totalProfit = baseProfit + hedgeProfit;

		if (totalProfit >= ProfitTarget)
		{
			ClosePair();
		}
	}

	private void ClosePair()
	{
		var basePos = GetPositionValue(Security, Portfolio) ?? 0m;
		if (basePos > 0)
		{
			SellMarket(basePos, Security);
		}
		else if (basePos < 0)
		{
			BuyMarket(-basePos, Security);
		}

		var hedgePos = GetPositionValue(HedgeSecurity, Portfolio) ?? 0m;
		if (hedgePos > 0)
		{
			SellMarket(hedgePos, HedgeSecurity);
		}
		else if (hedgePos < 0)
		{
			BuyMarket(-hedgePos, HedgeSecurity);
		}

		_pairDirection = 0;
		_baseEntryPrice = 0m;
		_hedgeEntryPrice = 0m;
	}
}
