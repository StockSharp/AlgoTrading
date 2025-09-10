using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buys 10% of portfolio when S&P 500 and TIPS are above SMA at a new month.
/// </summary>
public class TwoXSpyTipsStrategy : Strategy
{
	private readonly StrategyParam<Security> _sp500Security;
	private readonly StrategyParam<Security> _tipsSecurity;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _sp500Price;
	private decimal _sp500Sma;
	private decimal _tipsPrice;
	private decimal _tipsSma;
	private int _lastMonth;

	/// <summary>
	/// Security used for S&P 500 prices.
	/// </summary>
	public Security Sp500Security
	{
		get => _sp500Security.Value;
		set => _sp500Security.Value = value;
	}

	/// <summary>
	/// Security used for TIPS prices.
	/// </summary>
	public Security TipsSecurity
	{
		get => _tipsSecurity.Value;
		set => _tipsSecurity.Value = value;
	}

	/// <summary>
	/// Length of moving averages.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Position leverage multiplier.
	/// </summary>
	public decimal Leverage
	{
		get => _leverage.Value;
		set => _leverage.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TwoXSpyTipsStrategy"/>.
	/// </summary>
	public TwoXSpyTipsStrategy()
	{
		_sp500Security = Param<Security>(nameof(Sp500Security))
		                     .SetDisplay("S&P 500 Security", "Security for S&P 500 data", "General");

		_tipsSecurity =
		    Param<Security>(nameof(TipsSecurity)).SetDisplay("TIPS Security", "Security for TIPS data", "General");

		_smaLength = Param(nameof(SmaLength), 200)
		                 .SetGreaterThanZero()
		                 .SetDisplay("SMA Length", "Length of moving averages", "Indicators")
		                 .SetCanOptimize(true)
		                 .SetOptimize(100, 300, 50);

		_leverage = Param(nameof(Leverage), 2m)
		                .SetGreaterThanZero()
		                .SetDisplay("Leverage", "Position leverage multiplier", "Trading")
		                .SetCanOptimize(true)
		                .SetOptimize(1m, 6m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		                  .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Sp500Security, CandleType), (TipsSecurity, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_sp500Price = default;
		_sp500Sma = default;
		_tipsPrice = default;
		_tipsSma = default;
		_lastMonth = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Sp500Security == null || TipsSecurity == null)
			throw new InvalidOperationException("Required securities are not specified.");

		var sp500Sma = new SimpleMovingAverage { Length = SmaLength };
		var tipsSma = new SimpleMovingAverage { Length = SmaLength };

		var mainSub = SubscribeCandles(CandleType).Bind(ProcessMain).Start();

		SubscribeCandles(CandleType, security: Sp500Security).Bind(sp500Sma, ProcessSp500).Start();

		SubscribeCandles(CandleType, security: TipsSecurity).Bind(tipsSma, ProcessTips).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessSp500(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_sp500Price = candle.ClosePrice;
		_sp500Sma = sma;
	}

	private void ProcessTips(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_tipsPrice = candle.ClosePrice;
		_tipsSma = sma;
	}

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var month = candle.OpenTime.Month;
		if (month == _lastMonth)
			return;

		_lastMonth = month;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_sp500Price <= _sp500Sma || _tipsPrice <= _tipsSma)
			return;

		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		if (portfolioValue <= 0)
			return;

		var capital = portfolioValue * 0.1m;
		var qty = (capital / candle.ClosePrice) * Leverage;

		if (qty > 0)
			BuyMarket(qty);
	}
}
