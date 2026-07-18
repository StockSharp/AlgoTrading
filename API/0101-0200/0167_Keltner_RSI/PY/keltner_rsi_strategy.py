import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class keltner_rsi_strategy(Strategy):

    def __init__(self):
        super(keltner_rsi_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for EMA in Keltner Channels", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR in Keltner Channels", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        self._rsi_overbought_level = self.Param("RsiOverboughtLevel", 60.0) \
            .SetDisplay("RSI Overbought", "RSI level considered overbought", "Trading Levels")
        self._rsi_oversold_level = self.Param("RsiOversoldLevel", 40.0) \
            .SetDisplay("RSI Oversold", "RSI level considered oversold", "Trading Levels")
        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ema = None
        self._atr = None
        self._rsi = None
        self._cooldown = 0

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiOverboughtLevel(self):
        return self._rsi_overbought_level.Value

    @RsiOverboughtLevel.setter
    def RsiOverboughtLevel(self, value):
        self._rsi_overbought_level.Value = value

    @property
    def RsiOversoldLevel(self):
        return self._rsi_oversold_level.Value

    @RsiOversoldLevel.setter
    def RsiOversoldLevel(self, value):
        self._rsi_oversold_level.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(keltner_rsi_strategy, self).OnStarted2(time)

        self._cooldown = 0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(self._ema, self._atr, self._rsi, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ema_value, atr_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._atr.IsFormed or not self._rsi.IsFormed:
            return

        ema_f = float(ema_value)
        atr_f = float(atr_value)
        rsi_f = float(rsi_value)
        close = float(candle.ClosePrice)
        cooldown_bars = int(self.CooldownBars)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if close < ema_f and rsi_f < 45.0 and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cooldown_bars
        elif close > ema_f and rsi_f > 55.0 and self.Position == 0:
            self.SellMarket()
            self._cooldown = cooldown_bars
        elif self.Position > 0 and close >= ema_f and rsi_f > 50:
            self.SellMarket()
            self._cooldown = cooldown_bars
        elif self.Position < 0 and close <= ema_f and rsi_f < 50:
            self.BuyMarket()
            self._cooldown = cooldown_bars

    def OnReseted(self):
        super(keltner_rsi_strategy, self).OnReseted()
        self._ema = None
        self._atr = None
        self._rsi = None
        self._cooldown = 0

    def CreateClone(self):
        return keltner_rsi_strategy()
