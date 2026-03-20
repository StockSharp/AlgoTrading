import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_failure_swing_strategy(Strategy):
    """
    Strategy that trades based on CCI Failure Swing pattern.
    A failure swing occurs when CCI reverses direction without crossing through centerline.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(cci_failure_swing_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 20).SetDisplay("CCI Period", "Period for CCI", "CCI Settings")
        self._oversold_level = self.Param("OversoldLevel", -50.0).SetDisplay("Oversold Level", "CCI oversold threshold", "CCI Settings")
        self._overbought_level = self.Param("OverboughtLevel", 50.0).SetDisplay("Overbought Level", "CCI overbought threshold", "CCI Settings")
        self._cooldown_bars = self.Param("CooldownBars", 350).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_cci = 0.0
        self._prev_prev_cci = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_failure_swing_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._prev_prev_cci = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(cci_failure_swing_strategy, self).OnStarted(time)

        self._prev_cci = 0.0
        self._prev_prev_cci = 0.0
        self._cooldown = 0

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cv = float(cci_val)

        # Need at least 2 previous CCI values
        if self._prev_cci == 0 or self._prev_prev_cci == 0:
            self._prev_prev_cci = self._prev_cci
            self._prev_cci = cv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_prev_cci = self._prev_cci
            self._prev_cci = cv
            return

        cd = self._cooldown_bars.Value
        oversold = self._oversold_level.Value
        overbought = self._overbought_level.Value

        # Bullish Failure Swing: CCI was oversold, rose, pulled back but stayed above prior low
        is_bullish = (
            self._prev_prev_cci < oversold and
            self._prev_cci > self._prev_prev_cci and
            cv < self._prev_cci and
            cv > self._prev_prev_cci
        )

        # Bearish Failure Swing: CCI was overbought, fell, bounced but stayed below prior high
        is_bearish = (
            self._prev_prev_cci > overbought and
            self._prev_cci < self._prev_prev_cci and
            cv > self._prev_cci and
            cv < self._prev_prev_cci
        )

        if self.Position == 0:
            if is_bullish:
                self.BuyMarket()
                self._cooldown = cd
            elif is_bearish:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            # Exit long when CCI crosses above overbought
            if cv > overbought:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            # Exit short when CCI crosses below oversold
            if cv < oversold:
                self.BuyMarket()
                self._cooldown = cd

        self._prev_prev_cci = self._prev_cci
        self._prev_cci = cv

    def CreateClone(self):
        return cci_failure_swing_strategy()
