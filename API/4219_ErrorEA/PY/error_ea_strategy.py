import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class error_ea_strategy(Strategy):
    """
    Port of the errorEA MetaTrader strategy that compares +DI and -DI lines of ADX.
    Buys when +DI > -DI, sells when -DI > +DI, with scaling and risk controls.
    """

    def __init__(self):
        super(error_ea_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Smoothing period for ADX", "Indicators")
        self._max_trades = self.Param("MaxTrades", 9) \
            .SetDisplay("Max Trades", "Maximum entries per direction", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss Points", "Stop distance in price steps", "Protection")
        self._take_profit_points = self.Param("TakeProfitPoints", 10) \
            .SetDisplay("Take Profit Points", "Take profit in price steps", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._adx = None
        self._long_trades = 0
        self._short_trades = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(error_ea_strategy, self).OnReseted()
        self._adx = None
        self._long_trades = 0
        self._short_trades = 0

    def OnStarted(self, time):
        super(error_ea_strategy, self).OnStarted(time)

        self._adx = AverageDirectionalIndex()
        self._adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._adx, self._process_candle).Start()

        tp = self._take_profit_points.Value
        sl = self._stop_loss_points.Value
        tp_unit = Unit(float(tp), UnitTypes.Absolute) if tp > 0 else None
        sl_unit = Unit(float(sl), UnitTypes.Absolute) if sl > 0 else None
        self.StartProtection(tp_unit, sl_unit, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._adx.IsFormed:
            return

        if adx_value.IsEmpty:
            return

        plus_di = 0.0
        minus_di = 0.0
        try:
            dx = adx_value.Dx
            if dx.Plus is not None:
                plus_di = float(dx.Plus)
            if dx.Minus is not None:
                minus_di = float(dx.Minus)
        except:
            return

        if plus_di > minus_di:
            self._handle_long_signal()
        elif minus_di > plus_di:
            self._handle_short_signal()

    def OnPositionReceived(self, position):
        super(error_ea_strategy, self).OnPositionReceived(position)
        if self.Position == 0:
            self._long_trades = 0
            self._short_trades = 0
        elif self.Position > 0:
            self._short_trades = 0
        else:
            self._long_trades = 0

    def _handle_long_signal(self):
        if self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._short_trades = 0

        if self._long_trades >= self._max_trades.Value:
            return

        self.BuyMarket()
        self._long_trades += 1

    def _handle_short_signal(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self._long_trades = 0

        if self._short_trades >= self._max_trades.Value:
            return

        self.SellMarket()
        self._short_trades += 1

    def CreateClone(self):
        return error_ea_strategy()
