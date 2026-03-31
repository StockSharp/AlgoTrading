import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import OnBalanceVolume, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class obv_breakout_strategy(Strategy):
    """
    On-Balance Volume (OBV) Breakout strategy.
    Enters when OBV direction confirms price trend relative to SMA.
    """

    def __init__(self):
        super(obv_breakout_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for OBV moving average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_obv = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(obv_breakout_strategy, self).OnReseted()
        self._prev_obv = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(obv_breakout_strategy, self).OnStarted2(time)

        self._prev_obv = 0.0
        self._cooldown = 0

        obv = OnBalanceVolume()
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(obv, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, obv_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        ov = float(obv_val)

        if self._prev_obv == 0:
            self._prev_obv = ov
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_obv = ov
            return

        obv_rising = ov > self._prev_obv
        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0:
            if obv_rising and close > sv:
                self.BuyMarket()
                self._cooldown = cd
            elif not obv_rising and close < sv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and not obv_rising:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and obv_rising:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_obv = ov

    def CreateClone(self):
        return obv_breakout_strategy()
