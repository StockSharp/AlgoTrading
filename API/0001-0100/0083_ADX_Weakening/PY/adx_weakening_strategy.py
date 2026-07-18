import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class adx_weakening_strategy(Strategy):
    """
    ADX Weakening strategy.
    Enters when ADX is decreasing (trend weakening) using price vs SMA for direction.
    ADX weakening + price above SMA = buy.
    ADX weakening + price below SMA = sell.
    Exits on SMA cross.
    """

    def __init__(self):
        super(adx_weakening_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14).SetDisplay("ADX Period", "Period for ADX", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_adx = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_weakening_strategy, self).OnReseted()
        self._prev_adx = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(adx_weakening_strategy, self).OnStarted2(time)

        self._prev_adx = 0.0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_iv, adx_iv):
        if candle.State != CandleStates.Finished:
            return

        if not adx_iv.IsFormed or not sma_iv.IsFormed:
            return

        sma_value = float(sma_iv.Value)

        adx_ma = adx_iv.MovingAverage
        if adx_ma is None:
            return
        adx_value = float(adx_ma)

        if self._prev_adx == 0:
            self._prev_adx = adx_value
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_adx = adx_value
            return

        is_weakening = adx_value < self._prev_adx
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and is_weakening and close > sma_value:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and is_weakening and close < sma_value:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < sma_value:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sma_value:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_adx = adx_value

    def CreateClone(self):
        return adx_weakening_strategy()
