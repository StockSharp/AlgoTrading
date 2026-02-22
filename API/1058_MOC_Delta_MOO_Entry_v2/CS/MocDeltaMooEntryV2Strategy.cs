using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MOC Delta MOO Entry v2 strategy.
/// Uses volume delta to detect momentum and trade in the same direction.
/// </summary>
public class MocDeltaMooEntryV2Strategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _deltaWindow;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _smaFast;
	private SMA _smaSlow;

	private decimal _sessionBuyVol;
	private decimal _sessionSellVol;
	private int _candleCount;
	private decimal _prevDelta;
	private bool _hasPrevDelta;

	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int DeltaWindow { get => _deltaWindow.Value; set => _deltaWindow.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MocDeltaMooEntryV2Strategy()
	{
		_smaLength = Param(nameof(SmaLength), 15);
		_deltaWindow = Param(nameof(DeltaWindow), 12);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sessionBuyVol = 0;
		_sessionSellVol = 0;
		_candleCount = 0;
		_prevDelta = 0;
		_hasPrevDelta = false;

		_smaFast = new SMA { Length = SmaLength };
		_smaSlow = new SMA { Length = SmaLength * 2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_smaFast, _smaSlow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaFast, decimal smaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.ClosePrice > candle.OpenPrice)
			_sessionBuyVol += candle.TotalVolume;
		else
			_sessionSellVol += candle.TotalVolume;

		_candleCount++;

		if (_candleCount % DeltaWindow == 0)
		{
			var totalVol = _sessionBuyVol + _sessionSellVol;
			var delta = totalVol > 0 ? (_sessionBuyVol - _sessionSellVol) / totalVol * 100m : 0m;

			_sessionBuyVol = 0;
			_sessionSellVol = 0;

			if (_hasPrevDelta)
			{
				// Momentum: follow the delta direction
				if (_prevDelta > 1m && candle.ClosePrice > smaFast && smaFast > smaSlow && Position <= 0)
				{
					if (Position < 0)
						BuyMarket(Math.Abs(Position));
					BuyMarket();
				}
				else if (_prevDelta < -1m && candle.ClosePrice < smaFast && smaFast < smaSlow && Position >= 0)
				{
					if (Position > 0)
						SellMarket(Position);
					SellMarket();
				}
			}

			_prevDelta = delta;
			_hasPrevDelta = true;
			return;
		}

		// Exit on trend reversal
		if (Position > 0 && candle.ClosePrice < smaSlow)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice > smaSlow)
			BuyMarket();
	}
}
