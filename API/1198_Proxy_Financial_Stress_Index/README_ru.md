# Proxy Financial Stress Index

Стратегия строит композитный индекс финансового стресса из нескольких рынков (VIX, доходность US 10Y, DXY, S&P 500, EUR/USD и HYG). Каждый ряд нормализуется через z-score и взвешивается. Когда индекс опускается ниже порога, открывается длинная позиция. Позиция закрывается после фиксированного числа баров.

## Условия входа
- Индекс стресса пересекает `Threshold` вниз.

## Условия выхода
- Закрытие после `HoldingPeriod` баров.

## Параметры
- `SmaLength` = 41
- `StdDevLength` = 20
- `Threshold` = -0.8
- `HoldingPeriod` = 28
- `VixWeight` = 0.4
- `Us10yWeight` = 0.2
- `DxyWeight` = 0.12
- `Sp500Weight` = 0.06
- `EurusdWeight` = 0.1
- `HygWeight` = 0.18

## Индикаторы
- SMA
- StandardDeviation
