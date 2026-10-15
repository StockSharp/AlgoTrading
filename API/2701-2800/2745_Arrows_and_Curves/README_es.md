# Estrategia de Flechas y Curvas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port en C# del asesor experto (EA) "Arrows and Curves" de MetaTrader 5. Replica la lógica basada en indicadores dentro de StockSharp usando la API de alto nivel. El sistema opera un único símbolo y reacciona a las señales del canal personalizado generadas por el indicador Arrows and Curves. Solo puede haber una posición activa a la vez, y cada nueva señal abre una nueva operación o cierra una existente.

## Lógica de la estrategia
- Las velas del marco temporal configurable se transmiten mediante `SubscribeCandles`. La rutina de procesamiento solo trabaja con velas finalizadas para replicar el comportamiento del EA en las aperturas de nuevas barras.
- El canal Arrows and Curves se reconstruye dentro de la estrategia: el algoritmo escanea el máximo más alto y el mínimo más bajo para la ventana de retroceso `SSP`, desplazada por el offset `Relay` igual que el indicador de MT5. A partir de esos valores se derivan dos envolventes (`Channel %` para la banda exterior y `Channel Stop %` para la banda interior).
- Las variables de estado del indicador (`uptrend` y `uptrend2`) se actualizan exactamente en el mismo orden que en el código MQL original. Cada vez que la vela anterior produce una flecha Sell, la estrategia prepara una entrada larga; y cada vez que produce una flecha Buy, prepara una entrada corta. Esto replica el comportamiento del EA donde las señales se leen con un índice de 1 en la siguiente barra.
- Cuando no hay posición abierta, la señal almacenada de la barra anterior se usa para abrir una orden de mercado en la dirección opuesta a la flecha (flecha Sell → operación de compra, flecha Buy → operación de venta).
- Cuando ya existe una posición y aparece una señal contraria, la posición actual se cierra pero no se abre inmediatamente una posición inversa, lo que coincide con la fuente MT5 donde primero ocurre el cierre y las entradas se evalúan nuevamente en la siguiente barra.

## Gestión de riesgos
- Las distancias de stop loss y take profit se definen en pips y se convierten en offsets de precio absolutos usando el `PriceStep` del instrumento. Para instrumentos cotizados con 3 o 5 decimales, la conversión multiplica el paso por diez, reproduciendo los ajustes de pips del EA.
- La funcionalidad de trailing stop replica el EA: una vez que el beneficio flotante supera `Trailing Stop + Trailing Step`, el stop de protección es arrastrado la distancia configurada respetando el paso mínimo.
- Los niveles de protección se verifican en cada vela completada usando el máximo y mínimo de la vela para aproximar los desencadenantes intrábarra.
- El tamaño de la posición puede fijarse mediante el parámetro `Volume`. Cuando `Volume` es cero, la estrategia deriva una cantidad dinámica arriesgando `Risk %` del valor de la cartera frente a la distancia de stop loss configurada.

## Parámetros
- `Volume`: tamaño de orden fijo. Establecer en cero para habilitar el dimensionamiento basado en riesgo.
- `Risk %`: porcentaje del valor de la cartera a arriesgar cuando el volumen es cero.
- `Stop Loss (pips)`: distancia del stop de protección en pips.
- `Take Profit (pips)`: distancia del objetivo de beneficio en pips.
- `Trailing Stop (pips)`: distancia del trailing stop en pips; establecer en cero para deshabilitar.
- `Trailing Step (pips)`: movimiento adicional mínimo requerido antes de que el trailing stop se desplace nuevamente.
- `SSP`: número de velas usadas para calcular el rango del canal.
- `Channel %`: porcentaje de la envolvente exterior, idéntico a la configuración de MT5.
- `Channel Stop %`: porcentaje de la envolvente interior usado para cambiar el estado secundario.
- `Relay`: desplazamiento aplicado al cálculo del canal.
- `Candle Type`: marco temporal o tipo de vela que alimenta el indicador.

## Notas de implementación
- La estrategia almacena solo la cantidad mínima de máximos, mínimos y cierres históricos requeridos por el indicador (`SSP + Relay + 5` barras).
- Todos los comentarios y métodos auxiliares están escritos en inglés para cumplir con las directrices del repositorio.
- A diferencia de MT5, las órdenes de stop loss y take profit se simulan sobre datos de velas, por lo que las ejecuciones intrábarra pueden diferir del EA original. Todo lo demás sigue las mismas reglas de decisión, haciendo que el port sea fiel al script fuente.
