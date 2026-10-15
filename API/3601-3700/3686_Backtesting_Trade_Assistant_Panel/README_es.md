# Backtesting de la estrategia del panel de asistente comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia del panel Backtesting Trade Assistant** es un asistente manual convertido del MetaTrader 4 asesor experto *Backtesting Trade Assistant Panel V1.10*. El script original creó un panel de control gráfico dentro del probador que permitía al operador cambiar el tamaño del lote, las distancias de parada de pérdidas y toma de ganancias, y enviar instantáneamente órdenes de mercado de COMPRA o VENTA. El puerto StockSharp ofrece el mismo flujo de trabajo dentro de un componente de estrategia al exponer parámetros fuertemente tipados y métodos de ayuda públicos en lugar de widgets en el gráfico.

Capacidades clave:

- Mantenga un volumen de órdenes configurable junto con distancias de stop-loss y take-profit estilo MetaTrader (medidas en “puntos”).
- Emita órdenes de mercado largas o cortas a pedido a través de los ayudantes `ManualBuy()` y `ManualSell()`.
- Adjunte automáticamente compensaciones de stop-loss y take-profit después de cada entrada manual utilizando los valores de puntos convertidos.
- Proporcione métodos de utilidad que actualicen el volumen de operaciones y las distancias de riesgo en tiempo de ejecución, imitando los campos de texto editables del panel MT4.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Volumen en lotes aplicado a órdenes de mercado manuales. Cambiar el valor también actualiza la base `Strategy.Volume`. | `0.1` |
| `StopLossPips` | Distancia desde el precio de llenado hasta el tope de protección, expresada en MetaTrader puntos. Establezca en `0` para deshabilitar la colocación automática de stop-loss. | `50` |
| `TakeProfitPips` | Distancia desde el precio de cumplimiento hasta el objetivo de ganancias, expresada en MetaTrader puntos. Configúrelo en `0` para deshabilitar la colocación automática de obtención de ganancias. | `100` |
| `MagicNumber` | Identificador conservado del EA original para contabilidad. No lo utiliza directamente la lógica de ejecución StockSharp, pero se puede hacer referencia a él en extensiones personalizadas. | `99` |

## Operaciones manuales
El EA original se basaba en botones en los que se podía hacer clic. En StockSharp las mismas acciones están disponibles como métodos públicos:

- `SetOrderVolume(decimal volume)`: sincroniza el parámetro `OrderVolume` y el valor interno `Strategy.Volume`.
- `SetStopLoss(decimal pips)` / `SetTakeProfit(decimal pips)`: ajusta las distancias de protección mientras se ejecuta la estrategia. Los valores se interpretan en MetaTrader puntos, exactamente como los cuadros de texto MT4.
- `ManualBuy()`: envía una orden de compra de mercado utilizando el volumen actual. Después de la ejecución, la estrategia convierte los puntos de stop-loss y take-profit configurados en compensaciones de precios (utilizando metadatos de símbolos) y llama a `SetStopLoss`/`SetTakeProfit` para registrar órdenes de protección para la posición neta resultante.
- `ManualSell()` – ayudante simétrico para órdenes de venta de mercado.
- `CloseAllPositions()` – cierra toda la exposición al precio de mercado. Esto refleja el flujo de trabajo en el que el evaluador podría aplanar posiciones manualmente.

Todas las distancias de protección se convierten con la misma heurística de tamaño de pip que en MT4: los símbolos de cinco y tres dígitos multiplican `PriceStep` por diez para obtener un único "punto", mientras que otros símbolos se basan en el `PriceStep` sin procesar. Si los datos de mercado no proporcionan los metadatos necesarios, se utiliza un tamaño alternativo de `0.0001` para preservar un comportamiento coherente.

## Notas de comportamiento
- La estrategia se suscribe a actualizaciones de Nivel 1 para realizar un seguimiento de la mejor oferta/demanda. Cuando esos precios no están disponibles, vuelve al último precio comercial antes de aplicar compensaciones protectoras.
- No se generan señales comerciales automáticas; este módulo actúa estrictamente como un asistente de ejecución manual al igual que el panel MT4.
- Debido a que StockSharp administra las órdenes de protección de forma nativa, no es necesario un número mágico explícito. El campo se incluye únicamente por igualdad con el asesor experto fuente.
- Las distancias de stop-loss y take-profit se pueden ajustar en cualquier momento antes de activar `ManualBuy()`/`ManualSell()` para emular la edición de los campos de texto MT4 antes de presionar los botones.

## Diferencias con el EA original
- La interfaz de usuario MetaTrader se reemplaza por parámetros de estrategia y llamadas a métodos. Toda la funcionalidad está disponible mediante programación sin representar controles de gráficos.
- El manejo del deslizamiento de la llamada MT4 `OrderSend` (fijado en 50 puntos) no se reproduce porque los ayudantes `BuyMarket`/`SellMarket` de StockSharp no exponen un argumento de deslizamiento directo. El entorno circundante debe gestionar la tolerancia de ejecución si es necesario.
- Las órdenes de protección se crean con los ayudantes de alto nivel `SetStopLoss`/`SetTakeProfit` de StockSharp en lugar de llamadas directas de `OrderSend`, lo que mantiene la implementación coherente con las convenciones de StockSharp.

## Consejos de uso
1. Configure el símbolo, la cartera y el conector que desee en StockSharp como de costumbre y luego inicie la estrategia.
2. Ajuste `OrderVolume`, `StopLossPips` y `TakeProfitPips` a través de la cuadrícula de parámetros o los métodos de configuración proporcionados.
3. Llame a `ManualBuy()` o `ManualSell()` cuando necesite una entrada discrecional. El ayudante adjuntará automáticamente las órdenes de protección solicitadas.
4. Utilice `CloseAllPositions()` para reducir la exposición instantáneamente durante pruebas retrospectivas o sesiones de negociación discrecional en vivo.
