import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class hans123_trader_v2_strategy(Strategy):
    def __init__(self):
        super(hans123_trader_v2_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0)
        self._start_hour = self.Param("StartHour", 0)
        self._end_hour = self.Param("EndHour", 23)
        self._breakout_period = self.Param("BreakoutPeriod", 10)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._highest = None
        self._lowest = None
        self._entry_price = 0.0
        self._pip_size = 1.0
        self._sl_dist = 0.0
        self._tp_dist = 0.0
        self._trail_dist = 0.0
        self._trail_step_dist = 0.0
        self._highest_stop = 0.0
        self._prev_high = None
        self._prev_low = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(hans123_trader_v2_strategy, self).OnStarted(time)

        self._pip_size = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        self._sl_dist = self._stop_loss_pips.Value * self._pip_size
        self._tp_dist = self._take_profit_pips.Value * self._pip_size
        self._trail_dist = self._trailing_stop_pips.Value * self._pip_size
        self._trail_step_dist = self._trailing_step_pips.Value * self._pip_size

        self._highest = Highest()
        self._highest.Length = self._breakout_period.Value
        self._lowest = Lowest()
        self._lowest.Length = self._breakout_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._highest, self._lowest, self._process_candle).Start()

    def _process_candle(self, candle, breakout_high_val, breakout_low_val):
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        breakout_high = float(breakout_high_val)
        breakout_low = float(breakout_low_val)

        if self.Position != 0:
            self._manage_position(candle)
            return

        hour = candle.OpenTime.TimeOfDay.Hours
        if hour < self._start_hour.Value or hour >= self._end_hour.Value:
            self._prev_high = breakout_high
            self._prev_low = breakout_low
            return

        if self._prev_high is not None and self._prev_low is not None:
            if float(candle.HighPrice) > self._prev_high:
                self.BuyMarket()
                self._entry_price = float(candle.ClosePrice)
                self._highest_stop = 0.0
            elif float(candle.LowPrice) < self._prev_low:
                self.SellMarket()
                self._entry_price = float(candle.ClosePrice)
                self._highest_stop = 0.0

        self._prev_high = breakout_high
        self._prev_low = breakout_low

    def _manage_position(self, candle):
        price = float(candle.ClosePrice)

        if self.Position > 0:
            if self._sl_dist > 0 and float(candle.LowPrice) <= self._entry_price - self._sl_dist:
                self.SellMarket(self.Position)
                return
            if self._tp_dist > 0 and float(candle.HighPrice) >= self._entry_price + self._tp_dist:
                self.SellMarket(self.Position)
                return
            if self._trail_dist > 0:
                move = price - self._entry_price
                if move > self._trail_dist + self._trail_step_dist:
                    new_stop = price - self._trail_dist
                    if new_stop > self._highest_stop + self._trail_step_dist:
                        self._highest_stop = new_stop
                    if self._highest_stop > 0 and float(candle.LowPrice) <= self._highest_stop:
                        self.SellMarket(self.Position)
                        return
        elif self.Position < 0:
            vol = abs(self.Position)
            if self._sl_dist > 0 and float(candle.HighPrice) >= self._entry_price + self._sl_dist:
                self.BuyMarket(vol)
                return
            if self._tp_dist > 0 and float(candle.LowPrice) <= self._entry_price - self._tp_dist:
                self.BuyMarket(vol)
                return
            if self._trail_dist > 0:
                move = self._entry_price - price
                if move > self._trail_dist + self._trail_step_dist:
                    new_stop = price + self._trail_dist
                    if self._highest_stop == 0 or new_stop < self._highest_stop - self._trail_step_dist:
                        self._highest_stop = new_stop
                    if self._highest_stop > 0 and float(candle.HighPrice) >= self._highest_stop:
                        self.BuyMarket(vol)
                        return

    def OnReseted(self):
        super(hans123_trader_v2_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._highest_stop = 0.0
        self._prev_high = None
        self._prev_low = None

    def CreateClone(self):
        return hans123_trader_v2_strategy()
