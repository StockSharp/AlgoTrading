import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_hook_reversal_strategy(Strategy):
    """
    CCI Hook Reversal strategy.
    Enters long when CCI hooks up from oversold zone.
    Enters short when CCI hooks down from overbought zone.
    Exits when CCI crosses zero.
    """

    def __init__(self):
        super(cci_hook_reversal_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 20).SetDisplay("CCI Period", "Period for CCI", "CCI")
        self._oversold_level = self.Param("OversoldLevel", -100).SetDisplay("Oversold", "Oversold level", "CCI")
        self._overbought_level = self.Param("OverboughtLevel", 100).SetDisplay("Overbought", "Overbought level", "CCI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_cci = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_hook_reversal_strategy, self).OnReseted()
        self._prev_cci = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(cci_hook_reversal_strategy, self).OnStarted(time)

        self._prev_cci = None
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

        if self._prev_cci is None:
            self._prev_cci = cv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_cci = cv
            return

        cd = self._cooldown_bars.Value
        oversold = self._oversold_level.Value
        overbought = self._overbought_level.Value

        # Hook up from oversold
        oversold_hook_up = self._prev_cci < oversold and cv > self._prev_cci
        # Hook down from overbought
        overbought_hook_down = self._prev_cci > overbought and cv < self._prev_cci

        if self.Position == 0 and oversold_hook_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and overbought_hook_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and cv < 0:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and cv > 0:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_cci = cv

    def CreateClone(self):
        return cci_hook_reversal_strategy()
