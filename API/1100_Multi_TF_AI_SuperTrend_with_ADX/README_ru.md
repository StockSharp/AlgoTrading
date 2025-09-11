# Стратегия Multi-TF AI SuperTrend with ADX

Эта стратегия использует два индикатора SuperTrend и фильтр ADX. Направление тренда подтверждается сравнением ценовых WMA с WMA SuperTrend. Лонг открывается, когда оба SuperTrend бычьи и ADX показывает положительную силу. Шорт открывается при обратных условиях. ATR первого SuperTrend служит трейлинг-стопом.

- **Long**: оба SuperTrend в бычьем направлении, ценовые WMA выше линий SuperTrend, +DI > -DI и ADX выше порога.
- **Short**: оба SuperTrend в медвежьем направлении, ценовые WMA ниже линий SuperTrend, -DI > +DI и ADX выше порога.
- **Индикаторы**: SuperTrend, WMA, ATR, ADX.
- **Стопы**: трейлинг-стоп по ATR первого SuperTrend.
