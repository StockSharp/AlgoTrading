import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class center_of_gravity_osma_strategy(Strategy):
    """SMA*WMA and two smoothing stages, with local extrema trade signals."""

    def __init__(self):
        super(center_of_gravity_osma_strategy, self).__init__()
        self._period = self.Param("Period", 10).SetGreaterThanZero().SetDisplay("Period", "SMA and WMA length", "Indicator")
        self._smooth_period_1 = self.Param("SmoothPeriod1", 3).SetGreaterThanZero().SetDisplay("Smooth Period 1", "First smoothing length", "Indicator")
        self._smooth_period_2 = self.Param("SmoothPeriod2", 3).SetGreaterThanZero().SetDisplay("Smooth Period 2", "Second smoothing length", "Indicator")
        self._stop_loss = self.Param("StopLoss", 0.0).SetNotNegative().SetDisplay("Stop Loss", "Optional price-unit loss limit", "Risk")
        self._take_profit = self.Param("TakeProfit", 0.0).SetNotNegative().SetDisplay("Take Profit", "Optional price-unit target", "Risk")
        self._buy_pos_open = self.Param("BuyPosOpen", True).SetDisplay("Buy Pos Open", "Allow long entry", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True).SetDisplay("Sell Pos Open", "Allow short entry", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True).SetDisplay("Buy Pos Close", "Allow long exit", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True).SetDisplay("Sell Pos Close", "Allow short exit", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._reset_history()

    def _reset_history(self):
        self._closes = []
        self._products = []
        self._stage1 = []
        self._prior = None
        self._prior_prior = None
        self._pending_order = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(center_of_gravity_osma_strategy, self).OnReseted()
        self._reset_history()

    def OnStarted2(self, time):
        super(center_of_gravity_osma_strategy, self).OnStarted2(time)
        self._reset_history()
        self.StartProtection(
            takeProfit=Unit(self._take_profit.Value, UnitTypes.Absolute),
            stopLoss=Unit(self._stop_loss.Value, UnitTypes.Absolute))
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def _append(self, seq, value, length):
        seq.append(value)
        if len(seq) > length:
            seq.pop(0)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        length = int(self._period.Value)
        self._append(self._closes, float(candle.ClosePrice), length)
        if len(self._closes) < length:
            return
        sma = sum(self._closes) / length
        divisor = length * (length + 1) / 2
        wma = sum((index + 1) * value for index, value in enumerate(self._closes)) / divisor
        self._append(self._products, sma * wma, int(self._smooth_period_1.Value))
        if len(self._products) < int(self._smooth_period_1.Value):
            return
        self._append(self._stage1, sum(self._products) / len(self._products), int(self._smooth_period_2.Value))
        if len(self._stage1) < int(self._smooth_period_2.Value):
            return
        value = sum(self._stage1) / len(self._stage1)
        up = self._prior_prior is not None and self._prior < self._prior_prior and value > self._prior
        down = self._prior_prior is not None and self._prior > self._prior_prior and value < self._prior
        self._prior_prior, self._prior = self._prior, value
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if up:
            if self.Position < 0 and self._sell_pos_close.Value:
                self._pending_order = self.BuyMarket(-self.Position + (self.Volume if self._buy_pos_open.Value else 0))
            elif self.Position == 0 and self._buy_pos_open.Value:
                self._pending_order = self.BuyMarket(self.Volume)
        elif down:
            if self.Position > 0 and self._buy_pos_close.Value:
                self._pending_order = self.SellMarket(self.Position + (self.Volume if self._sell_pos_open.Value else 0))
            elif self.Position == 0 and self._sell_pos_open.Value:
                self._pending_order = self.SellMarket(self.Volume)

    def CreateClone(self):
        return center_of_gravity_osma_strategy()
