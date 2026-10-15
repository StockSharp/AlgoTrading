import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

LONDON_START = TimeSpan(2, 0, 0)
NY_START = TimeSpan(8, 20, 0)
NY_END = TimeSpan(16, 0, 0)
ASIA_START = TimeSpan(19, 0, 0)
LONDON_CLOSE_START = TimeSpan(10, 30, 0)
LONDON_CLOSE_END = TimeSpan(13, 0, 0)


class ict_bread_and_butter_sell_setup_strategy(Strategy):
    """
    ICT Bread and Butter Sell-Setup strategy.
    Tracks the London (02:00-08:20 UTC), New York (08:20-16:00 UTC) and Asia (19:00-02:00 UTC) session ranges.
    NY short: during the NY session a bearish candle whose high exceeds the London session high.
    London close buy: between 10:30 and 13:00 a close below the London session low.
    Asia short: during the Asia session a close above the Asia session high so far.
    Every setup has its own stop loss and take profit in ticks; an opposite setup reverses the position.
    """

    def __init__(self):
        super(ict_bread_and_butter_sell_setup_strategy, self).__init__()
        self._short_stop_ticks = self.Param("ShortStopTicks", 10).SetNotNegative().SetDisplay("Short Stop Ticks", "Stop loss ticks for NY short entry", "Risk Management")
        self._short_take_ticks = self.Param("ShortTakeTicks", 20).SetNotNegative().SetDisplay("Short Take Profit Ticks", "Take profit ticks for NY short entry", "Risk Management")
        self._buy_stop_ticks = self.Param("BuyStopTicks", 10).SetNotNegative().SetDisplay("Buy Stop Ticks", "Stop loss ticks for London close buy", "Risk Management")
        self._buy_take_ticks = self.Param("BuyTakeTicks", 20).SetNotNegative().SetDisplay("Buy Take Profit Ticks", "Take profit ticks for London close buy", "Risk Management")
        self._asia_stop_ticks = self.Param("AsiaStopTicks", 10).SetNotNegative().SetDisplay("Asia Stop Ticks", "Stop loss ticks for Asia sell entry", "Risk Management")
        self._asia_take_ticks = self.Param("AsiaTakeTicks", 15).SetNotNegative().SetDisplay("Asia Take Profit Ticks", "Take profit ticks for Asia sell entry", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._london_high = None
        self._london_low = None
        self._asia_high = None
        self._in_london = False
        self._in_asia = False
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(ict_bread_and_butter_sell_setup_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ict_bread_and_butter_sell_setup_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        tod = candle.OpenTime.TimeOfDay
        high = candle.HighPrice
        low = candle.LowPrice
        close = candle.ClosePrice

        in_london = tod >= LONDON_START and tod < NY_START
        in_ny = tod >= NY_START and tod < NY_END
        in_asia = tod >= ASIA_START or tod < LONDON_START

        if in_london:
            if not self._in_london or self._london_high is None:
                self._london_high = high
                self._london_low = low
            else:
                self._london_high = max(self._london_high, high)
                self._london_low = min(self._london_low, low)
        self._in_london = in_london

        # The Asia breakout is measured against the range before this candle.
        prev_asia_high = self._asia_high if self._in_asia and in_asia else None
        if in_asia:
            self._asia_high = high if prev_asia_high is None else max(prev_asia_high, high)
        self._in_asia = in_asia

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_exits(high, low):
            return

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        if step <= 0:
            step = Decimal(1)

        ny_short = in_ny and self._london_high is not None and high > self._london_high and close < candle.OpenPrice
        london_close_buy = tod >= LONDON_CLOSE_START and tod <= LONDON_CLOSE_END and self._london_low is not None and close < self._london_low
        asia_short = in_asia and prev_asia_high is not None and close > prev_asia_high

        if ny_short and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(close, False, self._short_stop_ticks.Value * step, self._short_take_ticks.Value * step)
        elif london_close_buy and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._set_levels(close, True, self._buy_stop_ticks.Value * step, self._buy_take_ticks.Value * step)
        elif asia_short and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(close, False, self._asia_stop_ticks.Value * step, self._asia_take_ticks.Value * step)

    def _set_levels(self, entry, is_long, stop, take):
        if stop > 0:
            self._stop_price = entry - stop if is_long else entry + stop
        else:
            self._stop_price = None
        if take > 0:
            self._take_price = entry + take if is_long else entry - take
        else:
            self._take_price = None

    def _check_exits(self, high, low):
        if self.Position > 0:
            if (self._stop_price is not None and low <= self._stop_price) or (self._take_price is not None and high >= self._take_price):
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return True
        elif self.Position < 0:
            if (self._stop_price is not None and high >= self._stop_price) or (self._take_price is not None and low <= self._take_price):
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return True
        return False

    def CreateClone(self):
        return ict_bread_and_butter_sell_setup_strategy()
