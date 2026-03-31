import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_hook_reversal_strategy(Strategy):
    """
    RSI Hook Reversal strategy.
    Enters long when RSI hooks up from oversold zone.
    Enters short when RSI hooks down from overbought zone.
    Exits when RSI reaches neutral zone.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(rsi_hook_reversal_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "Period for RSI", "RSI")
        self._oversold_level = self.Param("OversoldLevel", 30).SetDisplay("Oversold", "Oversold level", "RSI")
        self._overbought_level = self.Param("OverboughtLevel", 70).SetDisplay("Overbought", "Overbought level", "RSI")
        self._exit_level = self.Param("ExitLevel", 50).SetDisplay("Exit Level", "Neutral exit zone", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_rsi = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_hook_reversal_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(rsi_hook_reversal_strategy, self).OnStarted2(time)

        self._prev_rsi = 0.0
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

        rv = float(rsi_val)

        if self._prev_rsi == 0:
            self._prev_rsi = rv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rv
            return

        cd = self._cooldown_bars.Value
        oversold = self._oversold_level.Value
        overbought = self._overbought_level.Value
        exit_lvl = self._exit_level.Value

        # RSI hook up from oversold
        oversold_hook_up = self._prev_rsi < oversold and rv > self._prev_rsi
        # RSI hook down from overbought
        overbought_hook_down = self._prev_rsi > overbought and rv < self._prev_rsi

        if self.Position == 0 and oversold_hook_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and overbought_hook_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and rv < exit_lvl:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and rv > exit_lvl:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_rsi = rv

    def CreateClone(self):
        return rsi_hook_reversal_strategy()
