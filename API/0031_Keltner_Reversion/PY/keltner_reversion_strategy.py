import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class keltner_reversion_strategy(Strategy):
    """
    Keltner Channel mean reversion strategy.
    Buys below lower band, sells above upper band, exits at EMA.
    """

    def __init__(self):
        super(keltner_reversion_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "Period for EMA calculation (middle band)", "Technical Parameters")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Technical Parameters")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel width", "Technical Parameters")
        self._stop_loss_atr = self.Param("StopLossAtrMultiplier", 2.0).SetDisplay("ATR Multiplier (Stop Loss)", "ATR multiplier for stop-loss calculation", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "Technical Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_reversion_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(keltner_reversion_strategy, self).OnStarted2(time)

        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        ev = float(ema_val)
        av = float(atr_val)
        mult = float(self._atr_multiplier.Value)
        upper = ev + av * mult
        lower = ev - av * mult
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        if self.Position == 0:
            if close < lower:
                self.BuyMarket()
                self._cooldown = cd
            elif close > upper:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if close > ev:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            if close < ev:
                self.BuyMarket()
                self._cooldown = cd

    def CreateClone(self):
        return keltner_reversion_strategy()
