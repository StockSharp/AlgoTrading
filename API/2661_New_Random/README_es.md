# Estrategia Nueva Aleatoria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Nueva Aleatoria** emula el experto original de MetaTrader "New Random" ofreciendo tres modos distintos de selección de entrada. Abre solo una posición a la vez y espera hasta que la posición actual esté cerrada antes de generar la siguiente dirección de orden. Las entradas a mercado se activan en actualizaciones del mejor precio (datos Level 1) usando los mejores precios bid/ask como anclas de ejecución. La estrategia calcula automáticamente los offsets de stop-loss y take-profit en pips, adaptándose a cotizaciones forex de 3 y 5 dígitos de la misma manera que la versión MQL.

## Modos de entrada
1. **Generador** – la siguiente dirección es elegida por un generador pseudoaleatorio sembrado al inicio de la estrategia. Cada oportunidad es un lanzamiento de moneda independiente entre comprar y vender.
2. **Compra-Venta-Compra** – las posiciones alternan estrictamente entre compra y venta. La primera orden es una compra, seguida de una venta, y así sucesivamente.
3. **Venta-Compra-Venta** – las posiciones alternan estrictamente comenzando desde una venta, seguida de una compra, y repitiendo.

## Parámetros
- **Random Mode** (`Mode`) – selecciona uno de los tres mecanismos de entrada descritos anteriormente. Por defecto el generador aleatorio.
- **Minimal Lot Count** (`MinimalLotCount`) – multiplica el volumen mínimo negociable del instrumento. Un valor de `1` significa que la estrategia opera exactamente `Security.VolumeMin`, mientras que valores más altos escalan el tamaño de la orden por múltiplos enteros.
- **Stop Loss (pips)** (`StopLossPips`) – distancia en pips por debajo/encima del precio de llenado donde la estrategia saldrá de la posición. Establecer en `0` para deshabilitar el stop-loss.
- **Take Profit (pips)** (`TakeProfitPips`) – distancia en pips donde la estrategia realizará ganancias. Establecer en `0` para deshabilitar el take-profit.

## Lógica de trading
1. Se suscribe a datos Level 1 para el instrumento configurado y almacena constantemente los últimos precios bid, ask y último trade.
2. Cuando no hay posición abierta ni orden pendiente, la estrategia evalúa el modo seleccionado para determinar la siguiente dirección.
3. Las órdenes se colocan a mercado usando la última instantánea de mejor bid/ask. Los objetivos de stop-loss y take-profit se calculan inmediatamente desde el precio de entrada usando los parámetros de distancia en pips.
4. Solo puede existir una posición a la vez. Las entradas posteriores se suprimen hasta que la posición activa esté completamente cerrada.

## Gestión de posición
- Las posiciones largas salen anticipadamente cuando el precio actual cae al stop-loss o por debajo, o sube al take-profit o por encima.
- Las posiciones cortas salen cuando el precio actual sube al stop-loss o por encima, o cae al take-profit o por debajo.
- Las comparaciones de precio siempre usan la información Level 1 más fresca: el último precio de trade si está disponible, de lo contrario el mejor bid/ask para el lado respectivo.
- Después de cerrar un trade, la estrategia reinicia el estado interno, alterna opcionalmente la siguiente dirección (para modos secuencia), y espera la próxima actualización de cotización antes de volver a entrar.

## Notas
- La estrategia nunca piramidaliza posiciones y mantiene el comportamiento determinista para los modos basados en secuencia.
- El modo aleatorio se siembra con el conteo de ticks actual por lo que cada ejecución produce un flujo de órdenes único.
- Todos los comentarios internos y logs están en inglés para alinearse con las pautas del repositorio.
