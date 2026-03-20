import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy

class williams_r_hook_reversal_strategy(Strategy):
    """
    Williams %R Hook Reversal strategy.
    Enters long when Williams %R hooks up from oversold zone.
    Enters short when Williams %R hooks down from overbought zone.
    Exits when Williams %R reaches neutral zone.
    """

    def __init__(self):
        super(williams_r_hook_reversal_strategy, self).__init__()
        self._wr_period = self.Param("WillRPeriod", 14).SetDisplay("Williams %R Period", "Period for Williams %R", "Williams %R")
        self._oversold_level = self.Param("OversoldLevel", -80.0).SetDisplay("Oversold", "Oversold level", "Williams %R")
        self._overbought_level = self.Param("OverboughtLevel", -20.0).SetDisplay("Overbought", "Overbought level", "Williams %R")
        self._exit_level = self.Param("ExitLevel", -50.0).SetDisplay("Exit Level", "Neutral exit zone", "Williams %R")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_wr = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_r_hook_reversal_strategy, self).OnReseted()
        self._prev_wr = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(williams_r_hook_reversal_strategy, self).OnStarted(time)

        self._prev_wr = None
        self._cooldown = 0

        wr = WilliamsR()
        wr.Length = self._wr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, wr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        wv = float(wr_val)

        if self._prev_wr is None:
            self._prev_wr = wv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_wr = wv
            return

        cd = self._cooldown_bars.Value
        oversold = self._oversold_level.Value
        overbought = self._overbought_level.Value
        exit_lvl = self._exit_level.Value

        # Hook up from oversold
        oversold_hook_up = self._prev_wr < oversold and wv > self._prev_wr
        # Hook down from overbought
        overbought_hook_down = self._prev_wr > overbought and wv < self._prev_wr

        if self.Position == 0 and oversold_hook_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and overbought_hook_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and wv < exit_lvl:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and wv > exit_lvl:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_wr = wv

    def CreateClone(self):
        return williams_r_hook_reversal_strategy()
