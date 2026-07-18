import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage


class straddle_trail_strategy(Strategy):
    def __init__(self):
        super(straddle_trail_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR calculation length", "ATR")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Breakout distance multiplier", "ATR")

        self._stop_loss_mult = self.Param("StopLossMult", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("SL Multiplier", "Stop loss as ATR multiple", "Risk")

        self._take_profit_mult = self.Param("TakeProfitMult", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("TP Multiplier", "Take profit as ATR multiple", "Risk")

        self._trail_mult = self.Param("TrailMult", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Trail Multiplier", "Trailing distance as ATR multiple", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown", "Bars to wait after exit", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle subscription", "General")

        self._entry_price = 0.0
        self._stop_level = None
        self._take_level = None
        self._bars_since_entry = 0
        self._cooldown_counter = 0

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
    def StopLossMult(self):
        return self._stop_loss_mult.Value

    @StopLossMult.setter
    def StopLossMult(self, value):
        self._stop_loss_mult.Value = value

    @property
    def TakeProfitMult(self):
        return self._take_profit_mult.Value

    @TakeProfitMult.setter
    def TakeProfitMult(self, value):
        self._take_profit_mult.Value = value

    @property
    def TrailMult(self):
        return self._trail_mult.Value

    @TrailMult.setter
    def TrailMult(self, value):
        self._trail_mult.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(straddle_trail_strategy, self).OnStarted2(time)

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        sma = SimpleMovingAverage()
        sma.Length = 20

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(atr, sma, self.process_candle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        atr = float(atr_val)
        sma = float(sma_val)

        if self.Position != 0:
            self._bars_since_entry += 1

            if self.Position > 0:
                new_trail = close - float(self.TrailMult) * atr
                if self._stop_level is None or new_trail > self._stop_level:
                    self._stop_level = new_trail

                if close <= self._stop_level or (self._take_level is not None and close >= self._take_level):
                    self.SellMarket(abs(self.Position))
                    self._reset_position()
                    return
            else:
                new_trail = close + float(self.TrailMult) * atr
                if self._stop_level is None or new_trail < self._stop_level:
                    self._stop_level = new_trail

                if close >= self._stop_level or (self._take_level is not None and close <= self._take_level):
                    self.BuyMarket(abs(self.Position))
                    self._reset_position()
                    return

            return

        if self._cooldown_counter > 0:
            self._cooldown_counter -= 1
            return

        upper_level = sma + float(self.AtrMultiplier) * atr
        lower_level = sma - float(self.AtrMultiplier) * atr

        if close > upper_level:
            self.BuyMarket()
            self._entry_price = close
            self._stop_level = close - float(self.StopLossMult) * atr
            self._take_level = close + float(self.TakeProfitMult) * atr
            self._bars_since_entry = 0
        elif close < lower_level:
            self.SellMarket()
            self._entry_price = close
            self._stop_level = close + float(self.StopLossMult) * atr
            self._take_level = close - float(self.TakeProfitMult) * atr
            self._bars_since_entry = 0

    def _reset_position(self):
        self._entry_price = 0.0
        self._stop_level = None
        self._take_level = None
        self._bars_since_entry = 0
        self._cooldown_counter = self.CooldownBars

    def OnReseted(self):
        super(straddle_trail_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_level = None
        self._take_level = None
        self._bars_since_entry = 0
        self._cooldown_counter = 0

    def CreateClone(self):
        return straddle_trail_strategy()
