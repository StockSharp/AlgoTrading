import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class scalp_rsi_strategy(Strategy):
    def __init__(self):
        super(scalp_rsi_strategy, self).__init__()
        self._buy_movement = self.Param("BuyMovement", 10.0).SetDisplay("Buy Movement", "RSI drop vs earlier period", "Buy")
        self._buy_period = self.Param("BuyPeriod", 2).SetDisplay("Buy Period", "Bars back for comparison", "Buy")
        self._buy_breakdown = self.Param("BuyBreakdown", 5.0).SetDisplay("Buy Breakdown", "RSI drop vs previous bar", "Buy")
        self._buy_rsi_value = self.Param("BuyRsiValue", 30.0).SetDisplay("Buy RSI", "RSI value threshold", "Buy")
        self._sell_movement = self.Param("SellMovement", 0.004).SetDisplay("Sell Movement", "RSI rise vs earlier period", "Sell")
        self._sell_period = self.Param("SellPeriod", 2).SetDisplay("Sell Period", "Bars back for comparison", "Sell")
        self._sell_breakdown = self.Param("SellBreakdown", 0.003).SetDisplay("Sell Breakdown", "RSI rise vs previous bar", "Sell")
        self._sell_rsi_value = self.Param("SellRsiValue", 30.0).SetDisplay("Sell RSI", "RSI value threshold", "Sell")
        self._buy_sl = self.Param("BuyStopLoss", 60).SetDisplay("Buy Stop Loss", "Ticks for stop loss", "Buy")
        self._buy_tp = self.Param("BuyTakeProfit", 3).SetDisplay("Buy Take Profit", "Ticks for take profit", "Buy")
        self._sell_sl = self.Param("SellStopLoss", 60).SetDisplay("Sell Stop Loss", "Ticks for stop loss", "Sell")
        self._sell_tp = self.Param("SellTakeProfit", 3).SetDisplay("Sell Take Profit", "Ticks for take profit", "Sell")
        self._buy_ma_length = self.Param("BuyMaLength", 14).SetDisplay("Buy RSI Length", "RSI period for buy", "Buy")
        self._sell_ma_length = self.Param("SellMaLength", 14).SetDisplay("Sell RSI Length", "RSI period for sell", "Sell")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(scalp_rsi_strategy, self).OnReseted()
        self._buy_rsi_history = []
        self._sell_rsi_history = []
        self._entry_price = 0

    def OnStarted(self, time):
        super(scalp_rsi_strategy, self).OnStarted(time)
        self._buy_rsi_history = []
        self._sell_rsi_history = []
        self._entry_price = 0
        self._step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._step = float(self.Security.PriceStep)

        buy_rsi = RelativeStrengthIndex()
        buy_rsi.Length = self._buy_ma_length.Value
        sell_rsi = RelativeStrengthIndex()
        sell_rsi.Length = self._sell_ma_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(buy_rsi, sell_rsi, self.OnProcess).Start()

    def OnProcess(self, candle, buy_rsi_val, sell_rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Update RSI history
        buy_max = max(self._buy_period.Value, 1) + 1
        self._buy_rsi_history.append(buy_rsi_val)
        if len(self._buy_rsi_history) > buy_max:
            self._buy_rsi_history.pop(0)

        sell_max = max(self._sell_period.Value, 1) + 1
        self._sell_rsi_history.append(sell_rsi_val)
        if len(self._sell_rsi_history) > sell_max:
            self._sell_rsi_history.pop(0)

        step = self._step
        bh = self._buy_rsi_history
        sh = self._sell_rsi_history
        bp = self._buy_period.Value
        sp = self._sell_period.Value

        # Buy signal
        buy_signal = (len(bh) > bp
            and len(bh) >= 2
            and bh[len(bh) - 1 - bp] - bh[-1] >= self._buy_movement.Value
            and bh[-2] - bh[-1] > self._buy_breakdown.Value
            and bh[-1] < self._buy_rsi_value.Value)

        # Sell signal
        sell_signal = (len(sh) > sp
            and len(sh) >= 2
            and sh[-1] - sh[len(sh) - 1 - sp] >= self._sell_movement.Value
            and sh[-1] - sh[-2] > self._sell_breakdown.Value
            and sh[-1] > self._sell_rsi_value.Value)

        if buy_signal and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close

        # Manage position exits
        if self.Position > 0 and self._entry_price > 0:
            sl = self._entry_price - self._buy_sl.Value * step
            tp = self._entry_price + self._buy_tp.Value * step
            if close <= sl or close >= tp:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0 and self._entry_price > 0:
            sl = self._entry_price + self._sell_sl.Value * step
            tp = self._entry_price - self._sell_tp.Value * step
            if close >= sl or close <= tp:
                self.BuyMarket()
                self._entry_price = 0

        if self.Position == 0:
            self._entry_price = 0

    def CreateClone(self):
        return scalp_rsi_strategy()
