import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, BollingerBands, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class smc_order_block_zones_strategy(Strategy):
    """SMC Order Block Zones Strategy."""

    def __init__(self):
        super(smc_order_block_zones_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._sma_length = self.Param("SmaLength", 50) \
            .SetDisplay("SMA Length", "Equilibrium SMA period", "Indicators")
        self._bb_length = self.Param("BbLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._sma = None
        self._bb = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(smc_order_block_zones_strategy, self).OnReseted()
        self._sma = None
        self._bb = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(smc_order_block_zones_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = int(self._sma_length.Value)

        self._bb = BollingerBands()
        self._bb.Length = int(self._bb_length.Value)
        self._bb.Width = 2.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._sma, self._bb, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, sma_value, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed or not self._bb.IsFormed:
            return

        if sma_value.IsEmpty or bb_value.IsEmpty:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None:
            return

        sma_val = float(IndicatorHelper.ToDecimal(sma_value))
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        equilibrium = sma_val
        cooldown = int(self._cooldown_bars.Value)

        if price < equilibrium and price <= lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif price > equilibrium and price >= upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and price > equilibrium:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and price < equilibrium:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return smc_order_block_zones_strategy()
