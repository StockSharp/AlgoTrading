import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class atr_trailing_strategy(Strategy):
    """
    Strategy that uses ATR for trailing stop management.
    Enters positions using a simple moving average and manages exits with a dynamic
    trailing stop calculated as a multiple of ATR.
    """

    def __init__(self):
        super(atr_trailing_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0).SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop", "Risk")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation for entry", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._entry_price = 0.0
        self._trailing_stop_level = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_trailing_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._trailing_stop_level = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(atr_trailing_strategy, self).OnStarted2(time)

        self._entry_price = 0.0
        self._trailing_stop_level = 0.0
        self._cooldown = 0

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)
        sv = float(sma_val)
        close = float(candle.ClosePrice)
        trailing_dist = av * float(self._atr_multiplier.Value)
        cd = self._cooldown_bars.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if self.Position == 0:
            if close > sv:
                self.BuyMarket()
                self._entry_price = close
                self._trailing_stop_level = self._entry_price - trailing_dist
                self._cooldown = cd
            elif close < sv:
                self.SellMarket()
                self._entry_price = close
                self._trailing_stop_level = self._entry_price + trailing_dist
                self._cooldown = cd
        elif self.Position > 0:
            new_level = close - trailing_dist
            if new_level > self._trailing_stop_level:
                self._trailing_stop_level = new_level
            if float(candle.LowPrice) <= self._trailing_stop_level:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            new_level = close + trailing_dist
            if new_level < self._trailing_stop_level or self._trailing_stop_level == 0:
                self._trailing_stop_level = new_level
            if float(candle.HighPrice) >= self._trailing_stop_level:
                self.BuyMarket()
                self._cooldown = cd

    def CreateClone(self):
        return atr_trailing_strategy()
