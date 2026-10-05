import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange
from StockSharp.Algo.Strategies import Strategy


class anomaly_counter_trend_strategy(Strategy):
    """
    Anomaly counter-trend strategy.
    Measures the percentage change of the close over the last LookbackMinutes. A rise of at least PercentageThreshold sells and
    a drop of at least PercentageThreshold buys, reversing an opposite position. Positions are closed by a stop-loss and a
    take-profit set in ticks.
    """

    def __init__(self):
        super(anomaly_counter_trend_strategy, self).__init__()
        self._percentage_threshold = self.Param("PercentageThreshold", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Percentage Threshold", "Percentage move that counts as an anomaly", "Anomaly Detection")
        self._lookback_minutes = self.Param("LookbackMinutes", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Minutes", "Window of the percentage change in minutes", "Anomaly Detection")
        self._stop_loss_ticks = self.Param("StopLossTicks", 100) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss Ticks", "Stop-loss distance in ticks", "Risk")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 200) \
            .SetNotNegative() \
            .SetDisplay("Take Profit Ticks", "Take-profit distance in ticks", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        super(anomaly_counter_trend_strategy, self).OnStarted2(time)

        frame = self.CandleType.Arg
        frame_minutes = frame.TotalMinutes if isinstance(frame, TimeSpan) and frame > TimeSpan.Zero else 1.0
        lookback_bars = max(1, int(round(self._lookback_minutes.Value / frame_minutes)))
        roc = RateOfChange()
        roc.Length = lookback_bars

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(roc, self._process_candle).Start()

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        take_ticks = self._take_profit_ticks.Value
        stop_ticks = self._stop_loss_ticks.Value
        self.StartProtection(
            Unit(step * take_ticks, UnitTypes.Absolute) if take_ticks > 0 else Unit(),
            Unit(step * stop_ticks, UnitTypes.Absolute) if stop_ticks > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, roc)

    def _process_candle(self, candle, change_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        change = float(change_value)
        threshold = float(self._percentage_threshold.Value)

        if change >= threshold and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif change <= -threshold and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return anomaly_counter_trend_strategy()
