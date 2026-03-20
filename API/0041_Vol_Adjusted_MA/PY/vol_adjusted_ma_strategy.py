import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class vol_adjusted_ma_strategy(Strategy):
    """
    Vol Adjusted MA strategy.
    Enters long when price is above MA + k*ATR, short when below MA - k*ATR.
    Exits when price returns to MA.
    """

    def __init__(self):
        super(vol_adjusted_ma_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._atr_period = self.Param("ATRPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_multiplier = self.Param("ATRMultiplier", 2.0).SetDisplay("ATR Multiplier", "Multiplier for ATR to adjust MA bands", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vol_adjusted_ma_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(vol_adjusted_ma_strategy, self).OnStarted(time)

        self._cooldown = 0

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        mv = float(ma_val)
        av = float(atr_val)
        mult = float(self._atr_multiplier.Value)
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        upper_band = mv + mult * av
        lower_band = mv - mult * av

        if self.Position == 0:
            if close > upper_band:
                self.BuyMarket()
                self._cooldown = cd
            elif close < lower_band:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and close < mv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > mv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return vol_adjusted_ma_strategy()
