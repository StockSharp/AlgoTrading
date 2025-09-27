using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Quatro SMA strategy with volume filter and tiered profit targets.
/// </summary>
public class QuatroSmaStrategy : Strategy
{
	private readonly StrategyParam<int> _sma1Length;
	private readonly StrategyParam<int> _sma2Length;
	private readonly StrategyParam<int> _sma3Length;
	private readonly StrategyParam<int> _smaLongLength;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfit1;
	private readonly StrategyParam<decimal> _takeProfit2;
	private readonly StrategyParam<decimal> _takeProfit3;
	private readonly StrategyParam<int> _tpQty1;
	private readonly StrategyParam<int> _tpQty2;
	private readonly StrategyParam<int> _tpQty3;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _volumeSma;

	private decimal _entryPrice;
	private decimal _entryVolume;
	private decimal _remainingVolume;
	private bool _tp1Done;
	private bool _tp2Done;
	private bool _tp3Done;
	private decimal _tp1Price;
	private decimal _tp2Price;
	private decimal _tp3Price;
	private decimal _stopPrice;

	/// <summary>
	/// First short SMA length.
	/// </summary>
	public int Sma1Length
	{
		get => _sma1Length.Value;
		set => _sma1Length.Value = value;
	}

	/// <summary>
	/// Second short SMA length.
	/// </summary>
	public int Sma2Length
	{
		get => _sma2Length.Value;
		set => _sma2Length.Value = value;
	}

	/// <summary>
	/// Third short SMA length.
	/// </summary>
	public int Sma3Length
	{
		get => _sma3Length.Value;
		set => _sma3Length.Value = value;
	}

	/// <summary>
	/// Long SMA length.
	/// </summary>
	public int SmaLongLength
	{
		get => _smaLongLength.Value;
		set => _smaLongLength.Value = value;
	}

	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolumeSmaLength
	{
		get => _volumeSmaLength.Value;
		set => _volumeSmaLength.Value = value;
	}

	/// <summary>
	/// Volume multiplier over average.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// First take profit percent.
	/// </summary>
	public decimal TakeProfit1
	{
		get => _takeProfit1.Value;
		set => _takeProfit1.Value = value;
	}

	/// <summary>
	/// Second take profit percent.
	/// </summary>
	public decimal TakeProfit2
	{
		get => _takeProfit2.Value;
		set => _takeProfit2.Value = value;
	}

	/// <summary>
	/// Third take profit percent.
	/// </summary>
	public decimal TakeProfit3
	{
		get => _takeProfit3.Value;
		set => _takeProfit3.Value = value;
	}

	/// <summary>
	/// Quantity percent for first take profit.
	/// </summary>
	public int TpQty1
	{
		get => _tpQty1.Value;
		set => _tpQty1.Value = value;
	}

	/// <summary>
	/// Quantity percent for second take profit.
	/// </summary>
	public int TpQty2
	{
		get => _tpQty2.Value;
		set => _tpQty2.Value = value;
	}

	/// <summary>
	/// Quantity percent for third take profit.
	/// </summary>
	public int TpQty3
	{
		get => _tpQty3.Value;
		set => _tpQty3.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="QuatroSmaStrategy"/> class.
	/// </summary>
	public QuatroSmaStrategy()
	{
		_sma1Length = Param(nameof(Sma1Length), 4)
						  .SetGreaterThanZero()
						  .SetDisplay("SMA1 Length", "Period of first short SMA", "SMA");

		_sma2Length = Param(nameof(Sma2Length), 16)
						  .SetGreaterThanZero()
						  .SetDisplay("SMA2 Length", "Period of second short SMA", "SMA");

		_sma3Length = Param(nameof(Sma3Length), 32)
						  .SetGreaterThanZero()
						  .SetDisplay("SMA3 Length", "Period of third short SMA", "SMA");

		_smaLongLength = Param(nameof(SmaLongLength), 200)
							 .SetGreaterThanZero()
							 .SetDisplay("Long SMA Length", "Period of long SMA", "SMA");

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 40)
							   .SetGreaterThanZero()
							   .SetDisplay("Volume SMA Length", "Period of average volume", "Volume");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2.5m)
								.SetGreaterThanZero()
								.SetDisplay("Volume Multiplier", "Multiplier over average volume", "Volume");

		_stopLossPercent = Param(nameof(StopLossPercent), 10m)
							   .SetGreaterThanZero()
							   .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfit1 = Param(nameof(TakeProfit1), 10m)
						   .SetGreaterThanZero()
						   .SetDisplay("TP1 %", "First take profit percentage", "Risk");

		_takeProfit2 = Param(nameof(TakeProfit2), 20m)
						   .SetGreaterThanZero()
						   .SetDisplay("TP2 %", "Second take profit percentage", "Risk");

		_takeProfit3 = Param(nameof(TakeProfit3), 50m)
						   .SetGreaterThanZero()
						   .SetDisplay("TP3 %", "Third take profit percentage", "Risk");

		_tpQty1 = Param(nameof(TpQty1), 25)
					  .SetRange(1, 100)
					  .SetDisplay("TP1 Qty %", "Quantity percent for first take profit", "Risk");

		_tpQty2 = Param(nameof(TpQty2), 50)
					  .SetRange(1, 100)
					  .SetDisplay("TP2 Qty %", "Quantity percent for second take profit", "Risk");

		_tpQty3 = Param(nameof(TpQty3), 100)
					  .SetRange(1, 100)
					  .SetDisplay("TP3 Qty %", "Quantity percent for third take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_volumeSma = null;
		ResetTrade();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma1 = new SMA { Length = Sma1Length };
		var sma2 = new SMA { Length = Sma2Length };
		var sma3 = new SMA { Length = Sma3Length };
		var smaLong = new SMA { Length = SmaLongLength };
		_volumeSma = new SMA { Length = VolumeSmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(sma1, sma2, sma3, smaLong, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma1);
			DrawIndicator(area, sma2);
			DrawIndicator(area, sma3);
			DrawIndicator(area, smaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma1, decimal sma2, decimal sma3, decimal smaLong)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeMa = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longMa = sma1 > sma2 && sma2 > sma3;
		var shortMa = sma1 < sma2 && sma2 < sma3;
		var volumeCond = candle.TotalVolume > volumeMa * VolumeMultiplier;
		var longCond = longMa && candle.ClosePrice > smaLong && volumeCond;
		var shortCond = shortMa && candle.ClosePrice < smaLong && volumeCond;
		var longClose = shortMa && candle.ClosePrice > smaLong;
		var shortClose = longMa && candle.ClosePrice < smaLong;

		if (longCond && Position <= 0)
			EnterLong(candle.ClosePrice);
		else if (shortCond && Position >= 0)
			EnterShort(candle.ClosePrice);

		if (Position > 0)
			CheckLongExits(candle, longClose);
		else if (Position < 0)
			CheckShortExits(candle, shortClose);
	}

	private void EnterLong(decimal price)
	{
		_entryVolume = Volume + Math.Abs(Position);
		BuyMarket(_entryVolume);
		_entryPrice = price;
		_remainingVolume = _entryVolume;

		_tp1Price = _entryPrice * (1 + TakeProfit1 / 100m);
		_tp2Price = _entryPrice * (1 + TakeProfit2 / 100m);
		_tp3Price = _entryPrice * (1 + TakeProfit3 / 100m);
		_stopPrice = _entryPrice * (1 - StopLossPercent / 100m);

		_tp1Done = _tp2Done = _tp3Done = false;
	}

	private void EnterShort(decimal price)
	{
		_entryVolume = Volume + Math.Abs(Position);
		SellMarket(_entryVolume);
		_entryPrice = price;
		_remainingVolume = _entryVolume;

		_tp1Price = _entryPrice * (1 - TakeProfit1 / 100m);
		_tp2Price = _entryPrice * (1 - TakeProfit2 / 100m);
		_tp3Price = _entryPrice * (1 - TakeProfit3 / 100m);
		_stopPrice = _entryPrice * (1 + StopLossPercent / 100m);

		_tp1Done = _tp2Done = _tp3Done = false;
	}

	private void CheckLongExits(ICandleMessage candle, bool longClose)
	{
		var vol1 = _entryVolume * TpQty1 / 100m;
		var vol2 = _entryVolume * (TpQty2 - TpQty1) / 100m;
		var vol3 = _entryVolume - vol1 - vol2;

		if (!_tp1Done && candle.HighPrice >= _tp1Price)
		{
			SellMarket(vol1);
			_tp1Done = true;
			_remainingVolume -= vol1;
		}

		if (!_tp2Done && candle.HighPrice >= _tp2Price)
		{
			SellMarket(vol2);
			_tp2Done = true;
			_remainingVolume -= vol2;
		}

		if (!_tp3Done && candle.HighPrice >= _tp3Price)
		{
			SellMarket(vol3);
			_tp3Done = true;
			_remainingVolume -= vol3;
		}

		if (candle.LowPrice <= _stopPrice)
		{
			SellMarket(_remainingVolume);
			ResetTrade();
			return;
		}

		if (longClose && _remainingVolume > 0)
		{
			SellMarket(_remainingVolume);
			ResetTrade();
		}
	}

	private void CheckShortExits(ICandleMessage candle, bool shortClose)
	{
		var vol1 = _entryVolume * TpQty1 / 100m;
		var vol2 = _entryVolume * (TpQty2 - TpQty1) / 100m;
		var vol3 = _entryVolume - vol1 - vol2;

		if (!_tp1Done && candle.LowPrice <= _tp1Price)
		{
			BuyMarket(vol1);
			_tp1Done = true;
			_remainingVolume -= vol1;
		}

		if (!_tp2Done && candle.LowPrice <= _tp2Price)
		{
			BuyMarket(vol2);
			_tp2Done = true;
			_remainingVolume -= vol2;
		}

		if (!_tp3Done && candle.LowPrice <= _tp3Price)
		{
			BuyMarket(vol3);
			_tp3Done = true;
			_remainingVolume -= vol3;
		}

		if (candle.HighPrice >= _stopPrice)
		{
			BuyMarket(_remainingVolume);
			ResetTrade();
			return;
		}

		if (shortClose && _remainingVolume > 0)
		{
			BuyMarket(_remainingVolume);
			ResetTrade();
		}
	}

	private void ResetTrade()
	{
		_entryPrice = 0m;
		_entryVolume = 0m;
		_remainingVolume = 0m;
		_tp1Done = _tp2Done = _tp3Done = false;
	}
}
