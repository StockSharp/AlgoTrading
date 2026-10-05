import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, DateTime, DateTimeKind, TimeZoneInfo
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class cp_strat_orb_strategy(Strategy):
    """
    CP Strat ORB strategy.
    The New York opening range spans 9:30-9:45 local exchange time and is traded only when it is at least MinRangePoints wide.
    After a close above the range high, a later candle that dips back to the high and closes above it buys; after a close below
    the range low, a candle that rallies back to the low and closes below it sells short. At most MaxTradesPerSession entries
    are taken per day and every trade has a fixed StopPoints stop and TakePoints target.
    """

    _range_start = TimeSpan(9, 30, 0)
    _range_end = TimeSpan(9, 45, 0)
    _new_york = TimeZoneInfo.FindSystemTimeZoneById("America/New_York")

    def __init__(self):
        super(cp_strat_orb_strategy, self).__init__()
        self._min_range_points = self.Param("MinRangePoints", 60.0).SetNotNegative().SetDisplay("Min Range", "Minimum opening range width in points", "Range")
        self._stop_points = self.Param("StopPoints", 20.0).SetNotNegative().SetDisplay("Stop Points", "Stop loss in points", "Risk")
        self._take_points = self.Param("TakePoints", 60.0).SetNotNegative().SetDisplay("Take Points", "Take profit in points", "Risk")
        self._max_trades_per_session = self.Param("MaxTradesPerSession", 3).SetGreaterThanZero().SetDisplay("Max Trades", "Maximum entries per session", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._session_date = None
        self._reset_session()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_session(self):
        self._range_high = None
        self._range_low = None
        self._broke_up = False
        self._broke_down = False
        self._trades_today = 0

    def OnReseted(self):
        super(cp_strat_orb_strategy, self).OnReseted()
        self._session_date = None
        self._reset_session()

    def _price_step(self):
        step = self.Security.PriceStep if self.Security is not None else None
        return step if step is not None else Decimal(1)

    def OnStarted2(self, time):
        super(cp_strat_orb_strategy, self).OnStarted2(time)

        self._session_date = None
        self._reset_session()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        step = self._price_step()
        take = float(self._take_points.Value)
        stop = float(self._stop_points.Value)
        self.StartProtection(
            Unit(Decimal(take) * step, UnitTypes.Absolute) if take > 0 else Unit(),
            Unit(Decimal(stop) * step, UnitTypes.Absolute) if stop > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(candle.OpenTime, DateTimeKind.Utc), self._new_york)

        if self._session_date is None or self._session_date != local.Date:
            self._session_date = local.Date
            self._reset_session()

        time_of_day = local.TimeOfDay

        if time_of_day < self._range_start:
            return

        if time_of_day < self._range_end:
            self._range_high = candle.HighPrice if self._range_high is None else max(self._range_high, candle.HighPrice)
            self._range_low = candle.LowPrice if self._range_low is None else min(self._range_low, candle.LowPrice)
            return

        if self._range_high is None or self._range_low is None:
            return

        range_high = self._range_high
        range_low = self._range_low

        if range_high - range_low < Decimal(self._min_range_points.Value) * self._price_step():
            return

        close = candle.ClosePrice

        long_signal = self._broke_up and candle.LowPrice <= range_high and close > range_high
        short_signal = self._broke_down and candle.HighPrice >= range_low and close < range_low

        if close > range_high:
            self._broke_up = True
        if close < range_low:
            self._broke_down = True

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0 or self._trades_today >= self._max_trades_per_session.Value:
            return

        if long_signal:
            self.BuyMarket(self.Volume)
            self._trades_today += 1
            self._broke_up = False
        elif short_signal:
            self.SellMarket(self.Volume)
            self._trades_today += 1
            self._broke_down = False

    def CreateClone(self):
        return cp_strat_orb_strategy()
