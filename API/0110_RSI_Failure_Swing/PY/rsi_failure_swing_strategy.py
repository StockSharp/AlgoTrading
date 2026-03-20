import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_failure_swing_strategy(Strategy):
    """
    Strategy that trades based on RSI Failure Swing pattern.
    A failure swing occurs when RSI reverses direction without crossing through centerline.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(rsi_failure_swing_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "Period for RSI", "RSI Settings")
        self._oversold_level = self.Param("OversoldLevel", 40.0).SetDisplay("Oversold Level", "RSI oversold threshold", "RSI Settings")
        self._overbought_level = self.Param("OverboughtLevel", 60.0).SetDisplay("Overbought Level", "RSI overbought threshold", "RSI Settings")
        self._cooldown_bars = self.Param("CooldownBars", 400).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_rsi = 0.0
        self._prev_prev_rsi = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_failure_swing_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_prev_rsi = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(rsi_failure_swing_strategy, self).OnStarted(time)

        self._prev_rsi = 0.0
        self._prev_prev_rsi = 0.0
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

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rv = float(rsi_val)

        # Need at least 2 previous RSI values
        if self._prev_rsi == 0 or self._prev_prev_rsi == 0:
            self._prev_prev_rsi = self._prev_rsi
            self._prev_rsi = rv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_prev_rsi = self._prev_rsi
            self._prev_rsi = rv
            return

        cd = self._cooldown_bars.Value
        oversold = self._oversold_level.Value
        overbought = self._overbought_level.Value

        # Bullish Failure Swing: RSI was oversold, rose, pulled back but stayed above prior low
        is_bullish = (
            self._prev_prev_rsi < oversold and
            self._prev_rsi > self._prev_prev_rsi and
            rv < self._prev_rsi and
            rv > self._prev_prev_rsi
        )

        # Bearish Failure Swing: RSI was overbought, fell, bounced but stayed below prior high
        is_bearish = (
            self._prev_prev_rsi > overbought and
            self._prev_rsi < self._prev_prev_rsi and
            rv > self._prev_rsi and
            rv < self._prev_prev_rsi
        )

        if self.Position == 0:
            if is_bullish:
                self.BuyMarket()
                self._cooldown = cd
            elif is_bearish:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            # Exit long when RSI crosses above overbought or reverses from peak
            if rv > overbought or (rv < 45 and self._prev_rsi > 45):
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            # Exit short when RSI crosses below oversold or reverses from trough
            if rv < oversold or (rv > 55 and self._prev_rsi < 55):
                self.BuyMarket()
                self._cooldown = cd

        self._prev_prev_rsi = self._prev_rsi
        self._prev_rsi = rv

    def CreateClone(self):
        return rsi_failure_swing_strategy()
