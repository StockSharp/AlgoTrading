import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class eurusd_v2_0_strategy(Strategy):
    """
    SMA mean-reversion strategy with TP/SL.
    Buys below SMA, sells above SMA when distance exceeds threshold.
    Exits on take profit or stop loss.
    """

    def __init__(self):
        super(eurusd_v2_0_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 50) \
            .SetDisplay("MA Length", "SMA period", "General")
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._stop_loss = self.Param("StopLoss", 300.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(eurusd_v2_0_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(eurusd_v2_0_strategy, self).OnStarted(time)

        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sma_val = float(sma_val)
        tp = float(self._take_profit.Value)
        sl = float(self._stop_loss.Value)

        if self.Position > 0:
            if close - self._entry_price >= tp or self._entry_price - close >= sl:
                self.SellMarket()
                self._entry_price = 0.0
                return
        elif self.Position < 0:
            if self._entry_price - close >= tp or close - self._entry_price >= sl:
                self.BuyMarket()
                self._entry_price = 0.0
                return

        if self.Position != 0:
            return

        dist = abs(close - sma_val)
        if dist < sma_val * 0.002:
            return

        if close < sma_val:
            self.BuyMarket()
            self._entry_price = close
        elif close > sma_val:
            self.SellMarket()
            self._entry_price = close

    def CreateClone(self):
        return eurusd_v2_0_strategy()
