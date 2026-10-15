# Estrategia Simple 2 MA I
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Simple 2 MA I es una estrategia de seguimiento de tendencia que replica la lógica central del asesor experto original de MetaTrader. Usa un par de medias móviles ponderadas lineales (LWMAs) calculadas sobre precios típicos para identificar la tendencia dominante. La confirmación de momentum y los filtros de dirección MACD eliminan señales débiles. La estrategia puede gestionar opcionalmente el riesgo mediante stop-loss automático, take-profit, movimientos a break-even y trailing stops basados en velas.

## Lógica de trading

### Configuración larga

1. La LWMA rápida está por encima de la LWMA lenta, confirmando una tendencia alcista.
2. El mínimo de la vela de hace dos barras está por debajo del máximo de la barra anterior, señalando una nueva estructura alcista.
3. Al menos una de las tres últimas lecturas de tasa de cambio está por encima del umbral de momentum configurado.
4. La línea MACD está por encima de la línea de señal.
5. El volumen neto de posición es inferior al límite `Max Net Volume`.

Cuando se cumplen todas las condiciones, la estrategia cierra la exposición corta (si la hay) y compra a mercado.

### Configuración corta

1. La LWMA rápida está por debajo de la LWMA lenta, confirmando una tendencia bajista.
2. El mínimo de la barra anterior está por debajo del máximo de la barra de hace dos períodos, indicando estructura bajista.
3. Al menos una de las tres últimas lecturas de tasa de cambio está por encima del umbral de momentum (valor absoluto).
4. La línea MACD está por debajo de la línea de señal.
5. El volumen neto de posición es inferior a `Max Net Volume`.

Cuando las condiciones se mantienen, la estrategia cubre largos (si los hay) y vende a mercado.

### Gestión de riesgos

* **Stop-loss / take-profit:** distancias fijas opcionales definidas en puntos relativos al precio de entrada.
* **Break-even:** cuando el precio alcanza la distancia de disparo en ganancia, el stop se mueve a entrada ± desplazamiento.
* **Trailing por vela:** después de alcanzar la distancia de activación, el stop sigue los extremos de la vela con un buffer configurable.
* Las órdenes de protección se cancelan automáticamente cuando la posición se cierra.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | --------------- |
| Candle Type | Marco temporal usado para cálculos de indicadores. | Velas de 15 minutos |
| Fast LWMA | Período de la LWMA rápida. | 6 |
| Slow LWMA | Período de la LWMA lenta. | 85 |
| Momentum Length | Período retrospectivo del indicador de tasa de cambio. | 14 |
| Momentum Threshold | Valor absoluto mínimo de tasa de cambio requerido. | 0.3 |
| MACD Fast | Longitud de EMA rápida usada en MACD. | 12 |
| MACD Slow | Longitud de EMA lenta usada en MACD. | 26 |
| MACD Signal | Longitud de EMA de señal usada en MACD. | 9 |
| Use Stop-Loss | Habilita la colocación de órdenes stop-loss. | true |
| Stop-Loss (points) | Distancia hasta el stop-loss desde el precio de entrada. | 20 |
| Use Take-Profit | Habilita la colocación de órdenes take-profit. | true |
| Take-Profit (points) | Distancia hasta el take-profit desde el precio de entrada. | 50 |
| Use Break-Even | Habilita el movimiento automático a break-even. | true |
| Break-Even Trigger | Ganancia (puntos) necesaria antes de break-even. | 30 |
| Break-Even Offset | Desplazamiento (puntos) añadido al mover a break-even. | 30 |
| Use Candle Trailing | Habilita trailing stops basados en extremos de velas. | true |
| Trailing Activation | Ganancia (puntos) requerida antes de activar trailing. | 40 |
| Trailing Padding | Distancia extra (puntos) añadida al extremo de la vela. | 10 |
| Max Net Volume | Volumen neto absoluto máximo permitido. | 1 |

## Notas

* Todas las distancias de precio se expresan en pasos de precio del valor (puntos). La estrategia multiplica automáticamente los valores de los parámetros por el tamaño de tick del valor.
* La asignación predeterminada de marcos temporales sigue los valores por defecto del experto original, pero puede ajustarse libremente.
* La estrategia espera velas terminadas. Las barras no finalizadas se ignoran para mantener coherencia con el EA fuente.
