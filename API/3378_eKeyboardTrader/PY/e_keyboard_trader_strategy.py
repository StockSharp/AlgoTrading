import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class e_keyboard_trader_strategy(Strategy):
    """
    eKeyboard Trader strategy: CCI trend following.
    Buys when CCI crosses above +100, sells when CCI crosses below -100.
    """

    def __init__(self):
        super(e_keyboard_trader_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI period", "Indicators")

        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(e_keyboard_trader_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(e_keyboard_trader_strategy, self).OnStarted(time)
        self._has_prev = False

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

        cci_val = float(cci_val)

        if self._has_prev:
            if self._prev_cci <= 100.0 and cci_val > 100.0 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_cci >= -100.0 and cci_val < -100.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_cci = cci_val
        self._has_prev = True

    def CreateClone(self):
        return e_keyboard_trader_strategy()
