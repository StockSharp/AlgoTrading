import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class engulfing_bullish_strategy(Strategy):
    """
    Bullish Engulfing strategy.
    Enters long on bullish engulfing pattern below SMA.
    Enters short on bearish engulfing pattern above SMA.
    Exits via SMA crossover.
    """

    def __init__(self):
        super(engulfing_bullish_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._previous_candle = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(engulfing_bullish_strategy, self).OnReseted()
        self._previous_candle = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(engulfing_bullish_strategy, self).OnStarted(time)

        self._previous_candle = None
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_candle = candle
            return

        if self._previous_candle is not None:
            prev_bearish = self._previous_candle.ClosePrice < self._previous_candle.OpenPrice
            prev_bullish = self._previous_candle.ClosePrice > self._previous_candle.OpenPrice
            curr_bullish = candle.ClosePrice > candle.OpenPrice
            curr_bearish = candle.ClosePrice < candle.OpenPrice

            bullish_engulfing = prev_bearish and curr_bullish and candle.ClosePrice > self._previous_candle.OpenPrice and candle.OpenPrice < self._previous_candle.ClosePrice
            bearish_engulfing = prev_bullish and curr_bearish and candle.ClosePrice < self._previous_candle.OpenPrice and candle.OpenPrice > self._previous_candle.ClosePrice

            sv = float(sma_val)
            close = float(candle.ClosePrice)
            cd = self._cooldown_bars.Value

            if self.Position == 0 and bullish_engulfing and close < sv:
                self.BuyMarket()
                self._cooldown = cd
            elif self.Position == 0 and bearish_engulfing and close > sv:
                self.SellMarket()
                self._cooldown = cd
            elif self.Position > 0 and close < sv:
                self.SellMarket()
                self._cooldown = cd
            elif self.Position < 0 and close > sv:
                self.BuyMarket()
                self._cooldown = cd

        self._previous_candle = candle

    def CreateClone(self):
        return engulfing_bullish_strategy()
