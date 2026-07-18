import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hedge_any_positions_strategy(Strategy):
    """
    Hedge Any Positions: ATR-based mean reversion.
    Enters long when price drops below EMA by ATR*multiplier.
    Enters short when price rallies above EMA by ATR*multiplier.
    Uses SL/TP for risk management with cooldown.
    """

    def __init__(self):
        super(hedge_any_positions_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 100) \
            .SetDisplay("EMA Period", "EMA period for mean price", "Indicator")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR calculation period", "Indicator")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for entry distance", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 300) \
            .SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hedge_any_positions_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(hedge_any_positions_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self._process_candle).Start()

    def _process_candle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = float(candle.ClosePrice)
        ema = float(ema_val)
        atr = float(atr_val)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        threshold = atr * self._atr_multiplier.Value

        if self.Position > 0 and self._entry_price > 0:
            sl_pts = self._stop_loss_points.Value
            tp_pts = self._take_profit_points.Value
            if sl_pts > 0 and close <= self._entry_price - sl_pts * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                return
            if tp_pts > 0 and close >= self._entry_price + tp_pts * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                return
        elif self.Position < 0 and self._entry_price > 0:
            sl_pts = self._stop_loss_points.Value
            tp_pts = self._take_profit_points.Value
            if sl_pts > 0 and close >= self._entry_price + sl_pts * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                return
            if tp_pts > 0 and close <= self._entry_price - tp_pts * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                return

        if close < ema - threshold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 80
        elif close > ema + threshold and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 80

    def CreateClone(self):
        return hedge_any_positions_strategy()
