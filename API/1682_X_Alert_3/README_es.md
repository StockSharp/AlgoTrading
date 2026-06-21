# Estrategia X-Alert 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la lógica del experto original **X_alert_3.mq4**. Monitorea dos medias móviles con parámetros configurables y produce una alerta informativa cuando se produce un cruce.

## Cómo funciona

1. Se calculan dos medias móviles en cada vela completada.
2. Se genera una alerta alcista cuando:
   - MA1 está por encima de MA2 en la vela actual.
   - MA1 está por encima de MA2 en la vela anterior.
   - MA1 estaba por debajo de MA2 hace dos velas.
3. Se genera una alerta bajista cuando:
   - MA1 está por debajo de MA2 en la vela actual.
   - MA1 está por debajo de MA2 en la vela anterior.
   - MA1 estaba por encima de MA2 hace dos velas.
4. La estrategia **no** abre ni cierra ninguna posición. Solo escribe mensajes en el registro.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `Ma1Period` | Período de la primera media móvil. | `1` |
| `Ma1Type` | Tipo de la primera media móvil (Simple, Exponential, Smoothed, Weighted). | `Simple` |
| `Ma2Period` | Período de la segunda media móvil. | `14` |
| `Ma2Type` | Tipo de la segunda media móvil. | `Simple` |
| `PriceType` | Precio fuente usado en los cálculos (Close, Open, High, Low, Median, Typical, Weighted). | `Median` |
| `CandleType` | Serie de velas usada para el procesamiento. | marco temporal de `1 minuto` |

## Notas

- La implementación rastrea las últimas dos diferencias entre las medias móviles para detectar cruces sin acceder directamente a valores históricos del indicador.
- Las alertas se escriben usando `AddInfoLog` para mantener la estrategia sin efectos secundarios.
- El parámetro de MetaTrader `RunIntervalSeconds` no es necesario en StockSharp y ha sido omitido.
