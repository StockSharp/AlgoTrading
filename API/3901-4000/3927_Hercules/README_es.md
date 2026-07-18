# Estrategia de Hércules
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Hercules es una adaptación StockSharp del experto MetaTrader **Hercules v1.3 (Majors)**. Combina un cruce de media móvil rápida/lenta con filtros de confirmación de múltiples marcos temporales y ejecuta dos objetivos de ganancias independientes por señal.

## Lógica de trading

* **Brazo de señal**: calcula un EMA rápido (1 período predeterminado) en el cierre de velas y un SMA lento (72 períodos) en la apertura de velas. Detecta cruces que ocurrieron en la última o penúltima barra. El precio cruzado se promedia entre ambas medias móviles y se coloca un nivel de activación `TriggerPips` por encima (para posiciones largas) o por debajo (para posiciones cortas).
* **Ventana de ejecución**: una vez que se detecta un cruce, la configuración sigue siendo válida durante dos barras completas. Solo cuando el cierre actual excede el precio de activación dentro de esta ventana se permite activar la orden.
* **Filtros** –
  * H1 RSI (longitud predeterminada 10, entrada de precio típica) debe estar por encima de `RsiUpper` para posiciones largas y por debajo de `RsiLower` para posiciones cortas.
  * El cierre actual debe romper el máximo/mínimo reciente recopilado durante `LookbackMinutes` de velas en el período de negociación.
  * El sobre diario (SMA 24 con ±`DailyEnvelopeDeviation`%) requiere que el precio cierre fuera de la banda en la dirección de la operación.
  * El sobre H4 (SMA 96 con ±`H4EnvelopeDeviation`%) agrega una segunda confirmación de plazo más alto.
* **Gestión de riesgos**: el stop-loss se establece en el máximo/mínimo de la barra cuatro velas atrás. El volumen puede fijarse (`OrderVolume`) o recalcularse a partir de `RiskPercent` del valor actual de la cartera.
* **Gestión comercial**: cada señal abre dos órdenes de mercado de igual volumen. El primero se liquida en `TakeProfitFirstPips`, el segundo en `TakeProfitSecondPips`. Un trailing stop de `TrailingStopPips` mantiene ambas órdenes protegidas. Cuando se completa la parada o ambos objetivos, la estrategia entra en un período de bloqueo de `BlackoutHours` durante el cual no se realizan nuevas operaciones.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `OrderVolume` | Volumen de cada orden de mercado antes de ajustes de gestión de dinero. |
| `UseMoneyManagement` | Cuando está habilitado, vuelve a calcular el volumen de `RiskPercent` de la cartera y la distancia de parada actual. |
| `RiskPercent` | Porcentaje del valor de la cartera al riesgo por configuración. |
| `TriggerPips` | Distancia del precio de cruce que debe superarse para permitir una entrada. |
| `TrailingStopPips` | Distancia del trailing stop en pips aplicada a la posición combinada. |
| `TakeProfitFirstPips` | Distancia del pip de la primera toma de ganancias parcial. |
| `TakeProfitSecondPips` | Distancia del pip de la segunda toma de ganancias parcial. |
| `FastPeriod` | Longitud de la línea de activación rápida EMA. |
| `SlowPeriod` | Longitud de la línea base lenta SMA. |
| `RsiPeriod` | Longitud del filtro de confirmación RSI. |
| `RsiUpper` / `RsiLower` | RSI umbrales que permiten operaciones largas y cortas. |
| `LookbackMinutes` | Ventana (en minutos) utilizada para calcular el filtro de ruptura alto/bajo reciente. |
| `BlackoutHours` | Horas para pausar después de una ejecución antes de aceptar una nueva configuración. |
| `DailyEnvelopePeriod` / `DailyEnvelopeDeviation` | Parámetros del filtro de envolvente diaria. |
| `H4EnvelopePeriod` / `H4EnvelopeDeviation` | Parámetros del filtro de envolvente H4. |
| `CandleType` | Plazo principal utilizado para la ejecución comercial. |
| `RsiTimeFrame` | Plazo que alimenta el indicador RSI. |
| `DailyTimeFrame` | Periodo que alimenta el cálculo de la dotación diaria. |
| `H4TimeFrame` | Periodo que alimenta el cálculo de la envolvente H4. |

## Archivos

* `CS/HerculesStrategy.cs` – Implementación en C# de la estrategia Hércules.
* `README.md` – este documento.
* `README_ru.md` – Descripción rusa.
* `README_zh.md` – Descripción china.
