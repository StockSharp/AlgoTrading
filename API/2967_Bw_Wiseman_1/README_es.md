# Estrategia de Ruptura Bw WiseMan-1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del asesor experto MetaTrader **Exp_BW-wiseMan-1**. Automatiza la lógica de ruptura WiseMan-1 de Bill Williams construida alrededor del indicador Alligator. Las señales se producen cada vez que una vela completada escapa de las mandíbulas del Alligator y simultáneamente rompe los extremos de oscilación más recientes. El modo contrario opcional intercambia las señales para que la estrategia pueda desvanecerse de las mismas rupturas.

## Idea principal
- Calcular el Alligator de Bill Williams usando medias móviles suavizadas del precio mediano (alto + bajo) / 2.
- Desplazar las líneas de mandíbula, dientes y labios hacia adelante por desplazamientos configurables para coincidir con la visualización del indicador original.
- Confirmar una ruptura solo cuando la vela actual se expande más allá de los máximos o mínimos de los últimos *N* barras, asegurando que el movimiento sea más fuerte que el ruido reciente.
- Retrasar la ejecución por el número seleccionado de velas completadas para que el trader pueda operar en señales más antiguas si lo desea.

## Reglas de trading
### Dirección larga
1. La barra debe terminar **por debajo** de las tres líneas del Alligator (precio alto menor que mandíbula, dientes y labios).
2. El precio de cierre necesita estar en la mitad superior de la vela, es decir, por encima de la mediana de la vela.
3. El mínimo de la vela debe ser estrictamente menor que los mínimos de las barras `Back` anteriores.
4. Cuando la señal se activa después del retraso `SignalBar`:
   - Cerrar cualquier corto abierto si `Close Short` está habilitado.
   - Abrir una nueva posición larga si `Enable Long` está habilitado y no hay ninguna posición actualmente abierta.

### Dirección corta
1. La barra debe terminar **por encima** de las tres líneas del Alligator (precio bajo mayor que mandíbula, dientes y labios).
2. El precio de cierre debe estar en la mitad inferior de la vela, es decir, por debajo de la mediana de la vela.
3. El máximo de la vela tiene que ser mayor que los máximos de las barras `Back` anteriores.
4. Cuando la señal se activa:
   - Cerrar cualquier largo existente si `Close Long` está habilitado.
   - Abrir una nueva posición corta si `Enable Short` está habilitado y no hay posición actual.

### Modo contrario
Establecer `Counter-Trend Mode` en **true** intercambia las señales de compra y venta para que la estrategia tome operaciones contra la dirección de ruptura del Alligator.

## Parámetros
- **Candle Type** – marco temporal usado para construir velas y calcular todos los valores del indicador (predeterminado: 1 hora).
- **Counter-Trend Mode** – invertir la lógica de ruptura para operar contra la tendencia primaria (predeterminado: habilitado, siguiendo el EA original).
- **Breakout Depth (`Back`)** – número de barras anteriores comparadas con el máximo/mínimo actual al validar una ruptura (predeterminado: 2).
- **Jaw Length / Shift** – longitud de la media móvil suavizada y desplazamiento hacia adelante para la línea de mandíbula (predeterminados: 13 / 8).
- **Teeth Length / Shift** – longitud de la media móvil suavizada y desplazamiento hacia adelante para la línea de dientes (predeterminados: 8 / 5).
- **Lips Length / Shift** – longitud de la media móvil suavizada y desplazamiento hacia adelante para la línea de labios (predeterminados: 5 / 3).
- **Signal Bar** – número de velas ya terminadas para esperar antes de ejecutar una señal detectada (predeterminado: 1).
- **Enable Long / Enable Short** – interruptores para abrir nuevas posiciones largas o cortas.
- **Close Long / Close Short** – interruptores para cerrar posiciones opuestas cuando se activa la señal.

## Notas
- La estrategia se basa únicamente en órdenes de mercado y no establece niveles duros de stop-loss o take-profit. Cualquier salida es impulsada por la señal opuesta o deshabilitando el interruptor de cierre relevante.
- Todos los cálculos se realizan en velas terminadas; los datos parciales intrabar se ignoran para mantener la consistencia con el experto MetaTrader fuente.
- El volumen se hereda de la configuración de estrategia de StockSharp. Ajuste el volumen base en la configuración de la plataforma si necesita un tamaño de posición diferente.
