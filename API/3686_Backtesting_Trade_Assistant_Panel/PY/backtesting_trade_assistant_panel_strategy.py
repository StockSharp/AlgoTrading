import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class backtesting_trade_assistant_panel_strategy(Strategy):
    def __init__(self):
        super(backtesting_trade_assistant_panel_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 100) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")

        self._sma = None
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(backtesting_trade_assistant_panel_strategy, self).OnReseted()
        self._sma = None
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def OnStarted(self, time):
        super(backtesting_trade_assistant_panel_strategy, self).OnStarted(time)

        self.__sma = SimpleMovingAverage()
        self.__sma.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return backtesting_trade_assistant_panel_strategy()
