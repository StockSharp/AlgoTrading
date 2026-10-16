using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fixed quote-currency investment either at regular completed-candle
/// intervals or once per configured weekday. Never shorts.
/// </summary>
public class DcaStrategy : Strategy
{
    private readonly StrategyParam<DataType> _candleType;
    private readonly StrategyParam<decimal> _capitalToInvest;
    private readonly StrategyParam<DateTime> _startDate;
    private readonly StrategyParam<DateTime> _endDate;
    private readonly StrategyParam<bool> _closeAllOnLastCandle;
    private readonly StrategyParam<bool> _useDayOfWeek;
    private readonly StrategyParam<DayOfWeek> _dayOfWeek;
    private readonly StrategyParam<int> _everyXCandles;

    private int _eligibleBars;
    private DateTime? _lastWeekdayPurchase;
    private bool _endReached;

    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
    public decimal CapitalToInvest { get => _capitalToInvest.Value; set => _capitalToInvest.Value = value; }
    public DateTime StartDate { get => _startDate.Value; set => _startDate.Value = value; }
    public DateTime EndDate { get => _endDate.Value; set => _endDate.Value = value; }
    public bool CloseAllOnLastCandle { get => _closeAllOnLastCandle.Value; set => _closeAllOnLastCandle.Value = value; }
    public bool UseDayOfWeek { get => _useDayOfWeek.Value; set => _useDayOfWeek.Value = value; }
    public DayOfWeek DayOfWeek { get => _dayOfWeek.Value; set => _dayOfWeek.Value = value; }
    public int EveryXCandles { get => _everyXCandles.Value; set => _everyXCandles.Value = value; }

    public DcaStrategy()
    {
        _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
            .SetDisplay("Candle Type", "Completed-candle interval for scheduling", "General");
        _capitalToInvest = Param(nameof(CapitalToInvest), 200m)
            .SetGreaterThanZero()
            .SetDisplay("Capital To Invest", "Fixed quote-currency amount for each purchase", "Trading");
        _startDate = Param(nameof(StartDate), DateTime.MinValue)
            .SetDisplay("Start Date", "Inclusive start of the buying range", "Calendar");
        _endDate = Param(nameof(EndDate), DateTime.MaxValue)
            .SetDisplay("End Date", "Exclusive end of the buying range", "Calendar");
        _closeAllOnLastCandle = Param(nameof(CloseAllOnLastCandle), false)
            .SetDisplay("Close All On Last Candle", "Liquidate accumulated holdings when EndDate is reached", "Calendar");
        _useDayOfWeek = Param(nameof(UseDayOfWeek), false)
            .SetDisplay("Use Day Of Week", "Purchase once per selected weekday instead of every N bars", "Calendar");
        _dayOfWeek = Param(nameof(DayOfWeek), System.DayOfWeek.Wednesday)
            .SetDisplay("Day Of Week", "Weekday for scheduled purchases", "Calendar");
        _everyXCandles = Param(nameof(EveryXCandles), 72)
            .SetGreaterThanZero()
            .SetDisplay("Every X Candles", "Completed candles per periodic purchase", "Calendar");
    }

    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
    {
        return [(Security, CandleType)];
    }

    protected override void OnReseted()
    {
        base.OnReseted();
        _eligibleBars = 0;
        _lastWeekdayPurchase = null;
        _endReached = false;
    }

    protected override void OnStarted2(DateTime time)
    {
        base.OnStarted2(time);

        if (StartDate.Date > EndDate.Date)
            throw new ArgumentException("StartDate must not be after EndDate.");

        _eligibleBars = 0;
        _lastWeekdayPurchase = null;
        _endReached = false;

        var subscription = SubscribeCandles(CandleType);
        subscription.Bind(ProcessCandle).Start();

        var area = CreateChartArea();
        if (area != null)
        {
            DrawCandles(area, subscription);
            DrawOwnTrades(area);
        }
    }

    private void ProcessCandle(ICandleMessage candle)
    {
        if (candle.State != CandleStates.Finished)
            return;

        var date = candle.OpenTime.Date;
        if (date < StartDate.Date)
            return;

        // Treat EndDate as the first day on which new investments cease;
        // optionally exit once, using actual holdings rather than Volume.
        if (date >= EndDate.Date)
        {
            if (!_endReached && CloseAllOnLastCandle &&
                Position > 0m && IsFormedAndOnlineAndAllowTrading())
            {
                SellMarket(Position);
                _endReached = true;
            }
            return;
        }

        if (!IsFormedAndOnlineAndAllowTrading())
            return;

        if (UseDayOfWeek)
        {
            if (candle.OpenTime.DayOfWeek != DayOfWeek ||
                _lastWeekdayPurchase == date)
                return;
        }
        else
        {
            _eligibleBars++;
            if (_eligibleBars % EveryXCandles != 0)
                return;
        }

        var price = candle.ClosePrice;
        if (price <= 0m || Security is null)
            return;

        // The budget is expressed in quoted currency. Floor to the native
        // volume step and never silently round up into over-investment.
        var step = Security.VolumeStep > 0m ? Security.VolumeStep : 0.001m;
        var maxVolume = Security.MaxVolume > 0m ? Security.MaxVolume : decimal.MaxValue;
        var budgetVolume = Math.Min(CapitalToInvest / price, maxVolume);
        var volume = Math.Floor(budgetVolume / step) * step;

        if (volume <= 0m || (Security.MinVolume > 0m && volume < Security.MinVolume))
            return;

        BuyMarket(volume);
        if (UseDayOfWeek)
            _lastWeekdayPurchase = date;
    }
}
