import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class xma_candles_strategy(Strategy):
    def __init__(self):
        super(xma_candles_strategy, self).__init__()
        self._length = self.Param("Length", 12) \
            .SetDisplay("Length", "Smoothing length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Open", "Allow opening long positions", "Parameters")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Open", "Allow opening short positions", "Parameters")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Buy Close", "Allow closing long positions", "Parameters")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Sell Close", "Allow closing short positions", "Parameters")
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss in percent", "Protection")
        self._take_profit = self.Param("TakeProfit", 4.0) \
            .SetDisplay("Take Profit %", "Take profit in percent", "Protection")
        self._open_ma = None
        self._close_ma = None
        self._prev_color = -1

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value

    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value

    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value

    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    def OnReseted(self):
        super(xma_candles_strategy, self).OnReseted()
        self._prev_color = -1

    def OnStarted(self, time):
        super(xma_candles_strategy, self).OnStarted(time)
        self._prev_color = -1
        self._open_ma = ExponentialMovingAverage()
        self._open_ma.Length = int(self.length)
        self._close_ma = ExponentialMovingAverage()
        self._close_ma.Length = int(self.length)
        self.Indicators.Add(self._open_ma)
        self.Indicators.Add(self._close_ma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            stopLoss=Unit(float(self.stop_loss), UnitTypes.Percent),
            takeProfit=Unit(float(self.take_profit), UnitTypes.Percent))

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        t = candle.ServerTime
        open_result = self._open_ma.Process(candle.OpenPrice, t, True)
        close_result = self._close_ma.Process(candle.ClosePrice, t, True)
        if not self._open_ma.IsFormed or not self._close_ma.IsFormed:
            return
        open_ma = float(open_result.GetValue[float]())
        close_ma = float(close_result.GetValue[float]())
        if open_ma < close_ma:
            current_color = 2
        elif open_ma > close_ma:
            current_color = 0
        else:
            current_color = 1
        if current_color == 2 and self._prev_color != 2:
            if self.Position <= 0:
                self.BuyMarket()
        elif current_color == 0 and self._prev_color != 0:
            if self.Position >= 0:
                self.SellMarket()
        self._prev_color = current_color

    def CreateClone(self):
        return xma_candles_strategy()
