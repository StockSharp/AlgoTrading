# Estrategia Abrir Dos Órdenes Pendientes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el asesor experto de MetaTrader que simultáneamente coloca una orden buy stop y una orden sell stop alrededor del spread actual. Trabaja sobre un único instrumento y utiliza llamadas al API de alto nivel de StockSharp para suscribirse al libro de órdenes, gestionar órdenes pendientes y manejar controles de riesgo de cartera. Tan pronto como se llena una orden pendiente, se cancela la orden opuesta y la posición activa se gestiona con reglas de stop-loss, take-profit y trailing stop.

## Lógica de Trading
1. Suscribirse al libro de órdenes y leer los mejores precios bid y ask.
2. Cuando no hay posición abierta ni orden de entrada activa, calcular el volumen de entrada y colocar dos órdenes stop:
   - Buy stop en *ask + EntryOffsetPoints × PriceStep*.
   - Sell stop en *bid − EntryOffsetPoints × PriceStep*.
3. Cuando se ejecuta una orden stop:
   - Cancelar la orden pendiente opuesta.
   - Almacenar el precio de ejecución como el nuevo precio de entrada.
   - Calcular los niveles iniciales de stop-loss y take-profit en pasos de precio relativos al llenado.
4. Mientras la posición está activa, monitorear el libro de órdenes:
   - Cerrar largos cuando el bid alcanza el nivel de stop-loss o take-profit.
   - Cerrar cortos cuando el ask alcanza el nivel de stop-loss o take-profit.
   - Activar el trailing stop después de que el precio se mueva a favor de la operación por la distancia de trailing y deslizar el nivel de stop en consecuencia.
5. Cuando la posición vuelve a plana, restablecer el estado interno y colocar un nuevo par de órdenes stop.

Las salidas se ejecutan con órdenes de mercado una vez que se toca un nivel protector. Esto mantiene la lógica cercana a la implementación MQL sin depender de APIs de modificación de órdenes de nivel inferior.

## Gestión de Capital
La estrategia puede usar ya sea un volumen fijo o un dimensionamiento dinámico basado en riesgo:
- **Volumen Fijo** – usar el tamaño de lote constante definido por el parámetro `FixedVolume`.
- **Gestión de Capital** – si está habilitado, calcular el volumen a partir del capital del portafolio, el porcentaje de riesgo y la distancia del stop-loss en pasos de precio. Los volúmenes se redondean al paso de volumen del instrumento y se limitan entre los valores mínimo y máximo del instrumento.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `UseMoneyManagement` | Habilita el dimensionamiento de posición basado en riesgo. Predeterminado: `true`. |
| `RiskPercent` | Porcentaje del capital del portafolio a arriesgar por operación cuando la gestión de capital está activa. Predeterminado: `2`. |
| `FixedVolume` | Tamaño de lote usado cuando la gestión de capital está deshabilitada. Predeterminado: `1`. |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio desde el precio de entrada. Predeterminado: `100`. |
| `TakeProfitPoints` | Distancia de take-profit en pasos de precio desde el precio de entrada. Predeterminado: `300`. |
| `TrailingStopPoints` | Distancia del trailing stop en pasos de precio. Un valor de `0` deshabilita el trailing. Predeterminado: `50`. |
| `EntryOffsetPoints` | Distancia en pasos de precio usada para colocar las órdenes pendientes lejos del spread. Predeterminado: `50`. |
| `SlippagePoints` | Amortiguador adicional en pasos de precio reservado para el deslizamiento. Actualmente informativo y no se usa directamente. Predeterminado: `5`. |

## Notas
- La estrategia depende del feed del libro de órdenes. Asegúrese de que los datos de profundidad de mercado estén disponibles para el instrumento seleccionado.
- La ejecución de stop-loss y take-profit usa órdenes de mercado una vez que el bid/ask cruza el nivel, coincidiendo con el comportamiento de la lógica de trailing stop del MQL original.
- Los trailing stops comienzan solo después de que el precio se ha movido por la distancia de trailing configurada desde la entrada.
- El código usa sangría de tabulación, comentarios en inglés y métodos de alto nivel de StockSharp según las directrices del proyecto.
