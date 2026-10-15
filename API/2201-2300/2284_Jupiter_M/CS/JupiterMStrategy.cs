using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI-based grid trading strategy inspired by Jupiter M.
/// Enters on CCI level crosses, exits on opposite signal.
/// </summary>
public class JupiterMStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevCci;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal BuyLevel { get => _buyLevel.Value; set => _buyLevel.Value = value; }
	public decimal SellLevel { get => _sellLevel.Value; set => _sellLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public JupiterMStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 50)
			.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
			.SetGreaterThanZero();

		_buyLevel = Param(nameof(BuyLevel), -100m)
			.SetDisplay("Buy Level", "CCI level to buy (cross above)", "Trading");

		_sellLevel = Param(nameof(SellLevel), 100m)
			.SetDisplay("Sell Level", "CCI level to sell (cross below)", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_prevCci = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCci = null;

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevCci is null)
		{
			_prevCci = cci;
			return;
		}

		// CCI crosses above buy level -> buy
		if (_prevCci <= BuyLevel && cci > BuyLevel && Position <= 0)
			BuyMarket();
		// CCI crosses below sell level -> sell
		else if (_prevCci >= SellLevel && cci < SellLevel && Position >= 0)
			SellMarket();

		_prevCci = cci;
	}
}
