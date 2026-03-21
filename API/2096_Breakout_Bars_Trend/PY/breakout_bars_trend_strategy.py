import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class breakout_bars_trend_strategy(Strategy):
    def __init__(self):
        super(breakout_bars_trend_strategy, self).__init__()
        self._negatives = self.Param("Negatives", 1) \
            .SetDisplay("Negative Signals", "Negative reversals before entry", "General")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 4.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._parabolic = None
        self._last_trend = 0
        self._negative_counter = 0

    @property
    def negatives(self):
        return self._negatives.Value
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
        super(breakout_bars_trend_strategy, self).OnReseted()
        self._parabolic = None
        self._last_trend = 0
        self._negative_counter = 0

    def OnStarted(self, time):
        super(breakout_bars_trend_strategy, self).OnStarted(time)
        self._parabolic = ParabolicSar()
        self.Indicators.Add(self._parabolic)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        sar_result = self._parabolic.Process(candle)
        if not sar_result.IsFormed:
            return
        sar_value = float(sar_result.ToDecimal())
        trend = 1 if sar_value < float(candle.ClosePrice) else -1

        if self._last_trend != 0 and self._last_trend != trend:
            if trend == 1 and self.Position < 0:
                self.BuyMarket()
            elif trend == -1 and self.Position > 0:
                self.SellMarket()
            self._negative_counter += 1
            if self._negative_counter > int(self.negatives):
                if trend == 1 and self.Position <= 0:
                    self.BuyMarket()
                elif trend == -1 and self.Position >= 0:
                    self.SellMarket()
                self._negative_counter = 0

        self._last_trend = trend

    def CreateClone(self):
        return breakout_bars_trend_strategy()
