import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_overbought_oversold_strategy(Strategy):
    """
    RSI Overbought/Oversold strategy.
    Buys when RSI is oversold, sells when RSI is overbought.
    """

    def __init__(self):
        super(rsi_overbought_oversold_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        self._overbought_level = self.Param("OverboughtLevel", 70).SetDisplay("Overbought Level", "RSI overbought threshold", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 30).SetDisplay("Oversold Level", "RSI oversold threshold", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_overbought_oversold_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(rsi_overbought_oversold_strategy, self).OnStarted2(time)

        self._cooldown = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        rv = float(rsi_val)
        ob = self._overbought_level.Value
        os_level = self._oversold_level.Value
        cd = self._cooldown_bars.Value

        if self.Position == 0 and rv <= os_level:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and rv >= ob:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and rv >= ob:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and rv <= os_level:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return rsi_overbought_oversold_strategy()
