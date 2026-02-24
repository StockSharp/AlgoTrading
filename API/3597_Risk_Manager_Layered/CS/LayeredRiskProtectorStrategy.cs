using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Risk Manager Layered" MetaTrader expert.
/// Uses CCI crossover for entry with position management. CCI above level -> sell,
/// CCI below negative level -> buy, with volume-based confirmation.
/// </summary>
public class LayeredRiskProtectorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<decimal> _cciLevel;

	private CommodityChannelIndex _cci;
	private decimal? _prevCci;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	public LayeredRiskProtectorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");

		_cciLength = Param(nameof(CciLength), 75)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "CCI indicator period", "Indicators");

		_cciLevel = Param(nameof(CciLevel), 100m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Level", "CCI threshold for entries", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cci = new CommodityChannelIndex { Length = CciLength };
		_prevCci = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_cci.IsFormed)
		{
			_prevCci = cciValue;
			return;
		}

		if (_prevCci is null)
		{
			_prevCci = cciValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// CCI crosses below -level -> buy
		var buyCross = _prevCci.Value >= -CciLevel && cciValue < -CciLevel;
		// CCI crosses above +level -> sell
		var sellCross = _prevCci.Value <= CciLevel && cciValue > CciLevel;

		if (buyCross)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (sellCross)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevCci = cciValue;
	}
}
