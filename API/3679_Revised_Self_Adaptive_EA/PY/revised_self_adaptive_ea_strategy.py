import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class revised_self_adaptive_ea_strategy(Strategy):
    def __init__(self):
        super(revised_self_adaptive_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))
        self._moving_average_period = self.Param("MovingAveragePeriod", 2)
        self._rsi_period = self.Param("RsiPeriod", 6)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0)
        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 4.0)
        self._trailing_stop_atr_multiplier = self.Param("TrailingStopAtrMultiplier", 1.5)
        self._oversold_level = self.Param("OversoldLevel", 40.0)
        self._overbought_level = self.Param("OverboughtLevel", 60.0)

        self._prev_candle_close = None
        self._prev_candle_open = None
        self._last_atr_value = 0.0
        self._entry_price = 0.0
        self._direction = 0
        self._stop_price = None
        self._take_price = None
        self._trailing_stop = None
        self._body_values = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MovingAveragePeriod(self):
        return self._moving_average_period.Value

    @MovingAveragePeriod.setter
    def MovingAveragePeriod(self, value):
        self._moving_average_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def StopLossAtrMultiplier(self):
        return self._stop_loss_atr_multiplier.Value

    @StopLossAtrMultiplier.setter
    def StopLossAtrMultiplier(self, value):
        self._stop_loss_atr_multiplier.Value = value

    @property
    def TakeProfitAtrMultiplier(self):
        return self._take_profit_atr_multiplier.Value

    @TakeProfitAtrMultiplier.setter
    def TakeProfitAtrMultiplier(self, value):
        self._take_profit_atr_multiplier.Value = value

    @property
    def TrailingStopAtrMultiplier(self):
        return self._trailing_stop_atr_multiplier.Value

    @TrailingStopAtrMultiplier.setter
    def TrailingStopAtrMultiplier(self, value):
        self._trailing_stop_atr_multiplier.Value = value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversold_level.Value = value

    @property
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overbought_level.Value = value

    def OnReseted(self):
        super(revised_self_adaptive_ea_strategy, self).OnReseted()
        self._prev_candle_close = None
        self._prev_candle_open = None
        self._last_atr_value = 0.0
        self._entry_price = 0.0
        self._direction = 0
        self._stop_price = None
        self._take_price = None
        self._trailing_stop = None
        self._body_values = []

    def OnStarted(self, time):
        super(revised_self_adaptive_ea_strategy, self).OnStarted(time)
        self._prev_candle_close = None
        self._prev_candle_open = None
        self._last_atr_value = 0.0
        self._entry_price = 0.0
        self._direction = 0
        self._stop_price = None
        self._take_price = None
        self._trailing_stop = None
        self._body_values = []

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        ma = SimpleMovingAverage()
        ma.Length = self.MovingAveragePeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ma, atr, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value, ma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        ma_val = float(ma_value)
        atr_val = float(atr_value)
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        self._last_atr_value = atr_val

        # Track average body size
        body = abs(close - open_p)
        self._body_values.append(body)
        while len(self._body_values) > 3:
            self._body_values.pop(0)
        avg_body = sum(self._body_values) / len(self._body_values) if self._body_values else 0.0

        # Manage open positions
        if self._direction > 0 and self.Position > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._direction = 0
                self._prev_candle_close = close
                self._prev_candle_open = open_p
                return
            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._direction = 0
                self._prev_candle_close = close
                self._prev_candle_open = open_p
                return
            # Trailing stop
            if self._trailing_stop is not None and atr_val > 0:
                candidate = close - atr_val * float(self.TrailingStopAtrMultiplier)
                if candidate > self._trailing_stop:
                    self._trailing_stop = candidate
                if low <= self._trailing_stop:
                    self.SellMarket()
                    self._direction = 0
                    self._prev_candle_close = close
                    self._prev_candle_open = open_p
                    return
        elif self._direction < 0 and self.Position < 0:
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._direction = 0
                self._prev_candle_close = close
                self._prev_candle_open = open_p
                return
            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._direction = 0
                self._prev_candle_close = close
                self._prev_candle_open = open_p
                return
            if self._trailing_stop is not None and atr_val > 0:
                candidate = close + atr_val * float(self.TrailingStopAtrMultiplier)
                if candidate < self._trailing_stop:
                    self._trailing_stop = candidate
                if high >= self._trailing_stop:
                    self.BuyMarket()
                    self._direction = 0
                    self._prev_candle_close = close
                    self._prev_candle_open = open_p
                    return

        # Check for engulfing patterns
        if self._prev_candle_close is not None and self._prev_candle_open is not None and atr_val > 0:
            # Bullish engulfing
            bullish = (close > open_p and
                       self._prev_candle_close < self._prev_candle_open and
                       open_p <= self._prev_candle_close and
                       body >= avg_body)
            # Bearish engulfing
            bearish = (close < open_p and
                       self._prev_candle_close > self._prev_candle_open and
                       open_p >= self._prev_candle_close and
                       body >= avg_body)

            if bullish and rsi_val <= float(self.OversoldLevel) and close >= ma_val:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0:
                    self.BuyMarket()
                    self._entry_price = close
                    self._direction = 1
                    sl_mult = float(self.StopLossAtrMultiplier)
                    tp_mult = float(self.TakeProfitAtrMultiplier)
                    trail_mult = float(self.TrailingStopAtrMultiplier)
                    self._stop_price = close - atr_val * sl_mult if sl_mult > 0 else None
                    self._take_price = close + atr_val * tp_mult if tp_mult > 0 else None
                    self._trailing_stop = close - atr_val * trail_mult if trail_mult > 0 else None
            elif bearish and rsi_val >= float(self.OverboughtLevel) and close <= ma_val:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self.SellMarket()
                    self._entry_price = close
                    self._direction = -1
                    sl_mult = float(self.StopLossAtrMultiplier)
                    tp_mult = float(self.TakeProfitAtrMultiplier)
                    trail_mult = float(self.TrailingStopAtrMultiplier)
                    self._stop_price = close + atr_val * sl_mult if sl_mult > 0 else None
                    self._take_price = close - atr_val * tp_mult if tp_mult > 0 else None
                    self._trailing_stop = close + atr_val * trail_mult if trail_mult > 0 else None

        self._prev_candle_close = close
        self._prev_candle_open = open_p

    def CreateClone(self):
        return revised_self_adaptive_ea_strategy()
