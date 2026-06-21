# Estrategia DCA de Reversión a la Media con Bollinger Band
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compra una cantidad fija en dólares cuando el precio cruza por debajo de la banda inferior de Bollinger o el primer día de cada mes. Todas las posiciones se cierran en una fecha especificada.

## Parámetros
- `InvestmentAmount` - monto invertido cada vez
- `OpenDate` - fecha de inicio de compras
- `CloseDate` - fecha para cerrar todas las posiciones
- `StrategyMode` - reversión a la media BB, DCA mensual o combinado
- `BollingerPeriod` - período de las Bollinger Bands
- `BollingerMultiplier` - multiplicador de desviación estándar
- `CandleType` - marco temporal para el cálculo de Bollinger

## Indicadores
- Bollinger Bands
