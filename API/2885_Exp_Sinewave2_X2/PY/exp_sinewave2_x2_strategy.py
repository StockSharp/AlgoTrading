import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class exp_sinewave2_x2_strategy(Strategy):
    """
    Ehlers Sinewave X2 strategy (simplified). Uses CCI oscillator for entries.
    CCI crosses above -100 with price above EMA to buy, CCI crosses below 100 with price below EMA to sell.
    """

    def __init__(self):
        super(exp_sinewave2_x2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._cci_length = self.Param("CciLength", 14) \
            .SetDisplay("CCI Length", "CCI period", "Indicators")
        self._ema_length = self.Param("EmaLength", 30) \
            .SetDisplay("EMA Length", "Trend filter EMA", "Indicators")

        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_sinewave2_x2_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(exp_sinewave2_x2_strategy, self).OnStarted2(time)

        self._has_prev = False
        cci = CommodityChannelIndex()
        cci.Length = self._cci_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, cci_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        cci_val = float(cci_val)
        ema_val = float(ema_val)

        if not self._has_prev:
            self._prev_cci = cci_val
            self._has_prev = True
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_cci = cci_val
            return

        close = float(candle.ClosePrice)

        if self._prev_cci < -100 and cci_val >= -100 and close > ema_val and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_cci > 100 and cci_val <= 100 and close < ema_val and self.Position >= 0:
            self.SellMarket()

        self._prev_cci = cci_val

    def CreateClone(self):
        return exp_sinewave2_x2_strategy()
