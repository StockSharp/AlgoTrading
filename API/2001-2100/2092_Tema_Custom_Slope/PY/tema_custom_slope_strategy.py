import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import TripleExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class tema_custom_slope_strategy(Strategy):
    def __init__(self):
        super(tema_custom_slope_strategy, self).__init__()
        self._tema_length = self.Param("TemaLength", 12) \
            .SetDisplay("TEMA Length", "Length of the TEMA", "Indicators")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe of candles", "General")
        self._prev1 = None
        self._prev2 = None

    @property
    def tema_length(self):
        return self._tema_length.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tema_custom_slope_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None

    def OnStarted2(self, time):
        super(tema_custom_slope_strategy, self).OnStarted2(time)
        tema = TripleExponentialMovingAverage()
        tema.Length = self.tema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(tema, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, tema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, tema):
        if candle.State != CandleStates.Finished:
            return
        tema = float(tema)
        if self._prev1 is None or self._prev2 is None:
            self._prev2 = self._prev1
            self._prev1 = tema
            return

        falling = self._prev1 < self._prev2
        rising = self._prev1 > self._prev2
        turned_up = falling and tema > self._prev1
        turned_down = rising and tema < self._prev1

        self._prev2 = self._prev1
        self._prev1 = tema

        if turned_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turned_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return tema_custom_slope_strategy()
