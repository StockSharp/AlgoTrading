# Estrategia de Stop Trailing Virtual Level1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Stop Trailing Virtual** es una conversión directa del asesor experto de MetaTrader `Virtual Trailing Stop.mq5` (MQL ID 21362). El experto original solo gestiona los stops de protección para posiciones abiertas en otro lugar. Este port en C# reproduce el mismo comportamiento sobre la API de alto nivel de StockSharp: observa las mejores cotizaciones bid/ask y cierra la posición actual cuando se cumplen las condiciones de stop-loss, take-profit o trailing stop.

A diferencia de las estrategias de entrada, esta implementación nunca abre posiciones nuevas por sí sola. Está pensada para combinarse con otras entradas automatizadas o sesiones de trading manual cuando se necesita aplicar un trailing stop "virtual" al estilo MetaTrader dentro de StockSharp.

## Lógica de trading
1. **Feed Level1** – la estrategia se suscribe a datos de nivel 1 y almacena continuamente los últimos valores de bid/ask.
2. **Conversión de pips** – las entradas del usuario se definen en *pips*. La estrategia las convierte a desplazamientos de precio multiplicando el valor por el `PriceStep` del instrumento. Para cotizaciones forex de 3 y 5 dígitos se aplica un multiplicador de 10x para que coincida con la definición de pip de MetaTrader.
3. **Verificación del stop-loss** – si el bid de una posición larga cae por debajo de `PrecioEntrada − StopLoss`, o el ask de una corta sube por encima de `PrecioEntrada + StopLoss`, la posición se cierra a mercado.
4. **Verificación del take-profit** – si el bid de una posición larga sube por encima de `PrecioEntrada + TakeProfit`, o el ask de una corta cae por debajo de `PrecioEntrada − TakeProfit`, la posición se cierra.
5. **Activación del trailing** – una vez que el precio se mueve `TrailingStart` pips a favor de la posición, se crea un nivel de trailing en `Bid − TrailingStop` (largo) o `Ask + TrailingStop` (corto).
6. **Actualización del trailing** – cada vez que el beneficio no realizado aumenta al menos `TrailingStep` pips, el nivel de trailing se desplaza en consecuencia. Establecer el paso en cero hace que el trailing siga cada tick favorable.
7. **Salida por trailing** – la posición se cierra cuando el precio toca el nivel de trailing mientras la operación sigue siendo rentable (imitando la salvaguarda `Profit()>0` del EA fuente).

No se colocan órdenes pendientes. Cada salida se ejecuta mediante órdenes de mercado para imitar la naturaleza "virtual" de la implementación MQL.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
| --- | --- | --- |
| `StopLossPips` | Distancia del stop-loss en pips. Establecer en `0` para desactivar la gestión de stop-loss duro. | `0` |
| `TakeProfitPips` | Distancia del take-profit en pips. Establecer en `0` para desactivar la gestión de take-profit. | `0` |
| `TrailingStopPips` | Distancia entre el precio actual y el nivel de trailing, medida en pips. | `5` |
| `TrailingStartPips` | Umbral de beneficio (en pips) que debe alcanzarse antes de que se active el trailing. | `5` |
| `TrailingStepPips` | Incremento mínimo en pips necesario antes de que el nivel de trailing se mueva de nuevo. Usar `0` para trailing continuo. | `1` |

Todos los parámetros admiten optimización gracias a los helpers `StrategyParam` de StockSharp.

## Notas de implementación
- La estrategia usa solo datos de nivel 1 (`DataType.Level1`) y no registra objetos de gráfico porque StockSharp gestiona la visualización de forma diferente a MetaTrader.
- Las conversiones de precio dependen de `Security.PriceStep` y `Security.Decimals`. Si el exchange no proporciona estos metadatos, el tamaño de pip de reserva es `1`.
- La protección es simétrica para posiciones largas y cortas. Los valores de trailing se almacenan por separado para ambas direcciones.
- La inicialización automática de posiciones que existía en modo tester dentro del EA original se ha omitido intencionalmente porque las estrategias de StockSharp operan sobre posiciones netas.

## Consejos de uso
- Adjunte la estrategia a un par cartera/instrumento que ya tenga posiciones abiertas o que se espere que las reciba de otro componente.
- Combínela con trading discrecional o estrategias de entrada automatizadas para emular la gestión de operaciones al estilo MetaTrader en StockSharp Designer, Shell o Runner.
- Al operar con instrumentos no forex, ajuste las entradas basadas en pips para que coincidan con el tamaño de tick del instrumento. Establecer `TrailingStopPips = 1` efectivamente hace trailing de un `PriceStep`.

## Archivos
- `CS/VirtualTrailingStopLevel1Strategy.cs` – implementación de la estrategia.
- `README.md`, `README_zh.md`, `README_ru.md` – documentación multilingüe de la estrategia.
