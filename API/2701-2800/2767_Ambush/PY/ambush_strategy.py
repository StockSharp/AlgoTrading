import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ambush_strategy(Strategy):

    def __init__(self):
        super(ambush_strategy, self).__init__()
        self._indentation_points = self.Param("IndentationPoints", 10.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 10.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 1.0)
        self._equity_take_profit = self.Param("EquityTakeProfit", 15.0)
        self._equity_stop_loss = self.Param("EquityStopLoss", 5.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6)))
        self.Volume = 1
        self._previous_candle = None
        self._entry_price = 0.0
        self._stop_price = None
        self._price_step = 1.0

    @property
    def IndentationPoints(self):
        return self._indentation_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @property
    def EquityTakeProfit(self):
        return self._equity_take_profit.Value

    @property
    def EquityStopLoss(self):
        return self._equity_stop_loss.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ambush_strategy, self).OnStarted2(time)
        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        pos = float(self.Position)
        pnl = float(self.PnL)

        if float(self.EquityTakeProfit) > 0 and pnl >= float(self.EquityTakeProfit):
            self._flatten_position()
            self._previous_candle = candle
            return
        if float(self.EquityStopLoss) > 0 and pnl <= -float(self.EquityStopLoss):
            self._flatten_position()
            self._previous_candle = candle
            return

        if pos > 0 and self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
            self.SellMarket(pos)
            self._reset_targets()
        elif pos < 0 and self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
            self.BuyMarket(abs(pos))
            self._reset_targets()

        self._update_trailing(candle)

        pos = float(self.Position)
        if pos == 0 and self._previous_candle is not None:
            indentation = float(self.IndentationPoints) * self._price_step
            buy_level = float(self._previous_candle.HighPrice) + indentation
            sell_level = float(self._previous_candle.LowPrice) - indentation

            if float(candle.HighPrice) >= buy_level:
                self.BuyMarket(float(self.Volume))
                self._entry_price = float(candle.ClosePrice)
                trail_dist = (float(self.TrailingStopPoints) + float(self.TrailingStepPoints)) * self._price_step
                self._stop_price = self._entry_price - trail_dist if trail_dist > 0 else None
            elif float(candle.LowPrice) <= sell_level:
                self.SellMarket(float(self.Volume))
                self._entry_price = float(candle.ClosePrice)
                trail_dist = (float(self.TrailingStopPoints) + float(self.TrailingStepPoints)) * self._price_step
                self._stop_price = self._entry_price + trail_dist if trail_dist > 0 else None

        self._previous_candle = candle

    def _update_trailing(self, candle):
        if float(self.TrailingStopPoints) <= 0:
            return
        trail_dist = (float(self.TrailingStopPoints) + float(self.TrailingStepPoints)) * self._price_step
        if trail_dist <= 0:
            return
        pos = float(self.Position)
        if pos > 0:
            new_stop = float(candle.ClosePrice) - trail_dist
            if self._stop_price is None or new_stop > self._stop_price:
                self._stop_price = new_stop
        elif pos < 0:
            new_stop = float(candle.ClosePrice) + trail_dist
            if self._stop_price is None or new_stop < self._stop_price:
                self._stop_price = new_stop

    def _flatten_position(self):
        pos = float(self.Position)
        if pos > 0:
            self.SellMarket(pos)
        elif pos < 0:
            self.BuyMarket(abs(pos))
        self._reset_targets()

    def _reset_targets(self):
        self._entry_price = 0.0
        self._stop_price = None

    def OnReseted(self):
        super(ambush_strategy, self).OnReseted()
        self._previous_candle = None
        self._entry_price = 0.0
        self._stop_price = None
        self._price_step = 0.0

    def CreateClone(self):
        return ambush_strategy()
