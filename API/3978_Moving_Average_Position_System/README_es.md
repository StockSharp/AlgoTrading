# Estrategia del sistema de posición de media móvil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

El sistema de posición de media móvil es un puerto directo del asesor experto MetaTrader 4 "MovingAveragePositionSystem.mq4". La estrategia monitorea una media móvil retrospectiva larga y reacciona a los cruces de precios que ocurren en velas completadas. Admite tanto la selección manual de lotes como una rutina opcional de aumento de volumen similar a una martingala que reacciona a las ganancias y pérdidas acumuladas expresadas en MetaTrader puntos.

## Lógica de trading

1. **Detección de señal**
   - El sistema calcula una media móvil configurable (simple, exponencial, suavizada o lineal ponderada).
   - Cuando el cierre de la vela finalizada más recientemente cruza la media móvil en la dirección opuesta al cierre anterior, la estrategia abre una nueva posición.
   - Sólo se permite una posición por dirección; si la estrategia ya es larga, no aumentará la posición hasta que se cierre la actual, y lo mismo se aplica a las operaciones cortas.
2. **Gestión de posiciones**
   - Si la vela que acaba de cerrar termina por debajo de la media móvil mientras una posición larga está abierta, la posición se cierra inmediatamente en el mercado.
   - Si la vela vuelve a cerrar por encima de la media móvil mientras hay una posición corta abierta, la venta corta se cierra.
   - Se puede activar una toma de ganancias estilo MetaTrader expresada en pasos de precio (puntos) a través de los parámetros de la estrategia. Por lo demás, las paradas son gestionadas por el cruce de media móvil.
3. **Gestión del dinero**
   - Cuando el bloque martingala está habilitado, la estrategia acumula PnL realizado y flotante en MetaTrader puntos para el ciclo actual.
   - Si las pérdidas acumuladas exceden el umbral de pérdida configurado, el siguiente volumen comercial se duplica (sin exceder nunca el tamaño máximo de lote) y todas las posiciones abiertas se nivelan.
   - Cuando las ganancias acumuladas exceden el objetivo de ganancias configurado, el volumen se restablece al tamaño del lote inicial y cualquier posición abierta se cierra para asegurar las ganancias.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **MaType** | Método de cálculo de la media móvil: simple, exponencial, suavizado o ponderado lineal. Refleja la entrada `TypeMA` del experto original. |
| **MaPeriodo** | Período retroactivo para la media móvil (predeterminado 240). |
| **MaShift** | Desplazamiento hacia adelante aplicado a los valores de media móvil antes de generar señales. Equivalente a la entrada `SdvigMA`. |
| **Tipo de vela** | Tipo de datos de vela utilizado para cálculos de señales. El valor predeterminado es velas de marco de tiempo de 1 hora. |
| **Volumen inicial** | Volumen utilizado antes de que la rutina de martingala lo modifique. Corresponde a la entrada `Lots`. |
| **Volumen inicial** | Tamaño de lote base al que se restablece la martingala después de un ciclo rentable (`StarLots`). |
| **Volumen máximo** | Límite superior para el volumen comercial (`MaxLots`). La estrategia reduce a la mitad el volumen de trabajo si se excede este límite. |
| **PérdidaUmbralPips** | Umbral de pérdida en MetaTrader puntos que desencadena un evento de duplicación de volumen (`LossPips`). |
| **Pips de umbral de beneficio** | Objetivo de beneficio en puntos que restablece el volumen al valor inicial (`ProfitPips`). |
| **TakeProfitPips** | Distancia de obtención de beneficios fija opcional en puntos aplicada a través del asistente de protección integrado (`TakeProfit`). |
| **UsarAdministración de Dinero** | Activa o desactiva la rutina de dimensionamiento de posiciones tipo martingala (`MM`). |

## Notas de uso

- Configurar la estrategia con el mismo símbolo y plazo que se utilizaron en MetaTrader; el período predeterminado de 240 funciona bien con velas H1, replicando la configuración original.
- Los umbrales de puntos suponen que el instrumento proporciona un `PriceStep` y un `StepPrice` válidos. Para los símbolos que carecen de estos metadatos, es posible que deba ajustar los umbrales manualmente.
- Debido a que el código original recalcula los márgenes antes de cada entrada, el puerto realiza un paso de normalización de volumen simplificado que reduce a la mitad el tamaño de la negociación cada vez que excede `MaxVolume`. Se pueden agregar controles de riesgo adicionales a través de los proveedores de riesgos estándar StockSharp si es necesario.
- Solo las velas completadas activan entradas y salidas, reflejando la implementación de MQL que verificó los valores de `Close[1]` y `Close[2]` en cada nueva barra.

## Archivos

- `CS/MovingAveragePositionSystemStrategy.cs`: implementación en C# de la lógica comercial utilizando la StockSharp estrategia de alto nivel API.
- `README.md` – Documentación en inglés (este archivo).
- `README_ru.md` – Documentación rusa.
- `README_zh.md` – Documentación china.
