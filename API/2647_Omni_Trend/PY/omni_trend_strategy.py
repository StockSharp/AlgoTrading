import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, AverageTrueRange, CandleIndicatorValue, DecimalIndicatorValue
)


class omni_trend_strategy(Strategy):
    """Trend-following strategy replicating the Omni Trend MetaTrader expert."""

    def __init__(self):
        super(omni_trend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used to build Omni Trend signals", "General")
        self._ma_length = self.Param("MaLength", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Moving average period", "Indicators")
        self._atr_length = self.Param("AtrLength", 11) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period for volatility bands", "Indicators")
        self._volatility_factor = self.Param("VolatilityFactor", 1.3) \
            .SetGreaterThanZero() \
            .SetDisplay("Volatility Factor", "Multiplier applied to ATR", "Indicators")
        self._money_risk = self.Param("MoneyRisk", 0.15) \
            .SetGreaterThanZero() \
            .SetDisplay("Money Risk", "Offset factor used to position trend bands", "Indicators")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Delay in bars before acting on a signal", "Trading")
        self._enable_buy_open = self.Param("EnableBuyOpen", True) \
            .SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading")
        self._enable_sell_open = self.Param("EnableSellOpen", True) \
            .SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading")
        self._enable_buy_close = self.Param("EnableBuyClose", True) \
            .SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading")
        self._enable_sell_close = self.Param("EnableSellClose", True) \
            .SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit (points)", "Profit target distance expressed in price steps", "Risk")

        self._prev_smin = 0.0
        self._prev_smax = 0.0
        self._prev_trend_up = 0.0
        self._prev_trend_down = 0.0
        self._prev_trend = 0
        self._is_initialized = False
        self._long_entry_price = None
        self._short_entry_price = None
        self._pending_signals = []

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def MaLength(self):
        return self._ma_length.Value
    @property
    def AtrLength(self):
        return self._atr_length.Value
    @property
    def VolatilityFactor(self):
        return self._volatility_factor.Value
    @property
    def MoneyRisk(self):
        return self._money_risk.Value
    @property
    def SignalBar(self):
        return max(0, int(self._signal_bar.Value))
    @property
    def EnableBuyOpen(self):
        return self._enable_buy_open.Value
    @property
    def EnableSellOpen(self):
        return self._enable_sell_open.Value
    @property
    def EnableBuyClose(self):
        return self._enable_buy_close.Value
    @property
    def EnableSellClose(self):
        return self._enable_sell_close.Value
    @property
    def StopLossPoints(self):
        return max(0, int(self._stop_loss_points.Value))
    @property
    def TakeProfitPoints(self):
        return max(0, int(self._take_profit_points.Value))

    def OnStarted2(self, time):
        super(omni_trend_strategy, self).OnStarted2(time)

        self._prev_smin = 0.0
        self._prev_smax = 0.0
        self._prev_trend_up = 0.0
        self._prev_trend_down = 0.0
        self._prev_trend = 0
        self._is_initialized = False
        self._long_entry_price = None
        self._short_entry_price = None
        self._pending_signals = []

        self._ma = ExponentialMovingAverage()
        self._ma.Length = self.MaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        atr_v = float(atr_val)
        close = float(candle.ClosePrice)

        ma_result = self._ma.Process(DecimalIndicatorValue(self._ma, close, candle.OpenTime))
        if not ma_result.IsFinal:
            return

        self._check_risk(candle)

        ma_v = float(ma_result.GetValue[float]())
        vf = float(self.VolatilityFactor)
        mr = float(self.MoneyRisk)

        smax = ma_v + vf * atr_v
        smin = ma_v - vf * atr_v

        if not self._is_initialized:
            self._prev_smax = smax
            self._prev_smin = smin
            self._prev_trend_up = 0.0
            self._prev_trend_down = 0.0
            self._prev_trend = 0
            self._is_initialized = True
            return

        trend = self._prev_trend
        if float(candle.HighPrice) > self._prev_smax:
            trend = 1
        elif float(candle.LowPrice) < self._prev_smin:
            trend = -1

        trend_up = None
        trend_down = None

        if trend > 0:
            if smin < self._prev_smin:
                smin = self._prev_smin
            candidate = smin - (mr - 1.0) * atr_v
            if self._prev_trend > 0 and self._prev_trend_up > 0.0 and candidate < self._prev_trend_up:
                candidate = self._prev_trend_up
            trend_up = candidate
        elif trend < 0:
            if smax > self._prev_smax:
                smax = self._prev_smax
            candidate = smax + (mr - 1.0) * atr_v
            if self._prev_trend < 0 and self._prev_trend_down > 0.0 and candidate > self._prev_trend_down:
                candidate = self._prev_trend_down
            trend_down = candidate

        sig = [False, False, False, False]  # buy_open, buy_close, sell_open, sell_close

        if trend > 0:
            if self._prev_trend <= 0 and trend_up is not None and self.EnableBuyOpen:
                sig[0] = True
            if trend_up is not None and self.EnableSellClose:
                sig[3] = True
        elif trend < 0:
            if self._prev_trend >= 0 and trend_down is not None and self.EnableSellOpen:
                sig[2] = True
            if trend_down is not None and self.EnableBuyClose:
                sig[1] = True

        self._prev_trend = trend
        self._prev_smax = smax
        self._prev_smin = smin
        self._prev_trend_up = trend_up if trend_up is not None else 0.0
        self._prev_trend_down = trend_down if trend_down is not None else 0.0

        self._pending_signals.append(sig)
        while len(self._pending_signals) > self.SignalBar:
            pending = self._pending_signals.pop(0)
            self._execute_signal(candle, pending)

    def _execute_signal(self, candle, sig):
        buy_open, buy_close, sell_open, sell_close = sig

        if buy_close and self.Position > 0:
            self.SellMarket()
            self._long_entry_price = None

        if sell_close and self.Position < 0:
            self.BuyMarket()
            self._short_entry_price = None

        exec_price = float(candle.ClosePrice) if self.SignalBar == 0 else float(candle.OpenPrice)

        if buy_open and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
                self._short_entry_price = None
            self.BuyMarket()
            self._long_entry_price = exec_price

        if sell_open and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
                self._long_entry_price = None
            self.SellMarket()
            self._short_entry_price = exec_price

    def _check_risk(self, candle):
        sec = self.Security
        if sec is None:
            return
        step = float(sec.PriceStep) if sec.PriceStep is not None else 0.01
        if step <= 0:
            return

        if self.Position > 0:
            if self.StopLossPoints > 0 and self._long_entry_price is not None:
                stop_price = self._long_entry_price - self.StopLossPoints * step
                if float(candle.LowPrice) <= stop_price or float(candle.ClosePrice) <= stop_price:
                    self.SellMarket()
                    self._long_entry_price = None
                    return
            if self.TakeProfitPoints > 0 and self._long_entry_price is not None:
                target = self._long_entry_price + self.TakeProfitPoints * step
                if float(candle.HighPrice) >= target or float(candle.ClosePrice) >= target:
                    self.SellMarket()
                    self._long_entry_price = None
                    return
        elif self.Position < 0:
            if self.StopLossPoints > 0 and self._short_entry_price is not None:
                stop_price = self._short_entry_price + self.StopLossPoints * step
                if float(candle.HighPrice) >= stop_price or float(candle.ClosePrice) >= stop_price:
                    self.BuyMarket()
                    self._short_entry_price = None
                    return
            if self.TakeProfitPoints > 0 and self._short_entry_price is not None:
                target = self._short_entry_price - self.TakeProfitPoints * step
                if float(candle.LowPrice) <= target or float(candle.ClosePrice) <= target:
                    self.BuyMarket()
                    self._short_entry_price = None
                    return

    def OnReseted(self):
        super(omni_trend_strategy, self).OnReseted()
        self._prev_smin = 0.0
        self._prev_smax = 0.0
        self._prev_trend_up = 0.0
        self._prev_trend_down = 0.0
        self._prev_trend = 0
        self._is_initialized = False
        self._long_entry_price = None
        self._short_entry_price = None
        self._pending_signals = []

    def CreateClone(self):
        return omni_trend_strategy()
