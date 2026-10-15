# Estrategia de Hora de Apertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Hora de Apertura es un sistema de trading con programación horaria que replica el comportamiento del asesor experto de MetaTrader 5 *OpenTime*. La estrategia observa el reloj del mercado en velas cerradas y abre operaciones únicamente dentro de una ventana de tiempo configurable. Puede cerrar cualquier posición activa durante una ventana de salida dedicada, aplicar un trailing stop opcional y aplicar reglas básicas de stop-loss y take-profit expresadas en pips.

A diferencia de la versión original de cobertura, este port de StockSharp funciona en una cartera neteada: cuando aparece una señal que entra en conflicto con la posición actual, la estrategia primero cierra la exposición opuesta y luego abre la dirección solicitada con el volumen configurado.

## Flujo de operaciones
1. **Ventana de cierre** – Si el indicador *Use Close Window* está activado y el tiempo actual cae dentro de la ventana de cierre, la estrategia cierra inmediatamente cualquier posición abierta. No se permite ninguna nueva operación hasta que finalice la ventana.
2. **Actualización del trailing** – Cuando el trailing está activado y el mercado se ha movido al menos `TrailingStop + TrailingStep` pips a favor de la posición actual, el trailing stop se acerca al precio en la distancia definida en `TrailingStop`. Esto reproduce la lógica de MT5 donde el nivel de stop se modifica sólo después de un paso mínimo.
3. **Verificaciones de riesgo** – En cada vela cerrada, la estrategia verifica si los umbrales de stop-loss o take-profit han sido alcanzados. Si algún nivel es tocado, la posición se cierra y todo el estado interno de ese lado se reinicia.
4. **Ventana de entrada** – Cuando el tiempo está dentro de la ventana de operaciones, la estrategia evalúa los interruptores de dirección:
   - Si las entradas largas están habilitadas y la posición neta actual es plana o corta, compra el volumen configurado más cualquier cantidad requerida para cubrir una posición corta existente.
   - Si las entradas cortas están habilitadas y la posición neta es plana o larga, vende el volumen configurado más cualquier cantidad requerida para aplanar una posición larga existente.

Cada entrada ejecutada almacena el precio de entrada junto con los desplazamientos de stop y objetivo (si son distintos de cero). Estos valores son reutilizados por la lógica de trailing y las posteriores verificaciones de salida.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| Candle Type | Velas de 1 minuto | Tipo de dato usado para el seguimiento del tiempo; la estrategia reacciona sólo en velas cerradas. |
| Use Close Window | true | Habilita la ventana de cierre automático. |
| Close Hour / Close Minute | 20:50 | Inicio de la ventana de cierre. La hora admite valores 0–24; 24 pasa al día siguiente. |
| Enable Trailing | false | Activa la lógica de trailing stop. |
| Trailing Stop | 30 pips | Distancia entre el precio y el trailing stop. Se convierte a unidades de precio según el tamaño de tick del instrumento. |
| Trailing Step | 3 pips | Movimiento adicional requerido antes de que el trailing stop avance de nuevo. |
| Trade Hour / Trade Minute | 18:50 | Hora de inicio de la ventana de trading que permite nuevas entradas. |
| Duration | 300 segundos | Duración compartida por las ventanas de apertura y cierre. |
| Enable Sell / Enable Buy | Sell = true, Buy = false | Selecciona qué direcciones están permitidas. |
| Volume | 0.1 | Volumen de la orden enviada con nuevas entradas. Al revertir, se agrega volumen extra para aplanar la exposición opuesta. |
| Stop Loss | 0 pips | Distancia de stop-loss inicial. Un valor de cero deshabilita el stop estático y deja el control de salida al trailing o a la ventana de cierre. |
| Take Profit | 0 pips | Distancia de take-profit inicial. Un valor de cero deshabilita el objetivo de beneficio. |

## Detalles de implementación
- Los valores en pips se recalculan desde `Security.PriceStep`. Para símbolos cotizados con tres o cinco decimales, el paso se multiplica por diez para reproducir la conversión de "pip" original de MT5.
- Tanto el trailing como los niveles de riesgo estáticos operan sobre los extremos de la vela (`HighPrice`/`LowPrice`) para aproximar el comportamiento tick a tick trabajando en la API de alto nivel basada en velas.
- La estrategia reinicia el estado interno después de cada salida para evitar reutilizar stops u objetivos desactualizados en la siguiente operación.
- Dado que StockSharp trabaja con posiciones netas por defecto, las posiciones largas y cortas simultáneas no están admitidas. La lógica de reversión imita la cobertura de MT5 compensando la exposición existente antes de abrir el lado solicitado.

## Notas de uso
- Elige un tipo de vela que coincida con la granularidad temporal requerida por la ventana de trading. Un marco temporal más corto (p. ej., 1 minuto) proporciona una sincronización más precisa.
- Las ventanas de cierre y apertura comparten el mismo parámetro de duración. Para deshabilitar cualquiera de las ventanas, establece la duración en cero o desactiva *Use Close Window*.
- Los trailing stops se activan sólo cuando el mercado ha avanzado al menos `Trailing Stop + Trailing Step` pips desde el precio de entrada registrado, reproduciendo el comportamiento original del paso de trailing.
