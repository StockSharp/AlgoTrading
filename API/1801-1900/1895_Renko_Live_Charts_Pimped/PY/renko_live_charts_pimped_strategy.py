import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class renko_live_charts_pimped_strategy(Strategy):
    def __init__(self):
        super(renko_live_charts_pimped_strategy, self).__init__()
        self._box_size = self.Param("BoxSize", 1000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Box Size", "Renko brick size", "Renko")
        self._calculate_best_box_size = self.Param("CalculateBestBoxSize", False) \
            .SetDisplay("Use ATR Box", "Calculate brick size from ATR", "Renko")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR calculation period", "Renko")
        self._use_atr_ma = self.Param("UseAtrMa", False) \
            .SetDisplay("Smooth ATR", "Apply moving average on ATR", "Renko")
        self._atr_ma_period = self.Param("AtrMaPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR MA Period", "Moving average length for ATR", "Renko")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._atr = None
        self._atr_ma = None
        self._renko_price = 0.0
        self._prev_direction = 0
        self._dynamic_box_size = 0.0

    @property
    def box_size(self):
        return self._box_size.Value
    @property
    def calculate_best_box_size(self):
        return self._calculate_best_box_size.Value
    @property
    def atr_period(self):
        return self._atr_period.Value
    @property
    def use_atr_ma(self):
        return self._use_atr_ma.Value
    @property
    def atr_ma_period(self):
        return self._atr_ma_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(renko_live_charts_pimped_strategy, self).OnReseted()
        self._renko_price = 0.0
        self._prev_direction = 0
        self._dynamic_box_size = 0.0

    def OnStarted2(self, time):
        super(renko_live_charts_pimped_strategy, self).OnStarted2(time)
        self._dynamic_box_size = float(self.box_size)
        if self.calculate_best_box_size:
            self._atr = AverageTrueRange()
            self._atr.Length = self.atr_period
            self._atr_ma = SimpleMovingAverage()
            self._atr_ma.Length = self.atr_ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(None, None)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # update dynamic box size from ATR if enabled
        if self.calculate_best_box_size and self._atr is not None:
            atr_result = self._atr.Process(candle)
            if atr_result.IsFormed:
                atr_val = float(atr_result)
                if self.use_atr_ma and self._atr_ma is not None:
                    ma_result = self._atr_ma.Process(atr_val, candle.OpenTime, True)
                    if ma_result.IsFormed:
                        self._dynamic_box_size = float(ma_result)
                else:
                    self._dynamic_box_size = atr_val

        close = float(candle.ClosePrice)
        size = self._dynamic_box_size

        if size <= 0:
            return

        if self._renko_price == 0.0:
            self._renko_price = close
            return

        diff = close - self._renko_price
        if abs(diff) < size:
            return

        direction = 1 if diff > 0 else -1
        self._renko_price += direction * size

        # trade on direction change
        if self._prev_direction != 0 and direction != self._prev_direction:
            if direction > 0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif direction < 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        elif self._prev_direction == 0:
            if direction > 0:
                self.BuyMarket()
            elif direction < 0:
                self.SellMarket()

        self._prev_direction = direction

    def CreateClone(self):
        return renko_live_charts_pimped_strategy()
