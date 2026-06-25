# Estrategia Starter V6 Mod (Conversión a StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia **Starter V6 Mod** es una conversión al API de alto nivel de StockSharp del asesor experto de MetaTrader 5 `Starter_v6mod`. El sistema original combina un oscilador Laguerre RSI, dos medias móviles exponenciales, un filtro de índice de canal de materias primas y un módulo de gestión de posiciones en cuadrícula. Este port preserva la lógica de confirmación multicapa adaptando el manejo de posiciones, la gestión de capital y las acciones de protección al entorno StockSharp.

## Lógica de trading

### Indicadores

* **Proxy Laguerre RSI** – modelado mediante un RSI normalizado de 14 periodos para emular la escala 0-1 utilizada por el oscilador Laguerre original. El par de niveles `LevelDown` / `LevelUp` (por defecto 0,15 / 0,85) define las zonas de sobrevendido y sobrecomprado.
* **EMA lenta (120)** y **EMA rápida (40)** – calculadas sobre el precio mediano de la vela. Su desplazamiento relativo actúa como filtro de dirección de tendencia. El parámetro `AngleThreshold` convierte la diferencia de EMA en una distancia en ticks que condiciona las direcciones de trading.
* **Índice de Canal de Materias Primas (14)** – confirma la dirección del impulso requiriendo valores negativos para entradas largas y positivos para entradas cortas.

### Criterios de entrada

1. Determinar el sesgo de tendencia a partir de la diferencia de EMA:
   * Si la EMA lenta menos la EMA rápida es menor que `-AngleThreshold` ticks, solo se pueden iniciar posiciones largas.
   * Si la diferencia es mayor que `AngleThreshold`, solo se pueden iniciar posiciones cortas.
   * De lo contrario, el mercado se considera lateral y no se abren nuevas posiciones.
2. Cuando el sesgo de tendencia permite una dirección, verificar los filtros de oscilador e impulso:
   * Configuración larga – proxy Laguerre por debajo de `LevelDown`, EMA lenta < EMA lenta previa, EMA rápida < EMA rápida previa, y CCI < 0.
   * Configuración corta – proxy Laguerre por encima de `LevelUp`, EMA lenta > EMA lenta previa, EMA rápida > EMA rápida previa, y CCI > 0.
3. Espaciado de cuadrícula – al apilar posiciones en la misma dirección, el precio actual debe estar al menos `GridStepPips` por debajo de la entrada larga más baja o por encima de la entrada corta más alta. Esto replica la lógica de promediado del EA original.
4. Recuento de posiciones – el número total de entradas simultáneas en cuadrícula no puede superar `MaxOpenTrades`.

### Criterios de salida

* **Salidas Laguerre** – los largos cierran cuando el oscilador cruza por encima de `LevelUp`; los cortos cierran cuando cae por debajo de `LevelDown`.
* **Stop-loss / Take-profit** – expresados en pips, convertidos a incrementos de precio del instrumento. La conversión rastrea el ajuste original para símbolos con precios de 3/5 decimales.
* **Trailing stop** – se activa después de que el precio avanza `(TrailingStopPips + TrailingStepPips)` y luego sigue el precio con un desplazamiento de `TrailingStopPips`.
* **Protecciones del viernes** – no se permiten nuevas operaciones después de las 18:00 (hora del terminal) y todas las posiciones abiertas se liquidan después de las 20:00.

### Gestión de capital

* **Dimensionamiento de volumen** – fijo (`UseManualVolume = true`) o basado en riesgo. En modo riesgo, el volumen es igual a `(equity * RiskPercent) / (distancia StopLoss en unidades de precio)`.
* **Límite de capital** – el trading se detiene cuando el capital actual cae por debajo de `EquityCutoff`.
* **Límite de pérdidas diarias** – si la estrategia registra `MaxLossesPerDay` salidas perdedoras en la fecha actual, no se abren más posiciones.
* **Recuperación de pérdidas** – después de cada salida perdedora, el tamaño de la siguiente posición se divide por `DecreaseFactor^pérdidasHoy`, replicando la lógica de escalado de posiciones original.

## Notas de implementación

* La conversión utiliza el pipeline `SubscribeCandles().Bind(...)` de alto nivel de StockSharp para transmitir velas terminadas y valores de indicadores a la lógica de decisión.
* StockSharp no incluye un Laguerre RSI nativo, por lo que se utiliza un RSI normalizado como proxy. Los umbrales coinciden con el rango 0-1 de Laguerre.
* El filtro de ángulo EMA se reproduce midiendo la diferencia entre los valores de EMA lenta y rápida en ticks, proporcionando una puerta direccional similar al indicador personalizado `emaangle` original.
* La gestión manual de stops y trailing se realiza dentro de la rutina de procesamiento de velas para mantener paridad con las modificaciones de trailing de MQL.
* El registro contable de la cuadrícula rastrea el precio de entrada promedio, el precio de llenado más bajo/alto y los niveles de trailing para emular el flujo de trabajo multi-posición de MQL mientras se trabaja dentro del modelo de posición agregada de StockSharp.

## Parámetros

| Nombre | Por defecto | Descripción |
| ---- | ------- | ----------- |
| `UseManualVolume` | `false` | Alternar entre dimensionamiento de posición fijo y basado en riesgo. |
| `ManualVolume` | `1` | Volumen usado cuando el dimensionamiento manual está habilitado o el basado en riesgo no se puede calcular. |
| `RiskPercent` | `5` | Porcentaje del capital arriesgado por operación cuando el dimensionamiento automático está activo. |
| `StopLossPips` | `35` | Distancia del stop-loss en pips. |
| `TakeProfitPips` | `10` | Distancia del take-profit en pips. |
| `TrailingStopPips` | `0` | Distancia del trailing stop en pips (0 deshabilita el trailing). |
| `TrailingStepPips` | `5` | Avance mínimo antes de que el trailing stop comience a seguir el precio. |
| `DecreaseFactor` | `1.6` | Factor aplicado para reducir el tamaño después de cada pérdida. |
| `MaxLossesPerDay` | `3` | Máximo de salidas perdedoras permitidas por día calendario. |
| `EquityCutoff` | `800` | Umbral de capital que detiene las nuevas operaciones. |
| `MaxOpenTrades` | `10` | Número máximo de entradas simultáneas en cuadrícula. |
| `GridStepPips` | `30` | Espaciado mínimo entre entradas apiladas en la misma dirección. |
| `LongEmaPeriod` | `120` | Periodo de la EMA lenta. |
| `ShortEmaPeriod` | `40` | Periodo de la EMA rápida. |
| `CciPeriod` | `14` | Periodo del Índice de Canal de Materias Primas. |
| `AngleThreshold` | `3` | Umbral de diferencia de EMA expresado en ticks. |
| `LevelUp` | `0.85` | Nivel superior de Laguerre. |
| `LevelDown` | `0.15` | Nivel inferior de Laguerre. |
| `CandleType` | `15m` | Marco temporal de velas utilizado para los cálculos. |

## Consejos de uso

1. Configure el parámetro `CandleType` para que coincida con el marco temporal utilizado en la configuración original de MT5 (el EA a menudo se despliega en gráficos de 15 minutos).
2. Alinee la configuración de riesgo con las especificaciones de la cuenta. Al usar dimensionamiento basado en riesgo, asegúrese de que `StopLossPips` refleje la volatilidad del instrumento, ya que afecta directamente al volumen calculado.
3. Revise los horarios de trading de la bolsa. La protección del viernes integrada asume que el reloj del servidor se alinea con el cierre de sesión deseado.
4. Habilite el dibujo en gráfico (a través de `CreateChartArea`) para visualizar EMA, proxy RSI, CCI y operaciones ejecutadas para depuración u optimización.
5. Al trasladar conjuntos de parámetros de backtests de MT5, recuerde que el proxy RSI aproxima el oscilador Laguerre; puede ser necesario un ajuste fino de umbrales para coincidir con el timing de señal original.

## Archivos

* `CS/StarterV6ModStrategy.cs` – Implementación de la estrategia StockSharp.
* `README.md` – Documentación en inglés (este archivo).
* `README_zh.md` – Documentación en chino simplificado.
* `README_ru.md` – Documentación en ruso.
