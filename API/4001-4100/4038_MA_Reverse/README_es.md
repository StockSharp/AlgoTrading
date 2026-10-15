# Estrategia inversa MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia inversa MA es una StockSharp conversión del simple MetaTrader 4 asesor experto "MA_Reverse". El robot original
monitorea cuánto tiempo el precio de oferta permanece por encima o por debajo de un promedio móvil simple de 14 períodos (SMA). Después de una racha bastante larga en uno
dirección, abre una posición apostando a una reversión de corto plazo. El puerto StockSharp mantiene la misma idea al contar el número
de velas terminadas consecutivas que cierran por encima o por debajo del SMA y ejecutan una orden de mercado tan pronto como se alcanza el umbral configurado
alcanzado.

## Lógica comercial
- Suscríbase a velas del período de tiempo seleccionado y calcule una media móvil simple con el período definido por `SmaPeriod`.
- Mantener un contador de números enteros (`StreakThreshold` controla la longitud objetivo) que se incrementa mientras el cierre de la vela permanece por encima
la media móvil y disminuye mientras el cierre se mantiene por debajo de ella. Al tocar la media móvil se reinicia el contador.
- Una vez que el contador llega a `StreakThreshold` y el cierre está al menos `MinimumDeviation` por encima del SMA, la estrategia vende con un
orden del mercado. Se supone que es probable que una excursión alcista prolongada desde la media móvil se revierta.
- Cuando el contador llega a `-StreakThreshold` y el cierre es al menos `MinimumDeviation` por debajo del SMA, la lógica refleja la
comportamiento y abre una posición larga.
- Después de cada operación, el contador mantiene su valor actual, al igual que la fuente EA, de modo que pueda comenzar a medir inmediatamente el
siguiente racha.

## Gestión de pedidos
- Las entradas al mercado utilizan el parámetro `TradeVolume`. Si hay una posición opuesta en el libro, la estrategia primero la cierra y
luego abre la nueva operación en una orden de mercado única para que las reversiones coincidan con el comportamiento MetaTrader.
- Una toma de ganancias global se configura a través del asistente `StartProtection` de StockSharp. La distancia es igual a `TakeProfitPoints`
multiplicado por el paso del precio del valor, reproduciendo el objetivo de ganancia de "30 * Puntos" del código MQL. Cuando se alcanza el objetivo,
La posición se cierra con una orden de mercado.
- No se implementa ningún stop-loss en el experto original y, por lo tanto, no se agrega ninguno en el puerto. El control de riesgos está íntegramente a cargo de
la toma de ganancias y por la configuración de administración del dinero del usuario.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño de lote utilizado para cada entrada al mercado. El valor también se utiliza para dimensionar las inversiones al cambiar de dirección. |
| `SmaPeriod` | Número de velas utilizadas por la media móvil simple. El valor predeterminado coincide con la media móvil de 14 períodos del EA. |
| `StreakThreshold` | Número de cierres consecutivos que deben permanecer en un lado del SMA antes de que se permita una orden de reversión. |
| `MinimumDeviation` | Distancia mínima absoluta entre el cierre y el SMA que confirma la condición de ruptura. |
| `TakeProfitPoints` | Distancia de obtención de beneficios expresada en incrementos de precio. Se multiplica por el `PriceStep` del instrumento para obtener la compensación de precio absoluta. |
| `CandleType` | Tipo de vela (período de tiempo) utilizado para calcular el SMA y evaluar los contadores de racha. |

## Notas
- La lógica contraria funciona con velas terminadas proporcionadas por `SubscribeCandles`, lo que hace que la implementación sea sólida y
compatible con las pruebas históricas. El comportamiento coincide con la versión MetaTrader basada en ticks siempre que las velas estén bien
lo suficientemente granulado como para capturar excursiones de corta duración.
- Debido a que StockSharp agrega posiciones de forma predeterminada, varias entradas consecutivas se administran como una única posición con un único
distancia flotante de toma de ganancias. Esto equivale a que MetaTrader realice la misma toma de ganancias en cada orden porque el
La distancia con respecto al precio medio de entrada actual se mantiene constante.
- La estrategia no agrega su propio indicador a `Strategy.Indicators` porque la infraestructura vinculante administra el indicador
vida útil automáticamente.
- Valide siempre la configuración de paso de precio y volumen para los símbolos de su corredor específicos para que el parámetro `TakeProfitPoints`
produce el tamaño objetivo absoluto deseado.
