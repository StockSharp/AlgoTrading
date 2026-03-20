import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


# CloseTypes: 0 = SlClose, 1 = TrendClose

class ro_nz_rapid_fire_strategy(Strategy):

    def __init__(self):
        super(ro_nz_rapid_fire_strategy, self).__init__()

        self._stop_loss = self.Param("StopLoss", 150) \
            .SetDisplay("Stop Loss", "Stop loss in ticks", "Risk")

        self._take_profit = self.Param("TakeProfit", 100) \
            .SetDisplay("Take Profit", "Take profit in ticks", "Risk")

        self._trailing_stop = self.Param("TrailingStop", 0) \
            .SetDisplay("Trailing Stop", "Trailing stop in ticks", "Risk")

        self._averaging = self.Param("Averaging", False) \
            .SetDisplay("Averaging", "Add to position on continuing trend", "General")

        self._ma_period = self.Param("MaPeriod", 30) \
            .SetDisplay("MA Period", "Moving average period", "Indicator")

        self._psar_step = self.Param("PsarStep", 0.02) \
            .SetDisplay("PSAR Step", "Parabolic SAR step", "Indicator")

        self._psar_max = self.Param("PsarMax", 0.2) \
            .SetDisplay("PSAR Max", "Parabolic SAR maximum", "Indicator")

        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Bars between entries", "General")

        self._close_type = self.Param("CloseType", 0) \
            .SetDisplay("Close Type", "0=StopClose, 1=TrendClose", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles for calculations", "General")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._tick = 0.0
        self._bars_since_trade = 0

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def TrailingStop(self):
        return self._trailing_stop.Value

    @TrailingStop.setter
    def TrailingStop(self, value):
        self._trailing_stop.Value = value

    @property
    def Averaging(self):
        return self._averaging.Value

    @Averaging.setter
    def Averaging(self, value):
        self._averaging.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def PsarStep(self):
        return self._psar_step.Value

    @PsarStep.setter
    def PsarStep(self, value):
        self._psar_step.Value = value

    @property
    def PsarMax(self):
        return self._psar_max.Value

    @PsarMax.setter
    def PsarMax(self, value):
        self._psar_max.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CloseType(self):
        return self._close_type.Value

    @CloseType.setter
    def CloseType(self, value):
        self._close_type.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ro_nz_rapid_fire_strategy, self).OnStarted(time)

        sec = self.Security
        self._tick = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        self._bars_since_trade = self.CooldownBars

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(sma, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_trade += 1
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sma_val = float(sma_value)

        up_signal = (self._prev_close != 0.0 and self._prev_sma != 0.0
                     and self._prev_close <= self._prev_sma and close > sma_val)
        down_signal = (self._prev_close != 0.0 and self._prev_sma != 0.0
                       and self._prev_close >= self._prev_sma and close < sma_val)

        pos = self.Position

        if pos > 0:
            if self.CloseType == 1 and down_signal:
                self.SellMarket(pos)
                self._bars_since_trade = 0

            if self.TakeProfit > 0 and high >= self._take_price:
                self.SellMarket(self.Position)
                self._bars_since_trade = 0

            if self.StopLoss > 0 and low <= self._stop_price:
                self.SellMarket(self.Position)
                self._bars_since_trade = 0

            if self.TrailingStop > 0:
                trail = close - self.TrailingStop * self._tick
                if trail > self._stop_price:
                    self._stop_price = trail

            if self.Averaging and up_signal and self._bars_since_trade >= self.CooldownBars:
                self._enter_long(candle)

        elif pos < 0:
            if self.CloseType == 1 and up_signal:
                self.BuyMarket(abs(pos))
                self._bars_since_trade = 0

            if self.TakeProfit > 0 and low <= self._take_price:
                self.BuyMarket(abs(self.Position))
                self._bars_since_trade = 0

            if self.StopLoss > 0 and high >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._bars_since_trade = 0

            if self.TrailingStop > 0:
                trail = close + self.TrailingStop * self._tick
                if trail < self._stop_price:
                    self._stop_price = trail

            if self.Averaging and down_signal and self._bars_since_trade >= self.CooldownBars:
                self._enter_short(candle)

        else:
            if up_signal and self._bars_since_trade >= self.CooldownBars:
                self._enter_long(candle)
            elif down_signal and self._bars_since_trade >= self.CooldownBars:
                self._enter_short(candle)

        self._prev_close = close
        self._prev_sma = sma_val

    def _enter_long(self, candle):
        self.BuyMarket(self.Volume)
        self._entry_price = float(candle.ClosePrice)
        self._stop_price = self._entry_price - self.StopLoss * self._tick if self.StopLoss > 0 else 0.0
        self._take_price = self._entry_price + self.TakeProfit * self._tick if self.TakeProfit > 0 else 0.0
        self._bars_since_trade = 0

    def _enter_short(self, candle):
        self.SellMarket(self.Volume)
        self._entry_price = float(candle.ClosePrice)
        self._stop_price = self._entry_price + self.StopLoss * self._tick if self.StopLoss > 0 else 0.0
        self._take_price = self._entry_price - self.TakeProfit * self._tick if self.TakeProfit > 0 else 0.0
        self._bars_since_trade = 0

    def OnReseted(self):
        super(ro_nz_rapid_fire_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._tick = 0.0
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return ro_nz_rapid_fire_strategy()
