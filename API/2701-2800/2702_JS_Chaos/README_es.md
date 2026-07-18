# Estrategia JS Chaos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia JS Chaos replica el comportamiento del asesor experto original de MetaTrader "JS-Chaos" usando la API de alto nivel de StockSharp. La estrategia construye entradas por ruptura alrededor de la estructura del Alligator de Bill Williams y los niveles de fractales, combina la confirmación del Awesome Oscillator y Acceleration/Deceleration, y gestiona la exposición abierta con stops de seguimiento, lógica de punto de equilibrio y un rico filtro de tiempo.

## Lógica principal
1. **Pila de indicadores**
   - Alligator de Bill Williams (Medias Móviles Suavizadas con períodos 13/8/5 y desplazamientos de 8/5/3 barras) muestreados en el precio medio.
   - Awesome Oscillator y una SMA de 5 períodos de AO para derivar el oscilador Acceleration/Deceleration.
   - Media móvil suavizada de 21 períodos para el motor de trailing stop.
   - Desviación estándar de 10 períodos usada como condición de seguimiento adicional.
   - Detección de fractales sobre los últimos cinco máximos/mínimos, almacenando las formaciones más recientes durante diez barras.
2. **Generación de señales**
   - El contexto alcista requiere `AO[0] > AO[1] > 0` y `Lips > Teeth > Jaw`.
   - El contexto bajista requiere `AO[0] < AO[1] < 0` y `Lips < Teeth < Jaw`.
3. **Colocación de órdenes**
   - Cuando las condiciones se alinean y la hora actual es operable, la estrategia pone en cola dos entradas de tipo stop por dirección: una orden primaria (2× volumen base) y una orden secundaria (1× volumen base). Ambas se activan en el fractal calificador más reciente que se extiende más allá de los labios del Alligator.
   - El take-profit primario usa `Lips ± (Fractal − Lips) * Fibo1`. El take-profit secundario usa el multiplicador `Fibo2`.
4. **Gestión de operaciones**
   - Salida temprana opcional cuando los labios cruzan por encima (para largos) o por debajo (para cortos) de la apertura de la vela anterior.
   - El trailing stop lleva el nivel de protección a la SMMA de 21 períodos cuando la desviación estándar, AO y AC avanzan todos en la dirección de la operación.
   - La lógica de punto de equilibrio desplaza el stop de la operación secundaria una vez que se ha completado la operación primaria y el precio ha recorrido los pips extra configurados.
   - El monitoreo manual de los niveles de stop-loss y take-profit cierra las operaciones mediante órdenes de mercado cuando se superan los límites de precio correspondientes.
5. **Filtro de tiempo**
   - Ventana de trading definida por horas de inicio/fin (con soporte de vuelta de reloj) y filtros estacionales opcionales: deshabilitado antes del lunes 03:00, después del viernes 18:00, durante los primeros nueve días de enero y después del 20 de diciembre. Configurar `Use Time` en falso desactiva el filtro por completo.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `UseTime` | Activa el filtro de tiempo. |
| `OpenHour` / `CloseHour` | Límites de hora para el trading (0-23). |
| `BaseVolume` | Volumen de orden base, usado para dimensionar las dos entradas escalonadas (2× para la primaria, 1× para la secundaria). |
| `IndentingPips` | Offset añadido/sustraído de los niveles de fractales antes de colocar órdenes stop (expresado en pips). |
| `Fibo1` / `Fibo2` | Multiplicadores tipo Fibonacci aplicados a la distancia entre los labios y el fractal para los objetivos de take-profit. |
| `UseClosePositions` | Cierra posiciones contrarias cuando los labios cruzan la apertura de la vela anterior. |
| `UseTrailing` | Activa el trailing stop basado en MA/oscilador. |
| `UseBreakeven` | Activa la gestión de punto de equilibrio para la posición secundaria. |
| `BreakevenPlusPips` | Pips extra añadidos sobre el precio de entrada al mover el stop al punto de equilibrio. |
| `CandleType` | Marco temporal de las velas procesadas por la estrategia. |

## Notas
- La conversión mantiene la estructura de órdenes escalonadas y la lógica de gestión del robot MQL5 original mientras aprovecha el flujo de trabajo de suscripción de velas de StockSharp.
- Todos los cálculos dependen de velas finalizadas; la lógica de tick intrabarra del EA original se refleja a través de órdenes de mercado una vez que el rango de precio confirma una ruptura.
- La conversión de pips se adapta automáticamente a instrumentos cotizados con tres o cinco decimales (símbolos tipo forex).
