import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class triple_ma_htf_dynamic_smoothing_strategy(Strategy):
    """EMA crossover with RSI confirmation and re-entry on RSI crossing 50."""
    def __init__(self):
        super(triple_ma_htf_dynamic_smoothing_strategy, self).__init__()
        self._len1 = self.Param("Length1", 10).SetDisplay("MA1 Length", "Fast EMA", "Trend")
        self._len2 = self.Param("Length2", 30).SetDisplay("MA2 Length", "Slow EMA", "Trend")
        self._len3 = self.Param("Length3", 14).SetDisplay("RSI Length", "RSI period", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(triple_ma_htf_dynamic_smoothing_strategy, self).OnReseted()
        self._prev_ma1 = 0
        self._prev_ma2 = 0
        self._prev_rsi = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(triple_ma_htf_dynamic_smoothing_strategy, self).OnStarted2(time)
        self._prev_ma1 = 0
        self._prev_ma2 = 0
        self._prev_rsi = 0
        self._cooldown = 0

        ema1 = ExponentialMovingAverage()
        ema1.Length = self._len1.Value
        ema2 = ExponentialMovingAverage()
        ema2.Length = self._len2.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._len3.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema1, ema2, rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema1)
            self.DrawIndicator(area, ema2)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma1, ma2, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        ma1 = float(ma1)
        ma2 = float(ma2)
        rsi_val = float(rsi_val)

        if self._prev_ma1 == 0 or self._prev_ma2 == 0 or self._prev_rsi == 0:
            self._prev_ma1 = ma1
            self._prev_ma2 = ma2
            self._prev_rsi = rsi_val
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ma1 = ma1
            self._prev_ma2 = ma2
            self._prev_rsi = rsi_val
            return

        cross_up = self._prev_ma1 <= self._prev_ma2 and ma1 > ma2
        cross_down = self._prev_ma1 >= self._prev_ma2 and ma1 < ma2
        trend_up = ma1 > ma2
        trend_down = ma1 < ma2

        # Exit on opposite cross
        if self.Position > 0 and cross_down:
            self.SellMarket()
            self._cooldown = 30
        elif self.Position < 0 and cross_up:
            self.BuyMarket()
            self._cooldown = 30

        # Entry
        if self.Position == 0:
            if cross_up and rsi_val > 45 and rsi_val < 75:
                self.BuyMarket()
                self._cooldown = 30
            elif cross_down and rsi_val > 25 and rsi_val < 55:
                self.SellMarket()
                self._cooldown = 30
            elif trend_up and self._prev_rsi <= 50 and rsi_val > 50:
                self.BuyMarket()
                self._cooldown = 30
            elif trend_down and self._prev_rsi >= 50 and rsi_val < 50:
                self.SellMarket()
                self._cooldown = 30

        self._prev_ma1 = ma1
        self._prev_ma2 = ma2
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return triple_ma_htf_dynamic_smoothing_strategy()
