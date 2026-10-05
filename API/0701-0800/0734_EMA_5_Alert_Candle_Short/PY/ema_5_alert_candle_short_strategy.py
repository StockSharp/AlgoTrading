import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_5_alert_candle_short_strategy(Strategy):
    """
    EMA 5 alert candle short strategy.
    After at least three consecutive candles touching the EMA, a candle whose low stays above the EMA becomes the alert candle.
    When a following candle breaks below the alert candle low a short is opened with the stop at the alert candle high and the
    take profit at the same distance below the entry. A later candle that stays above the EMA replaces the alert candle.
    """

    def __init__(self):
        super(ema_5_alert_candle_short_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 5).SetGreaterThanZero().SetDisplay("EMA Period", "EMA period", "Indicators")
        self._risk_per_trade = self.Param("RiskPerTrade", 2.0).SetNotNegative().SetDisplay("Risk Per Trade %", "Risk per trade in percent of capital", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._touch_count = 0
        self._alert_high = None
        self._alert_low = None
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(ema_5_alert_candle_short_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_5_alert_candle_short_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = float(ema_value)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position < 0:
            if high >= self._stop_price or low <= self._take_price:
                self.BuyMarket(-self.Position)
            self._update_pattern(high, low, ema)
            return

        if self._alert_high is not None and self._alert_low is not None and low < self._alert_low:
            alert_high = self._alert_high
            entry = float(candle.ClosePrice)
            risk = alert_high - entry

            self._alert_high = None
            self._alert_low = None
            self._touch_count = 0

            if risk > 0:
                self._stop_price = alert_high
                self._take_price = entry - risk
                self.SellMarket(self.Volume)
                return

        self._update_pattern(high, low, ema)

    def _update_pattern(self, high, low, ema):
        touches = low <= ema and high >= ema

        if touches:
            # A touch after an alert candle starts a new sequence of touching candles.
            self._touch_count = self._touch_count + 1 if self._alert_high is None else 1
            self._alert_high = None
            self._alert_low = None
        elif low > ema and (self._touch_count >= 3 or self._alert_high is not None):
            self._alert_high = high
            self._alert_low = low
        else:
            self._touch_count = 0
            self._alert_high = None
            self._alert_low = None

    def CreateClone(self):
        return ema_5_alert_candle_short_strategy()
