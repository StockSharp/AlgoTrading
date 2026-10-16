using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only fixed-quote DCA on lower Bollinger band crossings, calendar
/// month changes, or both. Liquidates accumulated holdings on CloseDate.
/// </summary>
public class DcaMeanReversionBollingerBandStrategy : Strategy
{
    private readonly StrategyParam<decimal> _investmentAmount;
    private readonly StrategyParam<DateTime> _openDate;
    private readonly StrategyParam<DateTime> _closeDate;
    private readonly StrategyParam<string> _strategyMode;
    private readonly StrategyParam<int> _bollingerPeriod;
    private readonly StrategyParam<decimal> _bollingerMultiplier;
    private readonly StrategyParam<DataType> _candleType;

    private BollingerBands _bands;
    private decimal? _previousClose;
    private decimal? _previousLower;
    private int _lastInvestmentMonth;
    private int _lastInvestmentYear;
    private bool _liquidated;

    public decimal InvestmentAmount { get => _investmentAmount.Value; set => _investmentAmount.Value = value; }
    public DateTime OpenDate { get => _openDate.Value; set => _openDate.Value = value; }
    public DateTime CloseDate { get => _closeDate.Value; set => _closeDate.Value = value; }
    public string StrategyMode { get => _strategyMode.Value; set => _strategyMode.Value = value; }
    public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
    public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    public DcaMeanReversionBollingerBandStrategy()
    {
        _investmentAmount = Param(nameof(InvestmentAmount), 200m)
            .SetGreaterThanZero()
            .SetDisplay("Investment Amount", "Fixed quoted-currency amount per buy", "Trading");
        _openDate = Param(nameof(OpenDate), DateTime.MinValue)
            .SetDisplay("Open Date", "Inclusive first buy date", "Calendar");
        _closeDate = Param(nameof(CloseDate), DateTime.MaxValue)
            .SetDisplay("Close Date", "Exclusive end date; sell all on its first available candle", "Calendar");
        _strategyMode = Param(nameof(StrategyMode), "Combined")
            .SetDisplay("Strategy Mode", "Bollinger, Monthly or Combined", "Signals");
        _bollingerPeriod = Param(nameof(BollingerPeriod), 20)
            .SetGreaterThanZero()
            .SetDisplay("Bollinger Period", "Bollinger Bands averaging period", "Indicators");
        _bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
            .SetGreaterThanZero()
            .SetDisplay("Bollinger Multiplier", "Standard deviations for the lower band", "Indicators");
        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
            .SetDisplay("Candle Type", "Completed-candle interval", "General");
    }

    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
    {
        return [(Security, CandleType)];
    }

    protected override void OnReseted()
    {
        base.OnReseted();
        _bands = null;
        _previousClose = null;
        _previousLower = null;
        _lastInvestmentMonth = 0;
        _lastInvestmentYear = 0;
        _liquidated = false;
    }

    protected override void OnStarted2(DateTime time)
    {
        base.OnStarted2(time);

        if (OpenDate.Date > CloseDate.Date)
            throw new ArgumentException("OpenDate must not be after CloseDate.");
        if (StrategyMode != "Bollinger" && StrategyMode != "Monthly" && StrategyMode != "Combined")
            throw new ArgumentException("StrategyMode must be Bollinger, Monthly or Combined.");

        _bands = new BollingerBands { Length = BollingerPeriod, Width = BollingerMultiplier };
        _previousClose = null;
        _previousLower = null;
        _lastInvestmentMonth = 0;
        _lastInvestmentYear = 0;
        _liquidated = false;

        var subscription = SubscribeCandles(CandleType);
        subscription.BindEx(_bands, ProcessCandle).Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, subscription);
            DrawIndicator(area, _bands);
            DrawOwnTrades(area);
        }
    }

    private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var date = candle.OpenTime.Date;
        if (date < OpenDate.Date)
            return;

        // Liquidation does not depend on a new Bollinger crossing.
        if (date >= CloseDate.Date)
        {
            if (!_liquidated && Position > 0m && IsFormedAndOnlineAndAllowTrading())
            {
                SellMarket(Position);
                _liquidated = true;
            }
            return;
        }

        if (value is not BollingerBandsValue bandsValue ||
            bandsValue.LowBand is not decimal lowerBand)
            return;

        var close = candle.ClosePrice;
        var crossBelow = _previousClose is decimal oldClose &&
            _previousLower is decimal oldLower &&
            oldClose >= oldLower && close < lowerBand;

        _previousClose = close;
        _previousLower = lowerBand;

        if (!_bands.IsFormed || !IsFormedAndOnlineAndAllowTrading())
            return;

        var monthly = StrategyMode != "Bollinger" &&
            (_lastInvestmentMonth != date.Month || _lastInvestmentYear != date.Year);
        var bollinger = StrategyMode != "Monthly" && crossBelow;
        if (!monthly && !bollinger)
            return;

        var step = Security?.VolumeStep ?? 0m;
        if (step <= 0m)
            step = 0.001m;
        var price = candle.ClosePrice;
        if (price <= 0m)
            return;

        var maxVolume = Security.MaxVolume > 0m ? Security.MaxVolume : decimal.MaxValue;
        var volume = Math.Floor(Math.Min(InvestmentAmount / price, maxVolume) / step) * step;
        if (volume <= 0m || (Security.MinVolume > 0m && volume < Security.MinVolume))
            return;

        BuyMarket(volume);
        if (monthly)
        {
            _lastInvestmentMonth = date.Month;
            _lastInvestmentYear = date.Year;
        }
    }
}
