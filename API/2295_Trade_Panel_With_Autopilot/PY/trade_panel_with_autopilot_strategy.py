import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class trade_panel_with_autopilot_strategy(Strategy):
    def __init__(self):
        super(trade_panel_with_autopilot_strategy, self).__init__()
        self._open_threshold = self.Param("OpenThreshold", 70.0) \
            .SetDisplay("Open %", "Threshold for new position", "General")
        self._close_threshold = self.Param("CloseThreshold", 45.0) \
            .SetDisplay("Close %", "Threshold for closing", "General")
        self._window_size = self.Param("WindowSize", 10) \
            .SetDisplay("Window Size", "Number of candles for signal aggregation", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_window = []
        self._prev_candle = None

    @property
    def open_threshold(self):
        return self._open_threshold.Value

    @property
    def close_threshold(self):
        return self._close_threshold.Value

    @property
    def window_size(self):
        return self._window_size.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trade_panel_with_autopilot_strategy, self).OnReseted()
        self._prev_candle = None
        self._signal_window = []

    def OnStarted(self, time):
        super(trade_panel_with_autopilot_strategy, self).OnStarted(time)
        self._prev_candle = None
        self._signal_window = []
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_candle is None:
            self._prev_candle = candle
            return
        buy = 0
        sell = 0
        if float(candle.OpenPrice) > float(self._prev_candle.OpenPrice):
            buy += 1
        else:
            sell += 1
        if float(candle.HighPrice) > float(self._prev_candle.HighPrice):
            buy += 1
        else:
            sell += 1
        if float(candle.LowPrice) > float(self._prev_candle.LowPrice):
            buy += 1
        else:
            sell += 1
        if float(candle.ClosePrice) > float(self._prev_candle.ClosePrice):
            buy += 1
        else:
            sell += 1
        hl_curr = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        hl_prev = (float(self._prev_candle.HighPrice) + float(self._prev_candle.LowPrice)) / 2.0
        if hl_curr > hl_prev:
            buy += 1
        else:
            sell += 1
        hlc_curr = (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        hlc_prev = (float(self._prev_candle.HighPrice) + float(self._prev_candle.LowPrice) + float(self._prev_candle.ClosePrice)) / 3.0
        if hlc_curr > hlc_prev:
            buy += 1
        else:
            sell += 1
        hlcc_curr = (float(candle.HighPrice) + float(candle.LowPrice) + 2.0 * float(candle.ClosePrice)) / 4.0
        hlcc_prev = (float(self._prev_candle.HighPrice) + float(self._prev_candle.LowPrice) + 2.0 * float(self._prev_candle.ClosePrice)) / 4.0
        if hlcc_curr > hlcc_prev:
            buy += 1
        else:
            sell += 1
        self._signal_window.append((buy, sell))
        ws = int(self.window_size)
        while len(self._signal_window) > ws:
            self._signal_window.pop(0)
        self._prev_candle = candle
        if len(self._signal_window) < ws:
            return
        total_buy = 0
        total_sell = 0
        for b, s in self._signal_window:
            total_buy += b
            total_sell += s
        total = total_buy + total_sell
        if total == 0:
            return
        buy_pct = float(total_buy) / float(total) * 100.0
        sell_pct = float(total_sell) / float(total) * 100.0
        close_threshold = float(self.close_threshold)
        open_threshold = float(self.open_threshold)
        if self.Position > 0 and buy_pct < close_threshold:
            self.SellMarket()
        elif self.Position < 0 and sell_pct < close_threshold:
            self.BuyMarket()
        if self.Position == 0:
            if buy_pct >= open_threshold:
                self.BuyMarket()
            elif sell_pct >= open_threshold:
                self.SellMarket()

    def CreateClone(self):
        return trade_panel_with_autopilot_strategy()
