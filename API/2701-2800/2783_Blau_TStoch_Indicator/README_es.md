# Estrategia con Indicador Blau TStoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Port del asesor experto de MetaTrader 5 `Exp_BlauTStochI` a la API de alto nivel de StockSharp.
- Tradea el Índice Estocástico Triple de Blau (William Blau) en marcos temporales configurables.
- Admite dos modos de ejecución: **Breakdown** (rupturas de línea cero) y **Twist** (reversiones de pendiente).
- Los permisos de posición reproducen los indicadores del asesor experto original (interruptores independientes para abrir/cerrar operaciones largas y cortas).

## Construcción del indicador
- Calcula una serie de momentum como `precio aplicado - mínimo` sobre `MomentumLength` barras y su rango `máximo - mínimo`.
- Aplica tres etapas de suavizado consecutivas tanto al numerador como al denominador.
- Métodos de suavizado soportados: Exponencial (EMA), Simple (SMA), Suavizado/Corriente (SMMA) y Lineal Ponderado (LWMA).
- Las opciones originales de MQL (JJMA, JurX, ParMA, T3, VIDYA, AMA) **no** se reproducen; el parámetro `Phase` se retiene por compatibilidad pero se ignora.
- Las opciones de precio aplicado coinciden con las enumeraciones MQL (cierre, apertura, máximo, mínimo, mediana, típico, ponderado, simple, cuartil, variantes de seguimiento de tendencia, DeMark).
- Valor final del indicador: `100 * stochSuavizado / rangoSuavizado - 50`.

## Reglas de trading
### Modo Breakdown
- Inspecciona el indicador en la barra definida por `SignalBar` (predeterminado 1, es decir, la última vela cerrada).
- **Entrada larga:** valor anterior (`SignalBar+1`) por encima de cero **y** valor actual (`SignalBar`) cruza por debajo o es igual a cero.
- **Entrada corta:** valor anterior por debajo de cero **y** valor actual cruza por encima o es igual a cero.
- **Salida larga:** valor anterior por debajo de cero y salidas largas permitidas.
- **Salida corta:** valor anterior por encima de cero y salidas cortas permitidas.

### Modo Twist
- **Entrada larga:** indicador subiendo (`value[SignalBar+1] < value[SignalBar+2]`) y el último valor no menor que el anterior.
- **Entrada corta:** indicador bajando (`value[SignalBar+1] > value[SignalBar+2]`) y el último valor no mayor que el anterior.
- **Salida larga:** la pendiente del indicador gira hacia abajo (`value[SignalBar+1] > value[SignalBar+2]`).
- **Salida corta:** la pendiente del indicador gira hacia arriba (`value[SignalBar+1] < value[SignalBar+2]`).

### Gestión de posición
- Las entradas revierten posiciones opuestas existentes añadiendo el tamaño absoluto de la posición al `Volume` configurado.
- Las salidas cierran la posición existente completa con órdenes de mercado.
- El procesamiento de operaciones se realiza solo en velas completadas y después de que el indicador esté completamente formado.

## Gestión de riesgo
- Stop-loss y take-profit opcionales medidos en pasos de precio (`StopLossPoints`, `TakeProfitPoints`).
- Ambos se implementan a través de `StartProtection` y se pueden deshabilitar estableciendo la distancia a cero.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|---------------------|
| `CandleType` | Tipo de dato/marco temporal para cálculos. | Velas de 4 horas |
| `Smoothing` | Método de suavizado (EMA/SMA/SMMA/LWMA). | EMA |
| `MomentumLength` | Retrospectiva para detección de máximos/mínimos. | 20 |
| `FirstSmoothing` | Longitud de la etapa de suavizado 1. | 5 |
| `SecondSmoothing` | Longitud de la etapa de suavizado 2. | 8 |
| `ThirdSmoothing` | Longitud de la etapa de suavizado 3. | 3 |
| `Phase` | Mantenido por compatibilidad (ignorado). | 15 |
| `PriceType` | Constante de precio aplicado. | Close |
| `SignalBar` | Desplazamiento de barra usado para evaluación de señal (>= 1). | 1 |
| `Mode` | Modo de trading (Breakdown/Twist). | Twist |
| `AllowLongEntries` | Habilitar entradas largas. | true |
| `AllowShortEntries` | Habilitar entradas cortas. | true |
| `AllowLongExits` | Habilitar cierre de operaciones largas. | true |
| `AllowShortExits` | Habilitar cierre de operaciones cortas. | true |
| `TakeProfitPoints` | Distancia de take-profit en pasos (0 deshabilita). | 2000 |
| `StopLossPoints` | Distancia de stop-loss en pasos (0 deshabilita). | 1000 |

## Diferencias con el experto MT5
- Los algoritmos de suavizado avanzados de SmoothAlgorithms.mqh no están implementados; elija entre EMA/SMA/SMMA/LWMA.
- La gestión de capital (dimensionamiento de lotes) está simplificada: la estrategia depende de la propiedad `Volume` de StockSharp.
- La evaluación de señales ocurre solo en velas completadas; no hay ejecución intra-barra.

## Notas de uso
- Asegúrese de que `SignalBar` permanezca en al menos 1; la implementación mantiene suficiente historial del indicador automáticamente.
- Aumentar las longitudes de suavizado incrementa el tiempo de formación porque cada etapa requiere la ventana completa para completarse.
- Para trading de reversión en marcos temporales más altos, considere ampliar las distancias de stop/take o deshabilitar un lado a través de los permisos.
