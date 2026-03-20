import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class smc_order_block_zones_strategy(Strategy):
    def __init__(self):
        super(smc_order_block_zones_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._sma_length = self.Param("SmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "Equilibrium SMA period", "Indicators")
        self._bb_length = self.Param("BbLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger Bands period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def sma_length(self):
        return self._sma_length.Value
    @property
    def bb_length(self):
        return self._bb_length.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(smc_order_block_zones_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(smc_order_block_zones_strategy, self).OnStarted(time)
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_length
        self._bb = BollingerBands()
        self._bb.Length = self.bb_length
        self._bb.Width = 2.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .BindEx(self._sma, self._bb, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_value, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._sma.IsFormed or not self._bb.IsFormed:
            return
        if sma_value.IsEmpty or bb_value.IsEmpty:
            return

        sma = float(sma_value)
        upper = bb_value.UpBand
        lower = bb_value.LowBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        equilibrium = sma

        if price < equilibrium and price <= lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif price > equilibrium and price >= upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and price > equilibrium:
            self.SellMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and price < equilibrium:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return smc_order_block_zones_strategy()
