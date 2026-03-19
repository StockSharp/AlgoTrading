import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class candle_trader_strategy(Strategy):
    """
    Strategy based on candle direction patterns.
    Opens long or short positions depending on the directions of the last four candles.
    """

    def __init__(self):
        super(candle_trader_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Volume", "Order volume", "General")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 500.0) \
            .SetDisplay("Take Profit Ticks", "Take profit in price steps", "Risk Management")
        self._stop_loss_ticks = self.Param("StopLossTicks", 50.0) \
            .SetDisplay("Stop Loss Ticks", "Stop loss in price steps", "Risk Management")
        self._continuation = self.Param("Continuation", True) \
            .SetDisplay("Use Continuation", "Allow continuation pattern", "Trading Logic")
        self._reverse_close = self.Param("ReverseClose", True) \
            .SetDisplay("Reverse Close", "Close opposite position on signal", "Trading Logic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._bar1_dir = 0
        self._bar2_dir = 0
        self._bar3_dir = 0
        self._bar4_dir = 0

    @property
    def trade_volume(self):
        return self._trade_volume.Value

    @trade_volume.setter
    def trade_volume(self, value):
        self._trade_volume.Value = value

    @property
    def take_profit_ticks(self):
        return self._take_profit_ticks.Value

    @take_profit_ticks.setter
    def take_profit_ticks(self, value):
        self._take_profit_ticks.Value = value

    @property
    def stop_loss_ticks(self):
        return self._stop_loss_ticks.Value

    @stop_loss_ticks.setter
    def stop_loss_ticks(self, value):
        self._stop_loss_ticks.Value = value

    @property
    def continuation(self):
        return self._continuation.Value

    @continuation.setter
    def continuation(self, value):
        self._continuation.Value = value

    @property
    def reverse_close(self):
        return self._reverse_close.Value

    @reverse_close.setter
    def reverse_close(self, value):
        self._reverse_close.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(candle_trader_strategy, self).OnReseted()
        self._bar1_dir = 0
        self._bar2_dir = 0
        self._bar3_dir = 0
        self._bar4_dir = 0

    def OnStarted(self, time):
        super(candle_trader_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Shift stored directions
        self._bar4_dir = self._bar3_dir
        self._bar3_dir = self._bar2_dir
        self._bar2_dir = self._bar1_dir

        # Determine direction of current candle
        if candle.ClosePrice > candle.OpenPrice:
            self._bar1_dir = 1
        elif candle.ClosePrice < candle.OpenPrice:
            self._bar1_dir = -1
        else:
            self._bar1_dir = 0

        # Ensure sufficient history
        if self._bar4_dir == 0:
            return

        buy_direct = self._bar1_dir == 1 and self._bar2_dir == -1 and self._bar3_dir == -1
        buy_cont = (self._bar1_dir == 1 and self._bar2_dir == -1 and self._bar3_dir == 1
                    and self._bar4_dir == 1 and self.continuation)

        sell_direct = self._bar1_dir == -1 and self._bar2_dir == 1 and self._bar3_dir == 1
        sell_cont = (self._bar1_dir == -1 and self._bar2_dir == 1 and self._bar3_dir == -1
                     and self._bar4_dir == -1 and self.continuation)

        if (buy_direct or buy_cont) and self.Position <= 0:
            if self.reverse_close and self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif (sell_direct or sell_cont) and self.Position >= 0:
            if self.reverse_close and self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return candle_trader_strategy()
