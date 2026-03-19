import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bands_pending_breakout_strategy(Strategy):
    def __init__(self):
        super(bands_pending_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Source candles", "General")
        self._bb_period = self.Param("BbPeriod", 15) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def bb_period(self):
        return self._bb_period.Value
    @bb_period.setter
    def bb_period(self, value):
        self._bb_period.Value = value

    def OnStarted(self, time):
        super(bands_pending_breakout_strategy, self).OnStarted(time)
        self._bb = BollingerBands()
        self._bb.Length = self.bb_period
        self._bb.Width = 1.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        if upper is None or lower is None:
            return

        if candle.ClosePrice > upper and self.Position <= 0:
            self.BuyMarket()
        elif candle.ClosePrice < lower and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return bands_pending_breakout_strategy()
