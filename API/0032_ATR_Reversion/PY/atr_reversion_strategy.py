import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class atr_reversion_strategy(Strategy):
    """
    ATR Reversion strategy. Trades when price moves N*ATR in one direction.
    """

    def __init__(self):
        super(atr_reversion_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetDisplay("ATR Multiplier", "ATR multiplier for entry signal", "Entry")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for MA calculation for exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_close = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_reversion_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(atr_reversion_strategy, self).OnStarted(time)

        self._prev_close = 0.0
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
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)

        if self._prev_close == 0:
            self._prev_close = close
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            return

        av = float(atr_val)
        sv = float(sma_val)
        mult = float(self._atr_multiplier.Value)
        cd = self._cooldown_bars.Value

        norm = 0.0
        if av > 0:
            norm = (close - self._prev_close) / av

        if self.Position == 0:
            if norm < -mult:
                self.BuyMarket()
                self._cooldown = cd
            elif norm > mult:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if close > sv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            if close < sv:
                self.BuyMarket()
                self._cooldown = cd

        self._prev_close = close

    def CreateClone(self):
        return atr_reversion_strategy()
