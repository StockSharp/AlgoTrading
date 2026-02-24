namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy converted from the KWAN_CCC expert advisor.
/// Uses CCI and Momentum to detect trend transitions.
/// Enters long when CCI turns up while momentum is positive,
/// enters short when CCI turns down while momentum is negative.
/// </summary>
public class KwanCccStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _momentumPeriod;

	private decimal _prevCci;
	private decimal _prevMom;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public KwanCccStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI length", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum length", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCci = 0;
		_prevMom = 0;
		_initialized = false;

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(cci, momentum, (ICandleMessage candle, decimal cciValue, decimal momValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!_initialized)
				{
					_prevCci = cciValue;
					_prevMom = momValue;
					_initialized = true;
					return;
				}

				var cciUp = cciValue > _prevCci;
				var cciDown = cciValue < _prevCci;

				// Buy when CCI turns up and momentum is rising
				if (cciUp && momValue > _prevMom && Position <= 0)
				{
					BuyMarket();
				}
				// Sell when CCI turns down and momentum is falling
				else if (cciDown && momValue < _prevMom && Position >= 0)
				{
					SellMarket();
				}

				_prevCci = cciValue;
				_prevMom = momValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}
}
