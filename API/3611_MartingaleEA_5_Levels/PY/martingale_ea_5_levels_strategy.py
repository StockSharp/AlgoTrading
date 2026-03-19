import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class martingale_ea_5_levels_strategy(Strategy):
    """
    Martingale EA 5 Levels: SMA crossover entry (simplified, no martingale averaging in Python).
    """

    def __init__(self):
        super(martingale_ea_5_levels_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPercent", 0.5).SetDisplay("TP %", "Take profit percent", "Risk")
        self._stop_loss_pct = self.Param("StopLossPercent", 2.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_close = None
        self._prev_ma = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingale_ea_5_levels_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_ma = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(martingale_ea_5_levels_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = float(candle.ClosePrice)
        ma = float(sma_val)
        tp_pct = float(self._take_profit_pct.Value) / 100.0
        sl_pct = float(self._stop_loss_pct.Value) / 100.0
        if self.Position > 0 and self._entry_price > 0:
            if close >= self._entry_price * (1.0 + tp_pct) or close <= self._entry_price * (1.0 - sl_pct):
                self.SellMarket()
                self._entry_price = 0.0
                self._prev_close = close
                self._prev_ma = ma
                return
        elif self.Position < 0 and self._entry_price > 0:
            if close <= self._entry_price * (1.0 - tp_pct) or close >= self._entry_price * (1.0 + sl_pct):
                self.BuyMarket()
                self._entry_price = 0.0
                self._prev_close = close
                self._prev_ma = ma
                return
        if self._prev_close is not None and self._prev_ma is not None and self.Position == 0:
            if self._prev_close < self._prev_ma and close > ma:
                self.BuyMarket()
                self._entry_price = close
            elif self._prev_close > self._prev_ma and close < ma:
                self.SellMarket()
                self._entry_price = close
        self._prev_close = close
        self._prev_ma = ma

    def CreateClone(self):
        return martingale_ea_5_levels_strategy()
