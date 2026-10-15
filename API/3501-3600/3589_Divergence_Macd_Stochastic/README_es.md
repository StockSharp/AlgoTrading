# Divergencia MACD Stochastic Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el asesor experto MetaTrader 5 **"Divergencia EA pip sl tp"** en el marco StockSharp. El algoritmo busca divergencias clásicas entre la acción del precio y el histograma MACD, luego valida la señal con un filtro oscilador de sobrecompra/sobreventa Stochastic antes de abrir operaciones de reversión.

## Lógica comercial

1. Suscríbase a las velas de período de tiempo principales seleccionadas por el parámetro `CandleType`.
2. Calcule el histograma MACD (`MACD line - Signal line`) y los valores Stochastic %K/%D en cada vela terminada.
3. Realice un seguimiento de los dos últimos máximos y mínimos tanto del precio como de los valores del histograma.
4. **Divergencia bajista**: un nuevo máximo de precio más alto acompañado de un pico de histograma MACD más bajo y un Stochastic %K por encima de `StochasticUpperLevel` activa una posición corta o revierte una posición larga existente.
5. **Divergencia alcista**: un nuevo mínimo de precio más bajo con un histograma mínimo de MACD más alto y %K por debajo de `StochasticLowerLevel` se abre o se revierte en una posición larga.
6. Las protecciones opcionales `TakeProfitSteps` y `StopLossSteps` se convierten en StockSharp unidades de paso y se activan una vez cuando comienza la estrategia.

## Notas de implementación

- Construido con API de alto nivel de StockSharp utilizando una única suscripción de vela vinculada a los indicadores `MovingAverageConvergenceDivergenceSignal` y `StochasticOscillator`.
- Mantiene el estado de divergencia sin llamar a los ayudantes del indicador `GetValue`, cumpliendo con las pautas de conversión.
- La integración de gráficos muestra velas de precios, MACD y Stochastic líneas cuando hay un área de gráfico disponible.
- Las posiciones se invierten sumando el tamaño absoluto de la posición actual a la base `Volume`, lo que garantiza cambios de dirección inmediatos después de divergencias confirmadas.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Plazo utilizado para los cálculos de divergencia. | velas de 1 hora |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD EMA longitudes que replican las entradas originales EA. | 12 / 26 / 9 |
| `MacdDivergenceThreshold` | Diferencia mínima de histograma entre oscilaciones consecutivas requerida para confirmar la divergencia. | 0.0005 |
| `StochasticLength` | Periodo %K rápido del oscilador Stochastic. | 50 |
| `StochasticSlowK`, `StochasticSlowD` | Longitudes de suavizado %K/%D adicionales que reflejan la configuración EA. | 9 / 9 |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Filtros de sobrecompra y sobreventa que validan configuraciones bajistas/alcistas. | 80 / 20 |
| `TakeProfitSteps`, `StopLossSteps` | Distancias de protección opcionales expresadas en pasos de precio (0 desactiva el nivel). | 50 |

## Uso

1. Adjunte la estrategia a un conector StockSharp con un valor que admita el período de tiempo seleccionado.
2. Configure el tamaño de la posición a través de la propiedad base `Volume` y ajuste la configuración del indicador como desee.
3. Inicie la estrategia: las órdenes se generarán automáticamente cada vez que se cumplan las condiciones de divergencia y Stochastic.
