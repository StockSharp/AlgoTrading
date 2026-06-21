# Estrategia Elliott Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia que abre posiciones escalonadas cuando el oscilador Estocástico alcanza valores extremos en velas de cuatro horas. Coloca una orden a mercado inicial seguida de una cuadrícula de órdenes límite. Las posiciones se cierran una vez que se alcanza un objetivo de beneficio y la tendencia es confirmada por medias móviles y Bandas de Bollinger.

## Reglas de entrada
- Usar el oscilador Estocástico (longitud %K 21, suavizado 3) en velas H4.
- Cuando %K ≥ nivel de **Sobrecompra**:
  - Vender a mercado.
  - Colocar hasta ocho órdenes `SellLimit` adicionales por encima del precio actual a distancias de pips configuradas.
- Cuando %K ≤ nivel de **Sobreventa**:
  - Comprar a mercado.
  - Colocar hasta ocho órdenes `BuyLimit` adicionales por debajo del precio actual a distancias de pips configuradas.

## Reglas de salida
- La ganancia realizada alcanza **ProfitTarget** y el precio confirma la tendencia:
  - Las posiciones largas salen cuando el precio está por encima de la Banda de Bollinger inferior y la SMA de 200 períodos está por encima de la SMA de 55 períodos.
  - Las posiciones cortas salen cuando el precio está por debajo de la Banda de Bollinger superior y la SMA de 200 períodos está por debajo de la SMA de 55 períodos.
- Las órdenes límite de compra pendientes se cancelan cuando %K ≥ 90 y la SMA de 200 períodos ≤ SMA de 55 períodos.
- Las órdenes límite de venta pendientes se cancelan cuando %K ≤ 10 y la SMA de 200 períodos ≥ SMA de 55 períodos.

## Parámetros
- `StochLength` – período %K para el Estocástico.
- `OverboughtLevel` – nivel para comenzar a vender.
- `OversoldLevel` – nivel para comenzar a comprar.
- `ProfitTarget` – beneficio realizado requerido para cerrar posiciones abiertas.
- `Order2Offset` … `Order9Offset` – distancias en pips para órdenes límite adicionales.
- `CandleType` – marco temporal de las velas, predeterminado 4 horas.

## Indicadores
- StochasticOscillator
- BollingerBands
- SMA (200 y 55)
