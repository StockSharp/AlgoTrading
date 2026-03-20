import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class heikin_ashi_consecutive_strategy(Strategy):

    def __init__(self):
        super(heikin_ashi_consecutive_strategy, self).__init__()

        self._consecutive_candles = self.Param("ConsecutiveCandles", 7) \
            .SetDisplay("Consecutive Candles", "Number of consecutive candles required for signal", "Trading parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._bullish_count = 0
        self._bearish_count = 0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_ha_high = 0.0
        self._prev_ha_low = 0.0

    @property
    def ConsecutiveCandles(self):
        return self._consecutive_candles.Value

    @ConsecutiveCandles.setter
    def ConsecutiveCandles(self, value):
        self._consecutive_candles.Value = value

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

    def OnStarted(self, time):
        super(heikin_ashi_consecutive_strategy, self).OnStarted(time)

        self._bullish_count = 0
        self._bearish_count = 0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_ha_high = 0.0
        self._prev_ha_low = 0.0

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(float(self.StopLossPercent), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)

        if self._prev_ha_open == 0:
            ha_open = (o + c) / 2.0
            ha_close = (o + c + h + l) / 4.0
            ha_high = h
            ha_low = l
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
            ha_close = (o + c + h + l) / 4.0
            ha_high = max(max(h, ha_open), ha_close)
            ha_low = min(min(l, ha_open), ha_close)

        is_bullish = ha_close > ha_open
        is_bearish = ha_close < ha_open

        if is_bullish:
            self._bullish_count += 1
            self._bearish_count = 0
        elif is_bearish:
            self._bearish_count += 1
            self._bullish_count = 0
        else:
            self._bullish_count = 0
            self._bearish_count = 0

        consec = int(self.ConsecutiveCandles)

        if self._bullish_count >= consec and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif self._bearish_count >= consec and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_ha_high = ha_high
        self._prev_ha_low = ha_low

    def OnReseted(self):
        super(heikin_ashi_consecutive_strategy, self).OnReseted()
        self._bullish_count = 0
        self._bearish_count = 0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_ha_high = 0.0
        self._prev_ha_low = 0.0

    def CreateClone(self):
        return heikin_ashi_consecutive_strategy()
