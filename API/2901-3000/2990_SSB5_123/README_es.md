# Estrategia de Múltiples Indicadores SSB5_123
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de StockSharp del asesor experto de MetaTrader 5 "ssb5_123". El código original proviene de la colección SSB (Step by Step) de Yury V. Reshetov y combina varios osciladores clásicos para confirmar rupturas direccionales. La versión de StockSharp mantiene la misma lógica utilizando la API de suscripción de velas de alto nivel e implementaciones de indicadores nativos.

El algoritmo trabaja exclusivamente con velas completadas. Compara el precio de apertura de la barra actual con la barra anterior, verifica el impulso y la dirección del Awesome Oscillator, MACD e histograma OsMA, y confirma que el precio opera por encima o por debajo de una media móvil suavizada. La confirmación adicional se obtiene del oscilador estocástico requiriendo que tanto %K como %D estén por encima o por debajo del nivel 50.

## Indicadores y Señales
Los siguientes indicadores se emplean exactamente como en la versión de MetaTrader:

- **Media Móvil Suavizada (SMMA)**: media móvil suavizada de 45 períodos calculada desde las aperturas de las velas. La dirección de entrada requiere que el precio de apertura esté en el lado correcto de la media.
- **MACD (rápido 47, lento 95, señal 74)**: la línea principal debe ser positiva para operaciones largas (negativa para operaciones cortas) y debe estar subiendo (bajando) comparada con la vela anterior.
- **Histograma OsMA**: calculado como MACD menos su línea de señal. El histograma debe decrecer para operaciones largas y aumentar para operaciones cortas, reflejando la función original `fosma1()`.
- **Awesome Oscillator**: usa las medias móviles suavizadas 5/34 predeterminadas del precio mediano. El valor del oscilador debe ser positivo para largos (negativo para cortos) y su impulso entre las últimas dos barras debe apuntar en la dirección de la operación.
- **Oscilador Estocástico (K=25, D=12, Slowing=56)**: tanto las líneas %K como %D deben estar por encima de 50 para operaciones largas y por debajo de 50 para operaciones cortas, proporcionando un filtro de régimen.

## Lógica de Trading
1. Esperar una nueva vela completada.
2. Evaluar la **configuración larga**. Todas las siguientes condiciones deben ser verdaderas:
   - La apertura de la vela actual es menor o igual a la apertura de la vela anterior.
   - El Awesome Oscillator es positivo y cae respecto al valor anterior.
   - La línea principal MACD es positiva y sube respecto al valor anterior.
   - El histograma OsMA no está aumentando (histograma actual menos histograma anterior es menor o igual a cero).
   - La apertura de la vela actual está por encima de la media móvil suavizada.
   - Las líneas estocásticas %K y %D están en o por encima de 50.
3. Evaluar la **configuración corta**. Todas las siguientes condiciones deben ser verdaderas:
   - La apertura de la vela actual es mayor o igual a la apertura de la vela anterior.
   - El Awesome Oscillator es negativo y sube respecto al valor anterior.
   - La línea principal MACD es negativa y cae respecto al valor anterior.
   - El histograma OsMA no está decreciendo (histograma actual menos histograma anterior es mayor o igual a cero).
   - La apertura de la vela actual está por debajo de la media móvil suavizada.
   - Las líneas estocásticas %K y %D están en o por debajo de 50.
4. Si ya existe una posición, una señal opuesta la cierra inmediatamente, replicando la gestión de órdenes original de MetaTrader.
5. Cuando está plano, una entrada larga tiene prioridad: si ambas señales resultan ser verdaderas (posible cuando todos los indicadores son exactamente cero), la estrategia abre una posición larga. De lo contrario, abre una posición corta cuando solo se satisfacen las condiciones cortas.

## Parámetros
- **SMMA Period** – longitud del filtro de media móvil suavizada (predeterminado 45).
- **MACD Fast / Slow / Signal** – períodos EMA para el indicador MACD (47 / 95 / 74).
- **Stochastic %K / %D / Slowing** – período principal, período de suavizado y suavizado adicional para el oscilador estocástico (25 / 12 / 56).
- **Order Volume** – cantidad utilizada para órdenes de mercado (predeterminado 1).
- **Candle Type** – marco temporal de las velas de entrada (predeterminado 1 hora). Ajuste esto para que coincida con el marco temporal utilizado en MetaTrader.

## Notas de Uso
- La estrategia opera solo en velas terminadas; las actualizaciones intrabarra son ignoradas.
- Los valores de indicadores de la vela anterior se almacenan en caché para que las comparaciones de impulso coincidan con el comportamiento exacto de las funciones auxiliares originales `fao1`, `fmacd1` y `fosma1`.
- No hay reglas de stop-loss o take-profit integradas en el asesor experto original. La gestión de riesgos debe añadirse externamente si es necesario.
- Los ajustes de indicadores predeterminados coinciden con los parámetros MQL proporcionados, pero todos los valores se exponen como objetos `StrategyParam` y pueden optimizarse a través del optimizador de StockSharp.

## Consideraciones de Conversión
- La versión de MetaTrader usa un número mágico y validación manual de volumen; estas partes no son necesarias en StockSharp y fueron omitidas.
- La lógica de cierre de órdenes sigue la misma precedencia que el script MQL: las posiciones se cierran primero, y las nuevas entradas solo se toman cuando la estrategia está plana.
- Las implementaciones de Awesome Oscillator y MACD provienen de la biblioteca de indicadores de StockSharp, eliminando la necesidad de manejo manual de búferes presente en el código original.
