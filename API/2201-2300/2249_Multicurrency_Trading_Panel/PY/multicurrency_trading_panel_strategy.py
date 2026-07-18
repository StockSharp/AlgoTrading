import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class multicurrency_trading_panel_strategy(Strategy):
    def __init__(self):
        super(multicurrency_trading_panel_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._prev = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multicurrency_trading_panel_strategy, self).OnReseted()
        self._prev = None

    def OnStarted2(self, time):
        super(multicurrency_trading_panel_strategy, self).OnStarted2(time)
        warmup = ExponentialMovingAverage()
        warmup.Length = 5
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()

    def process_candle(self, candle, warmup_val):
        if candle.State != CandleStates.Finished:
            return
        if self._prev is None:
            self._prev = candle
            return
        buy = 0
        sell = 0
        cur_open = float(candle.OpenPrice)
        cur_high = float(candle.HighPrice)
        cur_low = float(candle.LowPrice)
        cur_close = float(candle.ClosePrice)
        prev_open = float(self._prev.OpenPrice)
        prev_high = float(self._prev.HighPrice)
        prev_low = float(self._prev.LowPrice)
        prev_close = float(self._prev.ClosePrice)
        if cur_open > prev_open:
            buy += 1
        else:
            sell += 1
        if cur_high > prev_high:
            buy += 1
        else:
            sell += 1
        if cur_low > prev_low:
            buy += 1
        else:
            sell += 1
        if (cur_high + cur_low) / 2.0 > (prev_high + prev_low) / 2.0:
            buy += 1
        else:
            sell += 1
        if cur_close > prev_close:
            buy += 1
        else:
            sell += 1
        if (cur_high + cur_low + cur_close) / 3.0 > (prev_high + prev_low + prev_close) / 3.0:
            buy += 1
        else:
            sell += 1
        if (cur_high + cur_low + cur_close + cur_close) / 4.0 > (prev_high + prev_low + prev_close + prev_close) / 4.0:
            buy += 1
        else:
            sell += 1
        if buy > sell and self.Position <= 0:
            self.BuyMarket()
        elif sell > buy and self.Position >= 0:
            self.SellMarket()
        self._prev = candle

    def CreateClone(self):
        return multicurrency_trading_panel_strategy()
