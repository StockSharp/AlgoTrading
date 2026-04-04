import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class double_trading_strategy(Strategy):
    # TradeDirections: 0=Auto, 1=Buy, 2=Sell
    def __init__(self):
        super(double_trading_strategy, self).__init__()
        self._profit_target = self.Param("ProfitTarget", 500.0) \
            .SetDisplay("Profit Target", "Exit profit per round trip", "Risk")
        self._direction1 = self.Param("Direction1", 0) \
            .SetDisplay("Direction1", "Initial side", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles", "Data")
        self._max_round_trips = self.Param("MaxRoundTrips", 20) \
            .SetDisplay("Max Round Trips", "Max number of entry/exit cycles", "Risk")

        self._in_position = False
        self._side1 = Sides.Buy
        self._entry_price = 0.0
        self._last_price = 0.0
        self._round_trips = 0

    def OnReseted(self):
        super(double_trading_strategy, self).OnReseted()
        self._in_position = False
        self._side1 = Sides.Buy
        self._entry_price = 0.0
        self._last_price = 0.0
        self._round_trips = 0

    def OnStarted2(self, time):
        super(double_trading_strategy, self).OnStarted2(time)

        # 2=Sell maps to Sides.Sell, otherwise Sides.Buy
        self._side1 = Sides.Sell if self._direction1.Value == 2 else Sides.Buy
        self._in_position = False
        self._round_trips = 0

        sub = self.SubscribeCandles(self._candle_type.Value)
        sub.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._last_price = float(candle.ClosePrice)

        if not self._in_position:
            if self._round_trips >= self._max_round_trips.Value:
                return

            # Enter position
            self._entry_price = float(candle.ClosePrice)

            if self._side1 == Sides.Buy:
                self.BuyMarket()
            else:
                self.SellMarket()

            self._in_position = True
            return

        # Check profit target
        if self._side1 == Sides.Buy:
            pnl = self._last_price - self._entry_price
        else:
            pnl = self._entry_price - self._last_price

        if pnl >= float(self._profit_target.Value):
            # Exit position
            if self._side1 == Sides.Buy:
                self.SellMarket()
            else:
                self.BuyMarket()

            self._in_position = False
            self._round_trips += 1

            # Alternate direction for next round
            if self._side1 == Sides.Buy:
                self._side1 = Sides.Sell
            else:
                self._side1 = Sides.Buy

    def CreateClone(self):
        return double_trading_strategy()
