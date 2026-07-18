import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class aftershock_playbook_stock_earnings_drift_engine_strategy(Strategy):
    def __init__(self):
        super(aftershock_playbook_stock_earnings_drift_engine_strategy, self).__init__()

        self._atr_mult = self.Param("AtrMultiplier", 1.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for surprise threshold", "Strategy")

        self._atr_len = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR lookback period", "Strategy")

        self._surprise_threshold = self.Param("SurpriseThreshold", 1.0) \
            .SetDisplay("Surprise Threshold", "ATR multiplier for detecting surprise moves", "Strategy")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._prev_close = 0.0
        self._cooldown_remaining = 0

    @property
    def AtrMultiplier(self):
        return self._atr_mult.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_mult.Value = value

    @property
    def AtrLength(self):
        return self._atr_len.Value

    @AtrLength.setter
    def AtrLength(self, value):
        self._atr_len.Value = value

    @property
    def SurpriseThreshold(self):
        return self._surprise_threshold.Value

    @SurpriseThreshold.setter
    def SurpriseThreshold(self, value):
        self._surprise_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(aftershock_playbook_stock_earnings_drift_engine_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(aftershock_playbook_stock_earnings_drift_engine_strategy, self).OnStarted2(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = float(candle.ClosePrice)
            return

        if self._prev_close == 0.0 or atr_val == 0.0:
            self._prev_close = float(candle.ClosePrice)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = float(candle.ClosePrice)
            return

        close_price = float(candle.ClosePrice)
        change = close_price - self._prev_close
        threshold = atr_val * float(self.SurpriseThreshold)

        # Detect large price moves (earnings surprise proxy)
        if change > threshold and self.Position <= 0:
            # Large positive move - go long (drift continuation)
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = self.CooldownBars
        elif change < -threshold and self.Position >= 0:
            # Large negative move - go short (drift continuation)
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = self.CooldownBars
        # Exit if price reverses by ATR amount
        elif self.Position > 0 and change < -atr_val * float(self.AtrMultiplier):
            self.SellMarket(abs(self.Position))
            self._cooldown_remaining = self.CooldownBars
        elif self.Position < 0 and change > atr_val * float(self.AtrMultiplier):
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.CooldownBars

        self._prev_close = close_price

    def CreateClone(self):
        return aftershock_playbook_stock_earnings_drift_engine_strategy()
