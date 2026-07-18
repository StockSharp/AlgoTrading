import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class mechanical_trading_strategy(Strategy):
    def __init__(self):
        super(mechanical_trading_strategy, self).__init__()
        self._profit_target = self.Param("ProfitTarget", 0.4) \
            .SetDisplay("Profit Target (%)", "Take profit percentage", "Risk Management")
        self._stop_loss = self.Param("StopLoss", 0.2) \
            .SetDisplay("Stop Loss (%)", "Stop loss percentage", "Risk Management")
        self._trade_hour = self.Param("TradeHour", 16) \
            .SetDisplay("Trade Hour", "Hour of the day to enter", "General")
        self._is_short = self.Param("IsShort", False) \
            .SetDisplay("Short Mode", "Enter short instead of long", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mechanical_trading_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(mechanical_trading_strategy, self).OnStarted2(time)
        self._entry_price = 0.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        hour = candle.OpenTime.Hour
        minute = candle.OpenTime.Minute
        tp_pct = float(self._profit_target.Value) / 100.0
        sl_pct = float(self._stop_loss.Value) / 100.0
        if self.Position > 0 and self._entry_price > 0.0:
            tp = self._entry_price * (1.0 + tp_pct)
            sl = self._entry_price * (1.0 - sl_pct)
            if close >= tp or close <= sl:
                self.SellMarket()
                self._entry_price = 0.0
                return
        elif self.Position < 0 and self._entry_price > 0.0:
            tp = self._entry_price * (1.0 - tp_pct)
            sl = self._entry_price * (1.0 + sl_pct)
            if close <= tp or close >= sl:
                self.BuyMarket()
                self._entry_price = 0.0
                return
        if hour != self._trade_hour.Value or minute != 0:
            return
        if self.Position != 0:
            return
        if self._is_short.Value:
            self.SellMarket()
        else:
            self.BuyMarket()
        self._entry_price = close

    def CreateClone(self):
        return mechanical_trading_strategy()
