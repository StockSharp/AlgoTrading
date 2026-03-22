import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ZigZag, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class last_zz50_strategy(Strategy):
    """
    ZigZag pivot midpoint strategy. Enters at midpoint of last two ZZ legs.
    """

    def __init__(self):
        super(last_zz50_strategy, self).__init__()
        self._deviation = self.Param("ZigZagDeviation", 0.003).SetDisplay("ZZ Deviation", "Percentage threshold", "ZigZag")
        self._sl_points = self.Param("StopLossPoints", 5).SetDisplay("Stop Loss", "SL in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 5).SetDisplay("Take Profit", "TP in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")

        self._zz = None
        self._pivots = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(last_zz50_strategy, self).OnReseted()
        self._pivots = []

    def OnStarted(self, time):
        super(last_zz50_strategy, self).OnStarted(time)
        self._zz = ZigZag()
        self._zz.Deviation = self._deviation.Value
        self.Indicators.Add(self._zz)

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        sl = Unit(self._sl_points.Value * step, UnitTypes.Absolute) if self._sl_points.Value > 0 else None
        tp = Unit(self._tp_points.Value * step, UnitTypes.Absolute) if self._tp_points.Value > 0 else None
        if sl is not None or tp is not None:
            self.StartProtection(tp, sl)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        result = self._zz.Process(CandleIndicatorValue(self._zz, candle))
        if not self._zz.IsFormed:
            return

        val = float(result) if result is not None else 0
        if val > 0:
            sec = self.Security
            step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.01
            if len(self._pivots) > 0 and abs(self._pivots[-1] - val) < step:
                self._pivots[-1] = val
            else:
                self._pivots.append(val)
                if len(self._pivots) > 50:
                    self._pivots.pop(0)

        if len(self._pivots) < 3:
            return

        b = self._pivots[-2]
        c = self._pivots[-3]
        price = float(candle.ClosePrice)
        mid_bc = (b + c) / 2.0

        if b < c:
            if price <= mid_bc and self.Position <= 0:
                self.BuyMarket()
        elif b > c:
            if price >= mid_bc and self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return last_zz50_strategy()
