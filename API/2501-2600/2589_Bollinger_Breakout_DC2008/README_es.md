# Ruptura de Bollinger DC2008
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reimplementación del asesor experto de ruptura de Bollinger de Sergey Pavlov (DC2008) de MetaTrader para la API de estrategia de alto nivel de StockSharp. La estrategia observa velas completadas, evalúa rupturas de las Bandas de Bollinger en la fuente de precio seleccionada y abre o revierte posiciones solo cuando la operación actual no está perdiendo.

## Descripción general
- Calcula una envolvente de Bandas de Bollinger en el marco temporal configurado y el precio aplicado (cierre, apertura, máximo, mínimo, mediana, típico, ponderado o promedio).
- Genera configuraciones **largo** cuando el mínimo de la vela cierra por debajo de la banda inferior mientras el máximo permanece bajo la banda media (fuerte extensión bajista que debería revertir).
- Genera configuraciones **corto** cuando el máximo de la vela supera la banda superior mientras el mínimo se mantiene por encima de la banda media (fuerte extensión alcista que se espera revierta).
- El experto MQL original operaba en ticks; en este port los señales se procesan una vez por vela terminada para mayor estabilidad y coherencia del indicador.
- Las posiciones solo se abren o revierten si la posición existente muestra un beneficio no realizado no negativo, replicando el filtro de riesgo original.

## Lógica de trading
### Pipeline del indicador
1. Suscribirse a velas del `CandleType` elegido (por defecto: marco temporal de 1 hora).
2. Alimentar el precio aplicado seleccionado en el indicador de Bandas de Bollinger (`Length = BandsPeriod`, `Width = BandsDeviation`).
3. Ignorar velas hasta que el indicador produzca valores válidos de banda superior, media e inferior.

### Criterios de entrada
- **Comprar**: `Low < LowerBand` **y** `High < MiddleBand`. Indica que toda la vela operó por debajo de la línea media tras perforar la banda inferior.
- **Vender**: `High > UpperBand` **y** `Low > MiddleBand`. Indica que toda la vela operó por encima de la línea media tras perforar la banda superior.

### Filtro de posición y gestión
- Si **no hay posición**, la estrategia abre una orden de mercado con el `Volume` configurado cuando aparece una señal.
- Si ya existe una posición:
  - Cuando la señal es opuesta a la dirección actual, calcular el beneficio no realizado como `Position * (Close - PositionPrice)` usando el cierre de la vela.
  - Si el beneficio no realizado es **negativo**, omitir todas las acciones para esta vela (idéntico al `return` temprano del original).
  - Si el beneficio no realizado es **no negativo** y la señal es opuesta, enviar una orden de mercado de reversión de tamaño `Volume + |Position|` para tanto aplanar la posición actual como establecer una nueva en la dirección de la señal.
  - Las señales que coinciden con la dirección actual no añaden a la posición (igual que la versión MQL).
- No hay órdenes de stop-loss o take-profit explícitas; las salidas de operaciones ocurren solo mediante señales opuestas que satisfacen el filtro de beneficio.

## Parámetros
| Nombre | Valor predeterminado | Descripción |
| --- | --- | --- |
| `BandsPeriod` | 80 | Número de velas utilizadas para calcular la media móvil y las desviaciones de Bollinger. Debe ser positivo y está disponible para optimización. |
| `BandsDeviation` | 3.0 | Multiplicador de desviación estándar aplicado al ancho de las Bandas de Bollinger. Positivo, optimizable. |
| `AppliedPrice` | Close | Fuente de precio para el indicador: Close, Open, High, Low, Median, Typical, Weighted o Average (OHLC/4). Refleja `ENUM_APPLIED_PRICE` de MetaTrader. |
| `CandleType` | Marco temporal de 1 hora | Tipo de vela (marco temporal) usado para análisis y órdenes. Se puede cambiar a cualquier otro tipo de datos compatible con StockSharp. |
| `Volume` (heredado) | dependiente del broker | Tamaño de orden para nuevas entradas. Las reversiones añaden automáticamente el tamaño absoluto de la posición existente. |

## Diferencias con el experto MQL original
- El EA MetaTrader evaluaba condiciones en cada tick; este port C# espera a que las velas estén terminadas para evitar actuar sobre datos incompletos.
- El desplazamiento del indicador estaba fijado en cero en el EA fuente y aquí permanece implícito; no se exponen desplazamientos adicionales.
- MetaTrader reportaba el beneficio flotante directamente; el port lo aproxima vía cierre de vela y `PositionPrice`, lo que es suficiente para la comparación de signo usada por el filtro.
- La gestión de operaciones, mensajes de cadena y comentarios de órdenes de la versión MQL se omiten, centrándose puramente en la generación de señales.

## Notas de implementación
- Las velas, indicadores y llamadas de trading dependen de las APIs de alto nivel de StockSharp (`SubscribeCandles().Bind(...)`, `BuyMarket`, `SellMarket`).
- El indicador se dibuja automáticamente si hay un área de gráfico disponible en la UI; las operaciones también se representan para depuración.
- La estrategia reinicia y reconstruye el indicador en cada inicio, por lo que los cambios de parámetros tienen efecto inmediato en la próxima ejecución.
