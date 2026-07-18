import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class trade_ea_template_for_news_strategy(Strategy):
    """News template EA: simple candle direction entries with SL/TP via StartProtection."""

    def __init__(self):
        super(trade_ea_template_for_news_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 100) \
            .SetDisplay("Take Profit Points", "TP distance in price steps", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 100) \
            .SetDisplay("Stop Loss Points", "SL distance in price steps", "Risk")

        self._previous_open_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def TakeProfitPoints(self):
        return int(self._take_profit_points.Value)
    @property
    def StopLossPoints(self):
        return int(self._stop_loss_points.Value)

    def OnStarted2(self, time):
        super(trade_ea_template_for_news_strategy, self).OnStarted2(time)

        self._previous_open_price = None

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0

        if step > 0:
            tp_unit = Unit(step * self.TakeProfitPoints, UnitTypes.Absolute) if self.TakeProfitPoints > 0 else None
            sl_unit = Unit(step * self.StopLossPoints, UnitTypes.Absolute) if self.StopLossPoints > 0 else None
            if tp_unit is not None and sl_unit is not None:
                self.StartProtection(takeProfit=tp_unit, stopLoss=sl_unit)
            elif tp_unit is not None:
                self.StartProtection(takeProfit=tp_unit)
            elif sl_unit is not None:
                self.StartProtection(stopLoss=sl_unit)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._previous_open_price is None:
            self._previous_open_price = float(candle.OpenPrice)
            return

        previous_open = self._previous_open_price
        self._previous_open_price = float(candle.OpenPrice)

        if self.Position != 0:
            return

        close = float(candle.ClosePrice)

        if close > previous_open:
            self.BuyMarket()
        elif close < previous_open:
            self.SellMarket()

    def OnReseted(self):
        super(trade_ea_template_for_news_strategy, self).OnReseted()
        self._previous_open_price = None

    def CreateClone(self):
        return trade_ea_template_for_news_strategy()
