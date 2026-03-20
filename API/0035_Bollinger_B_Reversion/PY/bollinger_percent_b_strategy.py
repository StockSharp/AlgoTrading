import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class bollinger_percent_b_strategy(Strategy):
    """
    Bollinger %B strategy.
    Buys when %B < 0 (below lower band), sells when %B > 1 (above upper band).
    """

    def __init__(self):
        super(bollinger_percent_b_strategy, self).__init__()
        self._bb_period = self.Param("BollingerPeriod", 20).SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        self._bb_deviation = self.Param("BollingerDeviation", 2.0).SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Indicators")
        self._exit_value = self.Param("ExitValue", 0.5).SetDisplay("Exit %B Value", "Exit threshold for %B", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_percent_b_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(bollinger_percent_b_strategy, self).OnStarted(time)

        self._cooldown = 0

        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = self._bb_deviation.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        if not bb_val.IsFormed:
            return

        if bb_val.UpBand is None or bb_val.LowBand is None:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        upper = float(bb_val.UpBand)
        lower = float(bb_val.LowBand)
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value
        exit_v = float(self._exit_value.Value)

        pct_b = 0.0
        if upper != lower:
            pct_b = (close - lower) / (upper - lower)

        if self.Position == 0:
            if pct_b < 0:
                self.BuyMarket()
                self._cooldown = cd
            elif pct_b > 1:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if pct_b > exit_v:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            if pct_b < exit_v:
                self.BuyMarket()
                self._cooldown = cd

    def CreateClone(self):
        return bollinger_percent_b_strategy()
