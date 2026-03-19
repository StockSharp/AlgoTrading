import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, AccumulationDistributionLine, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class pipsover_strategy(Strategy):
    def __init__(self):
        super(pipsover_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 20).SetGreaterThanZero().SetDisplay("SMA Length", "Simple moving average length", "Indicators")
        self._sl_points = self.Param("StopLossPoints", 65.0).SetGreaterThanZero().SetDisplay("Stop-Loss Points", "Stop-loss in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 100.0).SetGreaterThanZero().SetDisplay("Take-Profit Points", "Take-profit in price steps", "Risk")
        self._open_level = self.Param("OpenLevel", 20.0).SetGreaterThanZero().SetDisplay("Open Level", "Chaikin threshold for entries", "Chaikin")
        self._close_level = self.Param("CloseLevel", 30.0).SetGreaterThanZero().SetDisplay("Close Level", "Chaikin threshold for exits", "Chaikin")
        self._chaikin_fast = self.Param("ChaikinFastLength", 3).SetGreaterThanZero().SetDisplay("Chaikin Fast", "Fast EMA for Chaikin", "Chaikin")
        self._chaikin_slow = self.Param("ChaikinSlowLength", 10).SetGreaterThanZero().SetDisplay("Chaikin Slow", "Slow EMA for Chaikin", "Chaikin")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(pipsover_strategy, self).OnReseted()
        self._has_prev = False
        self._prev_open = 0
        self._prev_high = 0
        self._prev_low = 0
        self._prev_close = 0
        self._prev_sma = 0
        self._prev_chaikin = 0
        self._stop_price = 0
        self._tp_price = 0
        self._has_targets = False

    def OnStarted(self, time):
        super(pipsover_strategy, self).OnStarted(time)
        self._has_prev = False
        self._prev_open = 0
        self._prev_high = 0
        self._prev_low = 0
        self._prev_close = 0
        self._prev_sma = 0
        self._prev_chaikin = 0
        self._stop_price = 0
        self._tp_price = 0
        self._has_targets = False

        self._sma = SimpleMovingAverage()
        self._sma.Length = self._ma_length.Value
        self._adl = AccumulationDistributionLine()
        self._ema_fast = ExponentialMovingAverage()
        self._ema_fast.Length = self._chaikin_fast.Value
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = self._chaikin_slow.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._adl, self._sma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def _get_step(self):
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            return float(self.Security.PriceStep)
        return 1.0

    def OnProcess(self, candle, adl_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        fast_res = self._ema_fast.Process(DecimalIndicatorValue(self._ema_fast, adl_val, candle.OpenTime))
        slow_res = self._ema_slow.Process(DecimalIndicatorValue(self._ema_slow, adl_val, candle.OpenTime))
        chaikin = fast_res.ToDecimal() - slow_res.ToDecimal()

        if not self._ema_fast.IsFormed or not self._ema_slow.IsFormed or not self._sma.IsFormed:
            self._update_state(candle, chaikin, sma_val)
            return

        if not self._has_prev:
            self._update_state(candle, chaikin, sma_val)
            return

        step = self._get_step()
        sl_dist = self._sl_points.Value * step
        tp_dist = self._tp_points.Value * step

        if self._has_targets:
            if self.Position > 0:
                if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._tp_price:
                    self.SellMarket()
                    self._reset_targets()
            elif self.Position < 0:
                if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._tp_price:
                    self.BuyMarket()
                    self._reset_targets()
            elif self.Position == 0:
                self._reset_targets()

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._update_state(candle, chaikin, sma_val)
            return

        prev_bullish = self._prev_close > self._prev_open
        prev_bearish = self._prev_close < self._prev_open

        if self.Position > 0:
            if prev_bearish and self._prev_high > self._prev_sma and self._prev_chaikin > self._close_level.Value:
                self.SellMarket()
                self._reset_targets()
        elif self.Position < 0:
            if prev_bullish and self._prev_low < self._prev_sma and self._prev_chaikin < -self._close_level.Value:
                self.BuyMarket()
                self._reset_targets()
        else:
            allow_long = prev_bullish and self._prev_low < self._prev_sma and self._prev_chaikin < -self._open_level.Value
            allow_short = prev_bearish and self._prev_high > self._prev_sma and self._prev_chaikin > self._open_level.Value

            if allow_long:
                self.BuyMarket()
                entry = float(candle.ClosePrice)
                self._stop_price = entry - sl_dist
                self._tp_price = entry + tp_dist
                self._has_targets = True
            elif allow_short:
                self.SellMarket()
                entry = float(candle.ClosePrice)
                self._stop_price = entry + sl_dist
                self._tp_price = entry - tp_dist
                self._has_targets = True

        self._update_state(candle, chaikin, sma_val)

    def _update_state(self, candle, chaikin, sma_val):
        self._prev_open = float(candle.OpenPrice)
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_close = float(candle.ClosePrice)
        self._prev_sma = sma_val
        self._prev_chaikin = chaikin
        self._has_prev = True

    def _reset_targets(self):
        self._stop_price = 0
        self._tp_price = 0
        self._has_targets = False

    def CreateClone(self):
        return pipsover_strategy()
