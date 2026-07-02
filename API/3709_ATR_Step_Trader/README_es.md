# ATR Estrategia de comerciante por pasos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia ATR Step Trader es una adaptación directa del asesor experto MetaTrader5 `atrTrader.mq5`. Combina un filtro de promedio móvil rápido/lento con reglas de desglose y pirámide basadas en el rango verdadero promedio (ATR). El puerto mantiene el flujo de trabajo basado en barras del EA original: solo se procesan velas completadas, el SMA rápido debe ubicarse por encima o debajo del SMA lento para un número fijo de barras, y cada decisión está anclada a ATR múltiplos para normalizar las distancias entre los mercados.

## Indicadores y datos
- **Promedios móviles simples (SMA).** Dos promedios móviles (`FastPeriod` y `SlowPeriod`) definen el filtro de tendencia principal. Ambos se aplican a la serie de velas de suscripción.
- **Rango verdadero promedio (ATR).** Un indicador `AverageTrueRange` (`AtrPeriod`) convierte la volatilidad en distancias de precios. Cada cálculo de ruptura, complemento y parada utiliza múltiplos ATR.
- **Canales de precios más altos/más bajos.** Los indicadores `Highest` y `Lowest` rastrean los máximos y mínimos extremos de las velas `MomentumPeriod` más recientes. Reemplazan las llamadas `iHighest`/`iLowest` del código MQL.
- **Período de tiempo.** El tipo de vela predeterminado es una hora (`TimeSpan.FromHours(1)`), lo que refleja el comportamiento `PERIOD_CURRENT` del script original. Puede cambiar a cualquier otro período editando el parámetro `CandleType`.

## Lógica de entrada
1. Espera a que se acabe la vela. Las velas sin terminar se ignoran para permanecer sincronizadas con el protector MT5 OnTick + iTime.
2. Actualiza los contadores de racha de media móvil. Una racha alcista aumenta cuando el SMA rápido se imprime por encima del SMA lento; una racha bajista aumenta cuando se imprime debajo. Las lecturas mixtas restablecieron la racha opuesta.
3. Una vez que la racha alcista alcance `MomentumPeriod`, verifique si el precio de cierre todavía está por debajo del máximo reciente en al menos `StepMultiplier * ATR`. Si es así, compre en el mercado.
4. Una vez que la racha bajista alcance `MomentumPeriod`, compruebe si el precio de cierre todavía está por encima del mínimo reciente en al menos `StepMultiplier * ATR`. Si es así, véndalo en el mercado.
5. Cada nueva posición inicializa el estado direccional: la estrategia recuerda los precios completados más alto y más bajo por lado para que las pirámides posteriores tengan anclajes de referencia. La primera orden también incluye un stop del tamaño de la volatilidad (`StepMultiplier * StopMultiplier * ATR`).

## Gestión de posiciones
- **Pirámide:** Mientras el número de entradas activas esté por debajo de `PyramidLimit`, la estrategia agrega otra unidad cada vez que el precio se aleja `+/- StepsMultiplier * ATR` de la referencia extrema actual. Esto refleja la cuadrícula de escala de "Pasos" del EA y funciona tanto en direcciones favorables como desfavorables.
- **Paradas de protección:** La parada inicial para una nueva orden se encuentra a `StepMultiplier * StopMultiplier * ATR` del precio de cumplimiento. Cuando la pirámide está llena, las paradas se ajustan a `StepMultiplier * ATR` detrás (para largos) o delante (para cortos) del último cierre, emulando la actualización final del EA cuando hay tres posiciones abiertas.
- **Salidas adversas:** si el precio retrocede `StepsMultiplier * ATR` más allá del extremo rastreado, la estrategia sale inmediatamente de todas las posiciones de ese lado con una orden de mercado. Esto captura la lógica EA que descarta la pila completa cuando el precio supera el borde de la escalera más reciente.
- **Restablecimiento de estado:** Después de una salida completa, los contadores de racha y las referencias de parada ATR se reinician de modo que se debe desarrollar una nueva secuencia de tendencia antes del reingreso.

## Parámetros
| grupo | Nombre | Descripción | Predeterminado |
| --- | --- | --- | --- |
| Filtro de tendencias | `FastPeriod` | Longitud rápida SMA que mide la dirección a corto plazo. | `70` |
| Filtro de tendencias | `SlowPeriod` | Longitud lenta de SMA que mide la dirección a largo plazo. | `180` |
| Filtro de tendencias | `MomentumPeriod` | Número de velas terminadas consecutivas que deben confirmar la tendencia. | `50` |
| volatilidad | `AtrPeriod` | Ventana ATR utilizada para todos los cálculos de distancia. | `100` |
| Lógica de entrada | `StepMultiplier` | ATR múltiplo que bloquea las rupturas iniciales. | `4` |
| Lógica de entrada | `StepsMultiplier` | ATR múltiplo que separa las capas de la pirámide. | `2` |
| Gestión de riesgos | `StopMultiplier` | Multiplicador adicional aplicado a la parada inicial más allá de la distancia del paso base. | `3` |
| Tamaño de posición | `PyramidLimit` | Número máximo de entradas por sentido. | `3` |
| Comercio | `TradeVolume` | Volumen de estrategia presentado con cada orden de mercado. | `1` |
| generales | `CandleType` | Tipo de vela (período de tiempo) utilizado para la suscripción. | `TimeFrame(1h)` |

## Notas practicas
- La versión StockSharp utiliza la propiedad de estrategia `Volume` para el tamaño. Ajuste `TradeVolume` para que coincida con el tamaño del contrato de su instrumento antes de publicarlo.
- Se supone que las órdenes de mercado se ejecutan inmediatamente, al igual que el uso de `CTrade.Buy`/`Sell` por parte de MT5. En mercados reducidos es posible que desee sustituir las órdenes de mercado por órdenes de límite o de parada.
- Las referencias alta/baja replican las variables `h_price` y `l_price` de EA y se actualizan cada vez que se agrega o elimina una nueva capa. Son esenciales para determinar cuándo agregar o lavar la escalera.
- Debido a que EA almacena stop loss por posición mientras que StockSharp los administra a nivel de estrategia, el puerto aplica la lógica de stop más estricta a toda la pila. Esto ofrece el mismo comportamiento (todas las posiciones salen juntas) con menos órdenes del corredor que administrar.
- Pruebe siempre la estrategia en simulación. Las distancias ATR se adaptan a la volatilidad, pero en mercados con brechas o deslizamiento alto el riesgo realizado aún puede exceder la distancia de parada proyectada.
