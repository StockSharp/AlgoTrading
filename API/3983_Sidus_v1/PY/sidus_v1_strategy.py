import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class sidus_v1_strategy(Strategy):
    def __init__(self):
        super(sidus_v1_strategy, self).__init__()
        self._fast_ema_len = self.Param("FastEmaLength", 23).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA for buy", "Indicators")
        self._slow_ema_len = self.Param("SlowEmaLength", 62).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA for buy", "Indicators")
        self._fast_ema2_len = self.Param("FastEma2Length", 18).SetGreaterThanZero().SetDisplay("Fast EMA (Sell)", "Fast EMA for sell", "Indicators")
        self._slow_ema2_len = self.Param("SlowEma2Length", 54).SetGreaterThanZero().SetDisplay("Slow EMA (Sell)", "Slow EMA for sell", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 67).SetGreaterThanZero().SetDisplay("RSI Period", "RSI for buy", "Indicators")
        self._rsi_period2 = self.Param("RsiPeriod2", 97).SetGreaterThanZero().SetDisplay("RSI Period (Sell)", "RSI for sell", "Indicators")
        self._buy_diff = self.Param("BuyDifferenceThreshold", -100.0).SetDisplay("Buy EMA Threshold", "Max fast-slow EMA diff for buy", "Trading")
        self._buy_rsi_thresh = self.Param("BuyRsiThreshold", 45.0).SetDisplay("Buy RSI Threshold", "Max RSI for buy", "Trading")
        self._sell_diff = self.Param("SellDifferenceThreshold", 100.0).SetDisplay("Sell EMA Threshold", "Min fast-slow EMA diff for sell", "Trading")
        self._sell_rsi_thresh = self.Param("SellRsiThreshold", 55.0).SetDisplay("Sell RSI Threshold", "Min RSI for sell", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnStarted(self, time):
        super(sidus_v1_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_len.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_len.Value
        fast_ema2 = ExponentialMovingAverage()
        fast_ema2.Length = self._fast_ema2_len.Value
        slow_ema2 = ExponentialMovingAverage()
        slow_ema2.Length = self._slow_ema2_len.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        rsi2 = RelativeStrengthIndex()
        rsi2.Length = self._rsi_period2.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, fast_ema2, slow_ema2, rsi, rsi2, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, fast2_val, slow2_val, rsi_val, rsi2_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        diff_buy = fast_val - slow_val
        diff_sell = fast2_val - slow2_val

        if diff_buy < self._buy_diff.Value and rsi_val < self._buy_rsi_thresh.Value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif diff_sell > self._sell_diff.Value and rsi2_val > self._sell_rsi_thresh.Value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return sidus_v1_strategy()
