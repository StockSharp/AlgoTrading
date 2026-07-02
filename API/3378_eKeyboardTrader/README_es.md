# Estrategia eKeyboardTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el comportamiento del asesor experto MetaTrader "eKeyboardTrader" utilizando la API de alto nivel de StockSharp. El script original escuchaba los atajos de teclado para enviar órdenes de mercado manuales y mostraba texto de ayuda directamente en el gráfico. En la versión StockSharp, las entradas interactivas se exponen como parámetros de estrategia, mientras que la lógica de ejecución, los controles de seguridad y las funciones de protección de órdenes permanecen fieles a la implementación de MQL.

## Lógica de trading
1. **Suscripción de Nivel1**: la estrategia se suscribe a los datos de mercado de Nivel1 para recibir los mejores precios de oferta y demanda más recientes. Estas cotizaciones son necesarias antes de que se pueda ejecutar una solicitud manual, imitando la dependencia MetaTrader de los datos de ticks actuales.
2. **Comandos manuales**: tres parámetros booleanos (`BuyRequest`, `SellRequest`, `CloseRequest`) representan los atajos de teclado originales (B, S y C). Cuando cualquier parámetro se establece en `true`, la estrategia realiza la acción de mercado correspondiente e inmediatamente restablece la bandera.
3. **Limitación de velocidad**: un tiempo de reutilización de un segundo protege contra envíos dobles accidentales, idéntico a la verificación del temporizador implementada en la versión MQL. Las solicitudes generadas durante el tiempo de reutilización esperan el siguiente ciclo de procesamiento.
4. **Protección de órdenes**: las distancias opcionales de stop-loss y take-profit, expresadas en MetaTrader puntos, se traducen a precios absolutos usando `Security.PriceStep`. Cuando se configura al menos una distancia de protección, la estrategia habilita la lógica incorporada `StartProtection` de StockSharp para que cada entrada manual reciba automáticamente las órdenes de protección configuradas.
5. **Reconocimiento de deslizamiento**: el parámetro `SlippagePoints` se conserva por motivos de compatibilidad y se menciona en el registro cada vez que se envía un pedido manual, emulando los comentarios informativos mostrados por el asesor experto.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Volumen base para órdenes de mercado manuales. |
| `StopLossPoints` | Distancia desde el precio de entrada hasta el stop de protección en MetaTrader puntos. Establezca en `0` para desactivar. |
| `TakeProfitPoints` | Distancia del precio de entrada al objetivo de protección en MetaTrader puntos. Establezca en `0` para desactivar. |
| `SlippagePoints` | Tolerancia de deslizamiento informativa que se muestra en el registro para cada pedido manual. |
| `BuyRequest` | Establezca en `true` para enviar una orden de compra de mercado (se reinicia automáticamente después del procesamiento). |
| `SellRequest` | Establezca en `true` para enviar una orden de venta de mercado (se reinicia automáticamente después del procesamiento). |
| `CloseRequest` | Establezca en `true` para aplanar la posición neta al precio de mercado (se reinicia automáticamente después del procesamiento). |

## Diferencias con la versión MQL
- Las indicaciones de texto en el gráfico y las notificaciones sonoras no se reproducen. En cambio, los mensajes de registro documentan las acciones realizadas.
- Las órdenes de protección se administran a través del asistente `StartProtection` de StockSharp, que envía órdenes de mercado cuando se alcanza el umbral en lugar de modificar tickets MetaTrader individuales.
- La entrada del teclado se reemplaza por alternancia de parámetros. Cualquier interfaz de usuario que aloje la estrategia puede asignar las interacciones del usuario (teclado, botones, scripts) a estos parámetros.
- Los diagnósticos de solicitudes comerciales de MetaTrader se condensan en declaraciones de registro para mantener la conversión ligera.

## Notas de uso
- Asigne tanto `Security` como `Portfolio` antes de comenzar la estrategia; Estas comprobaciones reflejan las condiciones de inicialización del asesor experto.
- Los indicadores de comando manual se evalúan cuando llegan nuevos datos de Nivel1. En un mercado tranquilo, las acciones se ejecutan en la siguiente cotización disponible.
- Ajustar `StopLossPoints` o `TakeProfitPoints` mientras se ejecuta la estrategia requiere reiniciarla para reconfigurar el módulo de protección, coincidiendo con la configuración de protección una vez por sesión del script original.
