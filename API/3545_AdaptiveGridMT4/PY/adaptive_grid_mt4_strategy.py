import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class adaptive_grid_mt4_strategy(Strategy):
    def __init__(self):
        super(adaptive_grid_mt4_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._atr_period = self.Param("AtrPeriod", 20)
        self._breakout_multiplier = self.Param("BreakoutMultiplier", 2.5)

        self._prev_close = 0.0
        self._prev_atr = 0.0
        self._has_prev = False
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def BreakoutMultiplier(self):
        return self._breakout_multiplier.Value

    @BreakoutMultiplier.setter
    def BreakoutMultiplier(self, value):
        self._breakout_multiplier.Value = value

    def OnReseted(self):
        super(adaptive_grid_mt4_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_atr = 0.0
        self._has_prev = False
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(adaptive_grid_mt4_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_atr = 0.0
        self._has_prev = False
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if atr_val <= 0:
            self._prev_close = close
            self._prev_atr = atr_val
            self._has_prev = True
            return

        # Check protective stops
        if self.Position > 0:
            if self._stop_price > 0 and low <= self._stop_price:
                self.SellMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
            elif self._take_profit_price > 0 and high >= self._take_profit_price:
                self.SellMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
        elif self.Position < 0:
            if self._stop_price > 0 and high >= self._stop_price:
                self.BuyMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
            elif self._take_profit_price > 0 and low <= self._take_profit_price:
                self.BuyMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0

        if not self._has_prev or self._prev_atr <= 0:
            self._prev_close = close
            self._prev_atr = atr_val
            self._has_prev = True
            return

        threshold = self._prev_atr * float(self.BreakoutMultiplier)

        # Breakout up
        if close > self._prev_close + threshold and self.Position <= 0:
            self.BuyMarket()
            self._stop_price = close - atr_val * 3.0
            self._take_profit_price = close + atr_val * 4.0
            self._entry_price = close
        # Breakout down
        elif close < self._prev_close - threshold and self.Position >= 0:
            self.SellMarket()
            self._stop_price = close + atr_val * 3.0
            self._take_profit_price = close - atr_val * 4.0
            self._entry_price = close

        self._prev_close = close
        self._prev_atr = atr_val

    def CreateClone(self):
        return adaptive_grid_mt4_strategy()
