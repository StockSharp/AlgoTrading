import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class j_satl_digit_system_strategy(Strategy):
    def __init__(self):
        super(j_satl_digit_system_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("JMA Length", "Period of Jurik MA", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Open", "Enable long entries", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Open", "Enable short entries", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Buy Close", "Enable closing longs", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Sell Close", "Enable closing shorts", "Trading")
        self._last_state = None

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

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

    def OnReseted(self):
        super(j_satl_digit_system_strategy, self).OnReseted()
        self._last_state = None

    def OnStarted(self, time):
        super(j_satl_digit_system_strategy, self).OnStarted(time)
        self._last_state = None
        jma = JurikMovingAverage()
        jma.Length = int(self.jma_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, self.process_candle).Start()
        tp_val = float(self.take_profit_percent) * 100.0
        sl_val = float(self.stop_loss_percent) * 100.0
        self.StartProtection(
            takeProfit=Unit(tp_val, UnitTypes.Percent),
            stopLoss=Unit(sl_val, UnitTypes.Percent))

    def process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        jma_value = float(jma_value)
        state = 3.0 if close > jma_value else 1.0
        if self._last_state is not None and self._last_state == state:
            return
        if state > 2.0:
            if self.sell_pos_close and self.Position < 0:
                self.BuyMarket()
            if self.buy_pos_open and self.Position <= 0:
                self.BuyMarket()
        else:
            if self.buy_pos_close and self.Position > 0:
                self.SellMarket()
            if self.sell_pos_open and self.Position >= 0:
                self.SellMarket()
        self._last_state = state

    def CreateClone(self):
        return j_satl_digit_system_strategy()
