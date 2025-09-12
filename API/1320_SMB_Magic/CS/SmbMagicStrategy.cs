using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMB Magic breakout strategy.
/// Trades breakouts on large candles with volume confirmation.
/// </summary>
public class SmbMagicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _balance;
	private readonly StrategyParam<string> _riskModel;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _pipThreshold;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevVolume;
	private bool _isInitialized;

	public decimal Balance { get => _balance.Value; set => _balance.Value = value; }
	public string RiskModel { get => _riskModel.Value; set => _riskModel.Value = value; }
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	public int PipThreshold { get => _pipThreshold.Value; set => _pipThreshold.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SmbMagicStrategy()
	{
		_balance = Param(nameof(Balance), 5000m)
			.SetGreaterThanZero()
			.SetDisplay("Balance", "Account balance", "General");

		_riskModel = Param(nameof(RiskModel), "Low Risk")
			.SetDisplay("Risk Model", "Risk model for position sizing", "General");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Volume multiplier for breakout", "Trading");

		_pipThreshold = Param(nameof(PipThreshold), 200)
			.SetGreaterThanZero()
			.SetDisplay("Pip Threshold", "Movement threshold in pips", "Trading");

		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of EMA", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	private decimal CalcLotSize()
	{
		return RiskModel switch
		{
			"Low Risk" => Math.Max(Balance / 20000m, 0.01m),
			"Medium Risk" => Math.Max(Balance / 10000m, 0.01m),
			"High Risk" => Math.Max(Balance / 5000m, 0.01m),
			"Full Margin" => Math.Max(Balance / 2500m, 0.01m),
			_ => Math.Max(Balance / 20000m, 0.01m),
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevVolume = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = CalcLotSize();

		var ema = new EMA { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevClose = candle.ClosePrice;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevVolume = candle.TotalVolume;
			_isInitialized = true;
			return;
		}

		var movementInTicks = PipThreshold * (Security?.PriceStep ?? 0m) * 10m;
		var candleMovement = candle.HighPrice - candle.LowPrice;

		var positiveCandle = candle.ClosePrice > candle.OpenPrice && candleMovement > movementInTicks;
		var negativeCandle = candle.ClosePrice < candle.OpenPrice && candleMovement > movementInTicks;

		var breakoutUp = _prevClose < _prevHigh && candle.ClosePrice >= _prevHigh && candle.TotalVolume > VolumeMultiplier * _prevVolume;
		var breakoutDown = _prevClose > _prevLow && candle.ClosePrice <= _prevLow && candle.TotalVolume > VolumeMultiplier * _prevVolume;

		var buySignal = breakoutUp && positiveCandle && candle.ClosePrice > emaValue;
		var sellSignal = breakoutDown && negativeCandle && candle.ClosePrice < emaValue;

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sellSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevVolume = candle.TotalVolume;
	}
}
