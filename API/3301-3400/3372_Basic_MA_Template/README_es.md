# Estrategia básica de plantilla MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de plantilla de MA básica** es una fiel StockSharp versión del MetaTrader 4 asesor experto de la entrada del repositorio `MQL/27964`. El robot original negoció un solo símbolo en un promedio móvil de período de tiempo más alto y abrió una posición cada vez que la vela anterior cruzó el promedio. Esta versión de C# mantiene la estructura minimalista al tiempo que expone cada control como un parámetro para que el comportamiento se pueda ajustar u optimizar directamente dentro de StockSharp.

La plantilla espera una vela completamente terminada y compara sus precios de apertura y cierre con un promedio móvil desplazado. Si la barra abre por encima de la media y cierra por debajo de ella, la estrategia abre una posición corta. Cuando sucede lo contrario, se abre una operación larga. El sistema solo permite una posición de mercado a la vez, reflejando la verificación MQL "sin ticket activo". Las distancias protectoras de stop-loss y take-profit se definen en pips. Al inicio, la estrategia convierte esas distancias de pip en compensaciones de precio absoluto utilizando el paso del instrumento y la precisión decimal, replicando la lógica de conversión de punto a pip que dependía de los dígitos de cotización en MetaTrader.

## Lógica comercial

- **Fuente de datos**: una única serie de velas determinada por el parámetro `CandleType` (H4 predeterminado).
- **Indicador**: media móvil configurable (`SMA`, `EMA`, `SMMA` o `LWMA`). El parámetro `MovingAverageShift` mueve el indicador hacia adelante exactamente como la función MetaTrader `iMA`.
- **Reglas de entrada**:
  - Largo: la vela anterior se abrió por debajo y cerró por encima de la media móvil desplazada mientras no hay ninguna posición abierta.
  - Corto: la vela anterior se abrió por encima y cerró por debajo de la media móvil desplazada mientras no hay ninguna posición abierta.
- **Reglas de salida**: manejadas automáticamente por el módulo StockSharp `StartProtection` utilizando distancias de toma de ganancias y stop-loss basadas en pips. Cuando ambos objetivos son cero, la estrategia aún habilita el servicio de protección, por lo que las salidas manuales o de seguimiento siguen siendo posibles.
- **Filtro de posición**: la estrategia ignora las nuevas señales mientras una posición está activa, manteniendo el comportamiento idéntico a la rutina original `PosSelect()`.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Agregación de velas utilizada para señales. | H4 (velas de 4 horas) |
| `MovingAveragePeriod` | Duración del período de la media móvil. | 49 |
| `MovingAverageShift` | Desplazamiento hacia adelante aplicado al buffer de media móvil. | 0 |
| `MovingAverageMethod` | Modo de cálculo de media móvil (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |
| `TakeProfitPips` | Distancia de obtención de beneficios en pips convertida en compensaciones de precios absolutos en tiempo de ejecución. | 38,5 |
| `StopLossPips` | Distancia de stop-loss en pips convertida en compensaciones de precio absoluto en tiempo de ejecución. | 48,5 |

### Manejo de riesgos

El subsistema de protección recibe las distancias absolutas calculadas y las adjunta a cada orden de mercado. Debido a que el tamaño del pip se deriva del paso del símbolo y la precisión decimal (las cotizaciones de 5 y 3 dígitos multiplican el paso por diez), los niveles de parada respetan el espacio mínimo impuesto por los corredores en la versión MetaTrader.

### Notas de conversión

- La colocación de órdenes de dos pasos estilo ECN original se simplifica a StockSharp órdenes de mercado con protección automática, que ya maneja la vinculación de SL/TP después de la ejecución.
- Se omiten las rutinas `CheckVolumeValue` y `CheckMoneyForTrade`. El tamaño de la posición debe configurarse a través de la configuración de riesgo estándar StockSharp.
- Las declaraciones de registro se reemplazan por ganchos de dibujo de gráficos para que el promedio móvil y las operaciones ejecutadas se puedan visualizar directamente en el área del gráfico de estrategia.

Esta conversión mantiene el modelo de decisión idéntico al tiempo que adopta las API StockSharp idiomáticas de alto nivel (`SubscribeCandles`, `Bind` y `StartProtection`). Úselo como un andamio liviano para construir sistemas de media móvil más avanzados.
