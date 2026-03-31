import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class breakouts_with_time_filter_strategy(Strategy):
    def __init__(self):
        super(breakouts_with_time_filter_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("Length", "Lookback period for breakout levels", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR stop", "Risk Management")
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetDisplay("Risk Reward", "Risk to reward ratio", "Risk Management")
        self._stop_level = 0.0
        self._target_level = 0.0
        self._highs = []
        self._lows = []

    @property
    def length(self):
        return self._length.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value
    @property
    def risk_reward(self):
        return self._risk_reward.Value

    def OnReseted(self):
        super(breakouts_with_time_filter_strategy, self).OnReseted()
        self._stop_level = 0.0
        self._target_level = 0.0
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(breakouts_with_time_filter_strategy, self).OnStarted2(time)
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        atr_val = float(atr_value)

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        if len(self._highs) > self.length + 1:
            self._highs.pop(0)
        if len(self._lows) > self.length + 1:
            self._lows.pop(0)

        if len(self._highs) <= self.length:
            return

        prev_highest = max(self._highs[:-1])
        prev_lowest = min(self._lows[:-1])
        close = float(candle.ClosePrice)

        if self.Position == 0:
            if close > prev_highest:
                self._stop_level = close - atr_val * float(self.atr_multiplier)
                stop_distance = close - self._stop_level
                self._target_level = close + float(self.risk_reward) * stop_distance
                self.BuyMarket()
            elif close < prev_lowest:
                self._stop_level = close + atr_val * float(self.atr_multiplier)
                stop_distance = self._stop_level - close
                self._target_level = close - float(self.risk_reward) * stop_distance
                self.SellMarket()
        elif self.Position > 0:
            if float(candle.LowPrice) <= self._stop_level or float(candle.HighPrice) >= self._target_level:
                self.SellMarket()
        elif self.Position < 0:
            if float(candle.HighPrice) >= self._stop_level or float(candle.LowPrice) <= self._target_level:
                self.BuyMarket()

    def CreateClone(self):
        return breakouts_with_time_filter_strategy()
