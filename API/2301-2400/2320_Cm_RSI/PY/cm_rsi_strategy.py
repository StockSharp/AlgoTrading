import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Convert
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class cm_rsi_strategy(Strategy):
    """Wilder RSI on completed candle opens, with volume and price-step protection."""

    def __init__(self):
        super(cm_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._buy_level = self.Param("BuyLevel", 30.0).SetDisplay("Buy Level", "RSI level to enter long", "Indicators")
        self._sell_level = self.Param("SellLevel", 70.0).SetDisplay("Sell Level", "RSI level to enter short", "Indicators")
        self._take_profit = self.Param("TakeProfit", 200).SetDisplay("Take Profit", "Take profit in price steps", "Risk Management")
        self._stop_loss = self.Param("StopLoss", 100).SetDisplay("Stop Loss", "Stop loss in price steps", "Risk Management")
        self._order_volume = self.Param("OrderVolume", 0.1).SetDisplay("Order Volume", "Volume of each trade", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")
        self._reset_rsi()

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def BuyLevel(self):
        return self._buy_level.Value

    @BuyLevel.setter
    def BuyLevel(self, value):
        self._buy_level.Value = value

    @property
    def SellLevel(self):
        return self._sell_level.Value

    @SellLevel.setter
    def SellLevel(self, value):
        self._sell_level.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_rsi(self):
        zero = Decimal(0)
        self._previous_open = None
        self._previous_rsi = None
        self._sum_gain = self._sum_loss = zero
        self._average_gain = self._average_loss = zero
        self._changes = 0

    def OnReseted(self):
        super(cm_rsi_strategy, self).OnReseted()
        self._reset_rsi()

    def OnStarted2(self, time):
        super(cm_rsi_strategy, self).OnStarted2(time)
        self._reset_rsi()
        step = self.Security.PriceStep
        if step <= 0:
            raise ValueError("Cm RSI requires a positive Security.PriceStep for point-based stops")
        self.SubscribeCandles(self.CandleType).Bind(self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(Decimal(self.TakeProfit) * step, UnitTypes.Absolute) if self.TakeProfit > 0 else Unit(),
            stopLoss=Unit(Decimal(self.StopLoss) * step, UnitTypes.Absolute) if self.StopLoss > 0 else Unit(),
        )

    @staticmethod
    def _to_rsi(avg_gain, avg_loss):
        zero = Decimal(0)
        if avg_loss == zero:
            return Decimal(50) if avg_gain == zero else Decimal(100)
        if avg_gain == zero:
            return zero
        return Decimal(100) * avg_gain / (avg_gain + avg_loss)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = candle.OpenPrice
        if self._previous_open is None:
            self._previous_open = price
            return
        change = price - self._previous_open
        self._previous_open = price
        gain = max(change, Decimal(0))
        loss = max(-change, Decimal(0))
        period = int(self.RsiPeriod)
        if period <= 0:
            raise ValueError('RsiPeriod must be positive')
        if self._changes < period:
            self._sum_gain += gain
            self._sum_loss += loss
            self._changes += 1
            if self._changes < period:
                return
            self._average_gain = self._sum_gain / Decimal(period)
            self._average_loss = self._sum_loss / Decimal(period)
            self._previous_rsi = self._to_rsi(self._average_gain, self._average_loss)
            return
        self._average_gain = (self._average_gain * Decimal(period - 1) + gain) / Decimal(period)
        self._average_loss = (self._average_loss * Decimal(period - 1) + loss) / Decimal(period)
        rsi = self._to_rsi(self._average_gain, self._average_loss)
        if self._previous_rsi is not None and self.IsFormedAndOnlineAndAllowTrading():
            position = self.Position
            if self._previous_rsi < Convert.ToDecimal(self.BuyLevel) and rsi > Convert.ToDecimal(self.BuyLevel) and position <= 0:
                if position < 0:
                    self.BuyMarket(-position)
                self.BuyMarket(Convert.ToDecimal(self.OrderVolume))
            elif self._previous_rsi > Convert.ToDecimal(self.SellLevel) and rsi < Convert.ToDecimal(self.SellLevel) and position >= 0:
                if position > 0:
                    self.SellMarket(position)
                self.SellMarket(Convert.ToDecimal(self.OrderVolume))
        self._previous_rsi = rsi

    def CreateClone(self):
        return cm_rsi_strategy()
