import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy

class simple_trade_flip_strategy(Strategy):
    def __init__(self):
        super(simple_trade_flip_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetDisplay("Trade Volume", "Order size in lots", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 120.0) \
            .SetDisplay("Stop-Loss Points", "Protective stop distance in instrument points", "Risk")
        self._lookback_bars = self.Param("LookbackBars", 10) \
            .SetDisplay("Lookback Bars", "Number of bars used for open price comparison", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Primary timeframe used for signal calculations", "General")

        self._open_history = []
        self._cooldown = 0

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def LookbackBars(self):
        return self._lookback_bars.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(simple_trade_flip_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None else 0.0
        sl_pts = float(self.StopLossPoints)
        sl = Unit(sl_pts * step, UnitTypes.Absolute) if sl_pts > 0 and step > 0 else None
        self.StartProtection(None, sl)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._open_history.append(float(candle.OpenPrice))

        max_history = max(self.LookbackBars + 5, 5)
        if len(self._open_history) > max_history:
            self._open_history = self._open_history[-max_history:]

        lookback = self.LookbackBars
        if len(self._open_history) <= lookback:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        volume = float(self.TradeVolume)
        if volume <= 0:
            return

        current_open = float(candle.OpenPrice)
        reference_open = self._open_history[-(lookback + 1)]

        diff = current_open - reference_open
        if abs(diff) < current_open * 0.001:
            return

        if diff > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket(volume)
            self._cooldown = 5
        elif diff < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
            self.SellMarket(volume)
            self._cooldown = 5

    def OnReseted(self):
        super(simple_trade_flip_strategy, self).OnReseted()
        self._open_history = []
        self._cooldown = 0

    def CreateClone(self):
        return simple_trade_flip_strategy()
