import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class ma2_cci_ema_strategy(Strategy):
    """
    EMA crossover with CCI confirmation and ATR-based stop.
    """

    def __init__(self):
        super(ma2_cci_ema_strategy, self).__init__()
        self._fast_period = self.Param("FastMaPeriod", 10).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowMaPeriod", 37).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 39).SetDisplay("CCI Period", "CCI length", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 3).SetDisplay("ATR Period", "ATR length", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_cci = 0.0
        self._has_prev = False
        self._stop_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma2_cci_ema_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_cci = 0.0
        self._has_prev = False
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(ma2_cci_ema_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, cci, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, cci_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        f = float(fast_val)
        s = float(slow_val)
        c = float(cci_val)
        a = float(atr_val)
        if not self._has_prev:
            self._prev_fast = f
            self._prev_slow = s
            self._prev_cci = c
            self._has_prev = True
            return
        close = float(candle.ClosePrice)
        cross_up = self._prev_fast <= self._prev_slow and f > s
        cross_down = self._prev_fast >= self._prev_slow and f < s
        cci_up = self._prev_cci <= 0 and c > 0
        cci_down = self._prev_cci >= 0 and c < 0
        if self.Position != 0:
            if self.Position > 0:
                if self._stop_price > 0 and float(candle.LowPrice) <= self._stop_price:
                    self.SellMarket()
                    self._stop_price = 0
                elif cross_down:
                    self.SellMarket()
                    self._stop_price = 0
            elif self.Position < 0:
                if self._stop_price > 0 and float(candle.HighPrice) >= self._stop_price:
                    self.BuyMarket()
                    self._stop_price = 0
                elif cross_up:
                    self.BuyMarket()
                    self._stop_price = 0
        else:
            if cross_up and cci_up:
                self.BuyMarket()
                self._stop_price = close - a
            elif cross_down and cci_down:
                self.SellMarket()
                self._stop_price = close + a
        self._prev_fast = f
        self._prev_slow = s
        self._prev_cci = c

    def CreateClone(self):
        return ma2_cci_ema_strategy()
