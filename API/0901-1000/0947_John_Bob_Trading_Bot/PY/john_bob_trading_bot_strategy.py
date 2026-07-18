import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class john_bob_trading_bot_strategy(Strategy):
    def __init__(self):
        super(john_bob_trading_bot_strategy, self).__init__()
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Mult", "ATR stop multiplier", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev2_high = 0.0
        self._prev2_low = 0.0
        self._highest_high = 0.0
        self._lowest_low = 999999999.0
        self._bar_count = 0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._target_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(john_bob_trading_bot_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev2_high = 0.0
        self._prev2_low = 0.0
        self._highest_high = 0.0
        self._lowest_low = 999999999.0
        self._bar_count = 0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._target_price = 0.0

    def OnStarted2(self, time):
        super(john_bob_trading_bot_strategy, self).OnStarted2(time)
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        atr_v = float(atr_val)
        mult = float(self._atr_multiplier.Value)
        if high > self._highest_high:
            self._highest_high = high
        if low < self._lowest_low:
            self._lowest_low = low
        if self._bar_count < 50:
            self._prev2_high = self._prev_high
            self._prev2_low = self._prev_low
            self._prev_high = high
            self._prev_low = low
            self._prev_close = close
            return
        fvg_up = self._prev2_low > high
        fvg_down = self._prev2_high < low
        cross_up = self._prev_close <= self._lowest_low and close > self._lowest_low
        cross_down = self._prev_close >= self._highest_high and close < self._highest_high
        buy_signal = cross_up or fvg_up
        sell_signal = cross_down or fvg_down
        if self.Position > 0 and self._stop_price > 0 and self._target_price > 0:
            if low <= self._stop_price or high >= self._target_price:
                self.SellMarket()
                self._stop_price = 0.0
                self._target_price = 0.0
        elif self.Position < 0 and self._stop_price > 0 and self._target_price > 0:
            if high >= self._stop_price or low <= self._target_price:
                self.BuyMarket()
                self._stop_price = 0.0
                self._target_price = 0.0
        if self.Position == 0:
            if buy_signal:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - atr_v * mult
                self._target_price = close + atr_v * mult * 2.0
            elif sell_signal:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + atr_v * mult
                self._target_price = close - atr_v * mult * 2.0
        self._prev2_high = self._prev_high
        self._prev2_low = self._prev_low
        self._prev_high = high
        self._prev_low = low
        self._prev_close = close

    def CreateClone(self):
        return john_bob_trading_bot_strategy()
