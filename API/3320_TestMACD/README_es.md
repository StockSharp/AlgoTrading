# Estrategia Test MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
La **estrategia Test MACD** es una conversión fiel del asesor experto MetaTrader `TestMACD` a la API de alto nivel de StockSharp. Usa el indicador Moving Average Convergence Divergence (MACD) para detectar cambios de momentum y ejecuta operaciones cuando la línea MACD cruza la línea de señal en velas cerradas. La estrategia opera en un solo instrumento y marco temporal suministrado mediante el parámetro `CandleType`.

## Lógica de negociación
1. Suscribirse a datos de velas definidos por `CandleType` y calcular un indicador MACD con periodos rápido, lento y de señal configurables.
2. Monitorizar la diferencia de valor MACD (`MACD - Signal`) en cada vela terminada.
3. Disparar una **entrada alcista** cuando la diferencia cambia de signo de no positiva a positiva, lo que significa que la línea MACD cruzó por encima de la señal. Cualquier exposición corta se cierra antes de abrir el largo.
4. Disparar una **entrada bajista** cuando la diferencia cambia de signo de no negativa a negativa, lo que significa que la línea MACD cruzó por debajo de la señal. Cualquier exposición larga se cierra antes de abrir el corto.
5. Todas las órdenes se emiten a mercado con un volumen fijo configurado por `TradeVolume`.
6. Cada entrada se protege automáticamente con niveles de stop-loss y take-profit expresados en pasos de precio para replicar la gestión de riesgo basada en puntos del experto original.

## Gestión de riesgo
- Las distancias de stop-loss y take-profit replican las entradas de MetaTrader y se suministran en pasos de precio. Si el instrumento carece de información `PriceStep`, la estrategia usa distancias absolutas de precio con `MinPriceStep` o `1` como multiplicador.
- Las órdenes protectoras se crean una vez, al iniciar la estrategia, mediante `StartProtection`, asegurando que se apliquen a cada operación posterior sin reconfiguración.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `FastPeriod` | Longitud de EMA rápida usada en cálculos MACD. | `12` |
| `SlowPeriod` | Longitud de EMA lenta usada en cálculos MACD. | `24` |
| `SignalPeriod` | Longitud de EMA de señal para suavizado MACD. | `9` |
| `StopLossPoints` | Distancia de stop-loss expresada en pasos de precio. | `90` |
| `TakeProfitPoints` | Distancia de take-profit expresada en pasos de precio. | `110` |
| `TradeVolume` | Volumen fijo para todas las órdenes de mercado. | `1` |
| `CandleType` | Tipo de datos de velas y marco temporal suscrito por la estrategia. | `Marco de 30 minutos` |

## Notas de uso
- Adjunte la estrategia a un instrumento antes de iniciarla para que `PriceStep` y `MinPriceStep` estén disponibles.
- Asegúrese de que se proporcionen datos de mercado para el `CandleType` seleccionado; de lo contrario el indicador MACD no se formará y no habrá operaciones.
- La estrategia registra cada evento de cruce, facilitando el seguimiento de decisiones durante backtests.

## Detalles de conversión
- Las clases originales de MetaTrader `CSignalMACD`, `CTrailingNone` y `CMoneyFixedLot` se reemplazan por binding de indicadores StockSharp y mecanismos `StartProtection`.
- La lógica de `ExtStateMACD`, que comprobaba cruces MACD, se representa mediante un detector de cambio de signo en la diferencia MACD entre velas terminadas consecutivas.
- La gestión monetaria se simplifica a un parámetro de volumen fijo, muy parecido al comportamiento de lote fijo de `CMoneyFixedLot` cuando el dimensionamiento porcentual está desactivado.
