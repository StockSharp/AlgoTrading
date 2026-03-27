import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, CommodityChannelIndex, WilliamsR,
    Highest, Lowest
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rabbit_m2_strategy(Strategy):
    def __init__(self):
        super(rabbit_m2_strategy, self).__init__()
        self._cci_sell_level = self.Param("CciSellLevel", 101).SetDisplay("CCI Sell Level", "CCI threshold for shorts", "CCI")
        self._cci_buy_level = self.Param("CciBuyLevel", 99).SetDisplay("CCI Buy Level", "CCI threshold for longs", "CCI")
        self._cci_period = self.Param("CciPeriod", 14).SetGreaterThanZero().SetDisplay("CCI Period", "CCI lookback", "CCI")
        self._donchian_period = self.Param("DonchianPeriod", 100).SetGreaterThanZero().SetDisplay("Donchian Period", "Donchian lookback", "Donchian")
        self._wpr_period = self.Param("WprPeriod", 50).SetGreaterThanZero().SetDisplay("Williams %R Period", "Williams %R lookback", "Momentum")
        self._fast_ema_period = self.Param("FastEmaPeriod", 40).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA for trend", "Trend Filter")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 80).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA for trend", "Trend Filter")
        self._tp_pips = self.Param("TakeProfitPips", 50).SetGreaterThanZero().SetDisplay("Take Profit (pips)", "TP distance", "Risk")
        self._sl_pips = self.Param("StopLossPips", 50).SetGreaterThanZero().SetDisplay("Stop Loss (pips)", "SL distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Primary timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rabbit_m2_strategy, self).OnReseted()
        self._buy_allowed = False
        self._sell_allowed = False
        self._prev_wpr = None
        self._prev_don_upper = None
        self._prev_don_lower = None
        self._current_stop = 0
        self._current_take = 0
        self._pip_size = 0

    def OnStarted(self, time):
        super(rabbit_m2_strategy, self).OnStarted(time)
        self._buy_allowed = False
        self._sell_allowed = False
        self._prev_wpr = None
        self._prev_don_upper = None
        self._prev_don_lower = None
        self._current_stop = 0
        self._current_take = 0
        self._pip_size = self._get_pip_size()

        self._sl_dist = self._sl_pips.Value * self._pip_size
        self._tp_dist = self._tp_pips.Value * self._pip_size

        wpr = WilliamsR()
        wpr.Length = self._wpr_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        don_high = Highest()
        don_high.Length = self._donchian_period.Value
        don_low = Lowest()
        don_low.Length = self._donchian_period.Value

        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self._fast_ema_period.Value
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self._slow_ema_period.Value

        trend_sub = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(4)))
        trend_sub.Bind(ema_fast, ema_slow, self._on_trend).Start()

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(wpr, cci, don_high, don_low, self._on_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def _get_pip_size(self):
        step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        return step

    def _on_trend(self, candle, ema_fast_val, ema_slow_val):
        if candle.State != CandleStates.Finished:
            return
        if ema_fast_val < ema_slow_val:
            self._sell_allowed = True
            self._buy_allowed = False
            if self.Position > 0:
                self.SellMarket()
                self._current_stop = 0
                self._current_take = 0
        elif ema_fast_val > ema_slow_val:
            self._buy_allowed = True
            self._sell_allowed = False
            if self.Position < 0:
                self.BuyMarket()
                self._current_stop = 0
                self._current_take = 0

    def _on_candle(self, candle, wpr_val, cci_val, don_high_val, don_low_val):
        if candle.State != CandleStates.Finished:
            return

        self._manage_position(candle, don_high_val, don_low_val)

        if self._prev_wpr is None:
            self._prev_wpr = wpr_val
            return

        wpr_current = wpr_val
        if wpr_current == 0:
            wpr_current = -1
        wpr_lag = self._prev_wpr
        if wpr_lag == 0:
            wpr_lag = -1
        self._prev_wpr = wpr_current

        close = candle.ClosePrice

        if self._sell_allowed and self.Position >= 0:
            if wpr_current < -20 and wpr_lag > -20 and wpr_lag < 0 and cci_val > self._cci_sell_level.Value:
                self.SellMarket()
                self._current_stop = close + self._sl_dist
                self._current_take = close - self._tp_dist
                return

        if self._buy_allowed and self.Position <= 0:
            if wpr_current > -80 and wpr_lag < -80 and wpr_lag < 0 and cci_val < self._cci_buy_level.Value:
                self.BuyMarket()
                self._current_stop = close - self._sl_dist
                self._current_take = close + self._tp_dist

    def _manage_position(self, candle, upper, lower):
        if self.Position > 0:
            if self._current_take > 0 and candle.HighPrice >= self._current_take:
                self.SellMarket()
                self._current_stop = 0
                self._current_take = 0
            elif self._current_stop > 0 and candle.LowPrice <= self._current_stop:
                self.SellMarket()
                self._current_stop = 0
                self._current_take = 0
            elif self._prev_don_lower is not None and candle.ClosePrice < self._prev_don_lower:
                self.SellMarket()
                self._current_stop = 0
                self._current_take = 0
        elif self.Position < 0:
            if self._current_take > 0 and candle.LowPrice <= self._current_take:
                self.BuyMarket()
                self._current_stop = 0
                self._current_take = 0
            elif self._current_stop > 0 and candle.HighPrice >= self._current_stop:
                self.BuyMarket()
                self._current_stop = 0
                self._current_take = 0
            elif self._prev_don_upper is not None and candle.ClosePrice > self._prev_don_upper:
                self.BuyMarket()
                self._current_stop = 0
                self._current_take = 0

        self._prev_don_upper = upper
        self._prev_don_lower = lower

    def CreateClone(self):
        return rabbit_m2_strategy()
