# Estrategia Delta RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el indicador **Delta RSI**. Se comparan dos indicadores RSI con diferentes períodos:

- El **RSI Rápido** reacciona rápidamente a los cambios de precio.
- El **RSI Lento** actúa como filtro de tendencia.

Se abre una posición larga en la barra siguiente a una señal **Up** cuando:

1. El RSI lento está por encima del umbral `Level`.
2. El RSI rápido es mayor que el RSI lento.
3. La barra anterior mostró el estado Up y la barra actual ya no está en Up.

Se abre una posición corta en la barra siguiente a una señal **Down** cuando:

1. El RSI lento está por debajo de `100 - Level`.
2. El RSI rápido es menor que el RSI lento.
3. La barra anterior mostró el estado Down y la barra actual ya no está en Down.

Los flags opcionales permiten habilitar o deshabilitar la apertura y el cierre de posiciones largas y cortas por separado.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `FastPeriod` | Período del RSI rápido. |
| `SlowPeriod` | Período del RSI lento. |
| `Level` | Nivel umbral para el RSI lento. |
| `BuyPosOpen` / `SellPosOpen` | Permitir apertura de posiciones largas/cortas. |
| `BuyPosClose` / `SellPosClose` | Permitir cierre de posiciones largas/cortas. |
| `CandleType` | Marco temporal de las velas de entrada. |

La estrategia se suscribe a velas del marco temporal seleccionado, calcula ambos valores de RSI y procesa señales en cada vela finalizada. Cuando aparece una señal, la estrategia opcionalmente cierra la posición opuesta y abre una nueva en la dirección de la señal.
