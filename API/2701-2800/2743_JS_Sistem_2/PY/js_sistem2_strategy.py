import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, MovingAverageConvergenceDivergence,
    Highest, Lowest
)
from StockSharp.Algo.Strategies import Strategy


class js_sistem2_strategy(Strategy):
    def __init__(self):
        super(js_sistem2_strategy, self).__init__()

        self._min_balance = self.Param("MinBalance", 100.0)
        self._stop_loss_pips = self.Param("StopLossPips", 200)
        self._take_profit_pips = self.Param("TakeProfitPips", 300)
        self._min_difference_pips = self.Param("MinDifferencePips", 5000)
        self._volatility_period = self.Param("VolatilityPeriod", 15)
        self._trailing_enabled = self.Param("TrailingEnabled", True)
        self._trailing_indent_pips = self.Param("TrailingIndentPips", 1)
        self._ma_fast_period = self.Param("MaFastPeriod", 12)
        self._ma_medium_period = self.Param("MaMediumPeriod", 26)
        self._ma_slow_period = self.Param("MaSlowPeriod", 50)
        self._osma_fast_period = self.Param("OsmaFastPeriod", 12)
        self._osma_slow_period = self.Param("OsmaSlowPeriod", 26)
        self._osma_signal_period = self.Param("OsmaSignalPeriod", 9)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._ema_fast = None
        self._ema_medium = None
        self._ema_slow = None
        self._macd = None
        self._highest = None
        self._lowest = None
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MinBalance(self):
        return self._min_balance.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def MinDifferencePips(self):
        return self._min_difference_pips.Value

    @property
    def VolatilityPeriod(self):
        return self._volatility_period.Value

    @property
    def TrailingEnabled(self):
        return self._trailing_enabled.Value

    @property
    def TrailingIndentPips(self):
        return self._trailing_indent_pips.Value

    @property
    def MaFastPeriod(self):
        return self._ma_fast_period.Value

    @property
    def MaMediumPeriod(self):
        return self._ma_medium_period.Value

    @property
    def MaSlowPeriod(self):
        return self._ma_slow_period.Value

    @property
    def OsmaFastPeriod(self):
        return self._osma_fast_period.Value

    @property
    def OsmaSlowPeriod(self):
        return self._osma_slow_period.Value

    @property
    def OsmaSignalPeriod(self):
        return self._osma_signal_period.Value

    def OnStarted2(self, time):
        super(js_sistem2_strategy, self).OnStarted2(time)

        self._ema_fast = ExponentialMovingAverage()
        self._ema_fast.Length = self.MaFastPeriod
        self._ema_medium = ExponentialMovingAverage()
        self._ema_medium.Length = self.MaMediumPeriod
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = self.MaSlowPeriod

        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.OsmaSlowPeriod
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.OsmaFastPeriod
        self._macd = MovingAverageConvergenceDivergence(slow_ma, fast_ma)

        self._highest = Highest()
        self._highest.Length = self.VolatilityPeriod
        self._lowest = Lowest()
        self._lowest.Length = self.VolatilityPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(
            self._ema_fast, self._ema_medium, self._ema_slow,
            self._macd, self._highest, self._lowest,
            self._process_candle
        ).Start()

    def _process_candle(self, candle, ema_fast_v, ema_medium_v, ema_slow_v, macd_v, highest_v, lowest_v):
        if candle.State != CandleStates.Finished:
            return

        if (not self._ema_fast.IsFormed or not self._ema_medium.IsFormed or
                not self._ema_slow.IsFormed or not self._macd.IsFormed or
                not self._highest.IsFormed or not self._lowest.IsFormed):
            return

        step = self._calculate_pip_size()
        if step == 0:
            sec = self.Security
            step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if step == 0:
            step = 1.0

        ef = float(ema_fast_v)
        em = float(ema_medium_v)
        es = float(ema_slow_v)
        macd_line = float(macd_v)
        hv = float(highest_v)
        lv = float(lowest_v)

        stop_dist = self.StopLossPips * step if self.StopLossPips > 0 else 0.0
        take_dist = self.TakeProfitPips * step if self.TakeProfitPips > 0 else 0.0
        min_diff = self.MinDifferencePips * step
        indent = self.TrailingIndentPips * step

        self._update_trailing(candle, hv, lv, indent)
        self._handle_stops(candle)

        pf = self.Portfolio
        can_trade = True
        if pf is not None and pf.CurrentValue is not None:
            can_trade = float(pf.CurrentValue) >= self.MinBalance

        ema_order_long = ef > em and em > es
        ema_order_short = ef < em and em < es
        ema_spread_long = abs(ef - es) < min_diff
        ema_spread_short = abs(es - ef) < min_diff

        long_cond = can_trade and ema_order_long and ema_spread_long and macd_line > 0
        short_cond = can_trade and ema_order_short and ema_spread_short and macd_line < 0

        close_price = float(candle.ClosePrice)

        if long_cond and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
                self._reset_orders()
            if self.Volume > 0:
                self.BuyMarket()
                self._entry_price = close_price
                self._stop_price = self._entry_price - stop_dist if stop_dist > 0 else None
                self._take_price = self._entry_price + take_dist if take_dist > 0 else None
        elif short_cond and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
                self._reset_orders()
            if self.Volume > 0:
                self.SellMarket()
                self._entry_price = close_price
                self._stop_price = self._entry_price + stop_dist if stop_dist > 0 else None
                self._take_price = self._entry_price - take_dist if take_dist > 0 else None

    def _update_trailing(self, candle, highest_v, lowest_v, indent):
        if not self.TrailingEnabled:
            return

        close = float(candle.ClosePrice)
        if self.Position > 0:
            new_stop = lowest_v
            if new_stop > 0 and close - new_stop > indent and new_stop - self._entry_price > indent:
                if self._stop_price is None or new_stop - self._stop_price > indent:
                    self._stop_price = new_stop
        elif self.Position < 0:
            new_stop = highest_v
            if new_stop > 0 and new_stop - close > indent and self._entry_price - new_stop > indent:
                if self._stop_price is None or self._stop_price - new_stop > indent:
                    self._stop_price = new_stop

    def _handle_stops(self, candle):
        if self.Position > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(); self._reset_orders(); return
            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket(); self._reset_orders()
        elif self.Position < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(); self._reset_orders(); return
            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket(); self._reset_orders()

    def _reset_orders(self):
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0.0

    def _calculate_pip_size(self):
        sec = self.Security
        if sec is None:
            return 0.0
        step = float(sec.PriceStep) if sec.PriceStep is not None else 0.0
        if step == 0:
            return 0.0
        decimals = sec.Decimals if sec.Decimals is not None else 0
        return step * 10.0 if (decimals == 3 or decimals == 5) else step

    def OnReseted(self):
        super(js_sistem2_strategy, self).OnReseted()
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0.0

    def CreateClone(self):
        return js_sistem2_strategy()
