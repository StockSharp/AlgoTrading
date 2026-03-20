import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Lowest, Highest

class zig_and_zag_trader_strategy(Strategy):
    PIVOT_NONE = 0
    PIVOT_LOW = 1
    PIVOT_HIGH = 2

    def __init__(self):
        super(zig_and_zag_trader_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Candles used for swing detection", "General")
        self._lots = self.Param("Lots", 0.1).SetDisplay("Lots", "Requested trade size", "Trading")
        self._trend_depth = self.Param("TrendDepth", 3).SetDisplay("Trend Depth", "Lookback for long-term ZigZag", "ZigZag")
        self._exit_depth = self.Param("ExitDepth", 3).SetDisplay("Exit Depth", "Lookback for short-term ZigZag", "ZigZag")
        self._max_orders = self.Param("MaxOrders", 1).SetDisplay("Max Orders", "Maximum simultaneous positions", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 0.0).SetDisplay("Stop Loss pips", "Protective stop distance", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 0.0).SetDisplay("Take Profit pips", "Profit target distance", "Risk")
        self._pip_size = 0.0001
        self._volume_step_val = 1.0
        self._breakout_threshold = 0.0
        self._last_trend_low = None
        self._last_trend_high = None
        self._last_short_low = None
        self._last_short_high = None
        self._last_slalom_zig = None
        self._last_slalom_zag = None
        self._trend_up = True
        self._prev_trend_up = True
        self._buy_armed = False
        self._sell_armed = False
        self._limit_armed = False
        self._last_pivot = self.PIVOT_NONE
        self._long_term_low = None
        self._long_term_high = None
        self._short_term_low = None
        self._short_term_high = None

    @property
    def CandleType(self): return self._candle_type.Value
    @property
    def Lots(self): return self._lots.Value
    @property
    def TrendDepth(self): return self._trend_depth.Value
    @property
    def ExitDepth(self): return self._exit_depth.Value
    @property
    def MaxOrders(self): return self._max_orders.Value
    @property
    def StopLossPips(self): return self._stop_loss_pips.Value
    @property
    def TakeProfitPips(self): return self._take_profit_pips.Value

    def OnStarted(self, time):
        super(zig_and_zag_trader_strategy, self).OnStarted(time)
        ps = self.Security.PriceStep if self.Security is not None else None
        self._pip_size = float(ps) if ps is not None and float(ps) > 0 else 0.0001
        vs = self.Security.VolumeStep if self.Security is not None else None
        self._volume_step_val = float(vs) if vs is not None and float(vs) > 0 else 1.0
        self._breakout_threshold = self._pip_size
        import math
        raw_vol = float(self.Lots) if float(self.Lots) > 0 else self._volume_step_val
        if raw_vol < self._volume_step_val:
            raw_vol = self._volume_step_val
        steps = max(1, int(math.ceil(raw_vol / self._volume_step_val)))
        self.Volume = steps * self._volume_step_val
        tp_p = float(self.TakeProfitPips)
        sl_p = float(self.StopLossPips)
        tp_unit = Unit(tp_p * self._pip_size, UnitTypes.Absolute) if tp_p > 0 else None
        sl_unit = Unit(sl_p * self._pip_size, UnitTypes.Absolute) if sl_p > 0 else None
        self.StartProtection(takeProfit=tp_unit, stopLoss=sl_unit, useMarketOrders=True)
        self._long_term_low = Lowest()
        self._long_term_low.Length = self.TrendDepth
        self._long_term_high = Highest()
        self._long_term_high.Length = self.TrendDepth
        self._short_term_low = Lowest()
        self._short_term_low.Length = self.ExitDepth
        self._short_term_high = Highest()
        self._short_term_high.Length = self.ExitDepth
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        t = candle.CloseTime
        long_low = float(self._long_term_low.Process(candle.LowPrice, t, True).ToDecimal())
        long_high = float(self._long_term_high.Process(candle.HighPrice, t, True).ToDecimal())
        short_low = float(self._short_term_low.Process(candle.LowPrice, t, True).ToDecimal())
        short_high = float(self._short_term_high.Process(candle.HighPrice, t, True).ToDecimal())
        long_formed = self._long_term_low.IsFormed and self._long_term_high.IsFormed
        short_formed = self._short_term_low.IsFormed and self._short_term_high.IsFormed
        cl = float(candle.ClosePrice)
        op = float(candle.OpenPrice)
        hi = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        navel = (5.0 * cl + 2.0 * op + hi + lo) / 9.0
        if long_formed:
            if lo == long_low and (self._last_trend_low is None or long_low != self._last_trend_low):
                self._trend_up = True
                self._last_trend_low = long_low
            if hi == long_high and (self._last_trend_high is None or long_high != self._last_trend_high):
                self._trend_up = False
                self._last_trend_high = long_high
        if self._trend_up != self._prev_trend_up:
            self._buy_armed = False
            self._sell_armed = False
            self._limit_armed = False
            self._prev_trend_up = self._trend_up
        if short_formed:
            if lo == short_low and (self._last_short_low is None or short_low != self._last_short_low):
                self._last_pivot = self.PIVOT_LOW
                self._last_short_low = short_low
                self._last_slalom_zig = navel
                self._buy_armed = False
                self._sell_armed = False
                self._limit_armed = False
            if hi == short_high and (self._last_short_high is None or short_high != self._last_short_high):
                self._last_pivot = self.PIVOT_HIGH
                self._last_short_high = short_high
                self._last_slalom_zag = navel
                self._buy_armed = False
                self._sell_armed = False
                self._limit_armed = False
        if not long_formed or not short_formed:
            return
        buy_signal = False
        sell_signal = False
        close_signal = False
        if self._last_pivot == self.PIVOT_LOW and self._last_slalom_zig is not None:
            if self._trend_up:
                sb = navel - self._last_slalom_zig >= self._breakout_threshold
                if sb and not self._buy_armed:
                    self._buy_armed = True
                    buy_signal = True
                elif not sb and self._buy_armed and navel <= self._last_slalom_zig:
                    self._buy_armed = False
                if self._limit_armed and navel <= self._last_slalom_zig:
                    self._limit_armed = False
            else:
                sc = navel > self._last_slalom_zig
                if sc and not self._limit_armed:
                    self._limit_armed = True
                    close_signal = True
                elif not sc and self._limit_armed:
                    self._limit_armed = False
                self._buy_armed = False
        elif self._last_pivot == self.PIVOT_HIGH and self._last_slalom_zag is not None:
            if not self._trend_up:
                ss = self._last_slalom_zag - navel >= self._breakout_threshold
                if ss and not self._sell_armed:
                    self._sell_armed = True
                    sell_signal = True
                elif not ss and self._sell_armed and navel >= self._last_slalom_zag:
                    self._sell_armed = False
                if self._limit_armed and navel >= self._last_slalom_zag:
                    self._limit_armed = False
            else:
                sc = self._last_slalom_zag > navel
                if sc and not self._limit_armed:
                    self._limit_armed = True
                    close_signal = True
                elif not sc and self._limit_armed:
                    self._limit_armed = False
                self._sell_armed = False
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._execute_signals(buy_signal, sell_signal, close_signal)

    def _execute_signals(self, buy_signal, sell_signal, close_signal):
        volume = self.Volume
        if volume <= 0 or int(self.MaxOrders) <= 0:
            return
        max_vol = int(self.MaxOrders) * float(volume)
        if buy_signal:
            cur_long = float(self.Position) if self.Position > 0 else 0.0
            avail = max_vol - cur_long
            if avail > 0:
                self.BuyMarket(min(float(volume), avail))
        if sell_signal:
            cur_short = -float(self.Position) if self.Position < 0 else 0.0
            avail = max_vol - cur_short
            if avail > 0:
                self.SellMarket(min(float(volume), avail))
        if close_signal and self.Position != 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(abs(self.Position))

    def OnReseted(self):
        super(zig_and_zag_trader_strategy, self).OnReseted()
        self._long_term_low = None
        self._long_term_high = None
        self._short_term_low = None
        self._short_term_high = None
        self._last_trend_low = None
        self._last_trend_high = None
        self._last_short_low = None
        self._last_short_high = None
        self._last_slalom_zig = None
        self._last_slalom_zag = None
        self._trend_up = True
        self._prev_trend_up = True
        self._buy_armed = False
        self._sell_armed = False
        self._limit_armed = False
        self._last_pivot = self.PIVOT_NONE

    def CreateClone(self):
        return zig_and_zag_trader_strategy()
