# Estrategia de Fuerza de Divisas v1.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Fuerza de Divisas v1.1 replica el asesor experto de MetaTrader *Currency Strength v1.1*. Mide la fuerza relativa de las ocho principales divisas (USD, EUR, JPY, CAD, AUD, NZD, GBP, CHF) usando cambios porcentuales diarios de 26 pares FX líquidos. Siempre que la fuerza de dos divisas diverge más allá de un umbral configurable, la estrategia abre una posición en el par de divisas correspondiente en la dirección de la divisa más fuerte.

## Mercado y datos
- **Universo de instrumentos:** 26 pares FX mayores y cruzados (USDJPY, USDCAD, AUDUSD, USDCHF, GBPUSD, EURUSD, NZDUSD, EURJPY, EURCAD, EURGBP, EURCHF, EURAUD, EURNZD, AUDNZD, AUDCAD, AUDCHF, AUDJPY, CHFJPY, GBPCHF, GBPAUD, GBPCAD, GBPJPY, CADJPY, NZDJPY, GBPNZD, CADCHF).
- **Frecuencia de datos:** Velas diarias (D1). Solo se procesan velas completadas para mantener cálculos consistentes.
- **Campos requeridos:** Precios de apertura, máximo, mínimo y cierre de cada vela.

## Cálculo de la fuerza de divisas
El cambio porcentual diario para cada par se calcula como:

```
(change) = (Close − Open) / Open × 100
```

Estos cambios específicos de par se combinan luego en índices de fuerza de divisas:

- **Fuerza EUR** = promedio de EURJPY, EURCAD, EURGBP, EURCHF, EURAUD, EURUSD, EURNZD
- **Fuerza USD** = promedio de USDJPY, USDCAD, –AUDUSD, USDCHF, –GBPUSD, –EURUSD, –NZDUSD
- **Fuerza JPY** = promedio negativo de USDJPY, EURJPY, AUDJPY, CHFJPY, GBPJPY, CADJPY, NZDJPY
- **Fuerza CAD** = promedio de CADCHF, CADJPY, –GBPCAD, –AUDCAD, –EURCAD, –USDCAD
- **Fuerza AUD** = promedio de AUDUSD, AUDNZD, AUDCAD, AUDCHF, AUDJPY, –EURAUD, –GBPAUD
- **Fuerza NZD** = promedio de NZDUSD, NZDJPY, –EURNZD, –AUDNZD, –GBPNZD
- **Fuerza GBP** = promedio de GBPUSD, –EURGBP, GBPCHF, GBPAUD, GBPCAD, GBPJPY, GBPNZD
- **Fuerza CHF** = promedio de CHFJPY, –USDCHF, –EURCHF, –AUDCHF, –GBPCHF, –CADCHF

Cada promedio usa el mismo número de componentes que en el asesor experto original para preservar el esquema de ponderación.

## Lógica de trading
1. Después de que todos los 26 pares reportan una nueva vela diaria terminada, se recalculan las fuerzas.
2. Para cada par la estrategia compara las dos fuerzas de divisas relevantes. Si la diferencia absoluta supera el parámetro `DifferenceThreshold`, se genera una señal de operación.
3. La dirección de la señal sigue a la divisa más fuerte:
   - Si fuerza de la divisa base > fuerza de la divisa cotizada → comprar el par.
   - Si fuerza de la divisa base < fuerza de la divisa cotizada → vender el par.
4. Las operaciones solo se permiten cuando la vela diaria del par concuerda con la señal (cierre por encima de la apertura para compras, cierre por debajo para ventas), reflejando el filtro de tendencia del EA original.
5. Las posiciones netas existentes se respetan. Si aparece una señal de inversión mientras hay una posición contraria abierta, la estrategia cierra la posición actual y cambia a la nueva dirección con una sola orden de mercado.
6. Cuando `TradeOncePerDay` está habilitado, cada par puede entrar largo como máximo una vez por día de trading y entrar corto como máximo una vez por día de trading.

## Gestión de riesgo y salidas
- El indicador opcional `UseSlTp` habilita la lógica de stop-loss y take-profit ejecutada en la vela diaria de cada par. Las distancias se definen en pips (`StopLossPips`, `TakeProfitPips`).
- La lógica protectora evalúa el máximo/mínimo diario de la vela más reciente. Si esos extremos alcanzan los objetivos respectivos, la posición se cierra al precio de mercado en el siguiente paso de evaluación.
- Sin SL/TP, las posiciones permanecen abiertas hasta que una señal opuesta fuerza una inversión o la estrategia se detiene manualmente, reflejando el comportamiento del EA fuente.

## Parámetros de la estrategia
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal para velas (por defecto: diario). |
| `DifferenceThreshold` | Diferencia mínima de fuerza (en puntos porcentuales) necesaria para activar una operación. |
| `TradeOncePerDay` | Si es `true`, limita cada par a una entrada larga y una corta por día. |
| `UseSlTp` | Habilita la evaluación diaria de los niveles de stop-loss y take-profit. |
| `TakeProfitPips` | Distancia del take-profit medida en pips. |
| `StopLossPips` | Distancia del stop-loss medida en pips. |
| Parámetros de pares | Entradas individuales `Security` para los 26 pares FX. Cada uno debe asignarse antes de iniciar la estrategia. |
| `Volume` | Propiedad de la clase base que define el tamaño de la operación (por defecto 0.01 lotes). |

## Notas de implementación
- La estrategia se suscribe a cada par por separado usando la API de suscripción de velas de alto nivel (`SubscribeCandles`).
- El manejo de velas ignora estrictamente las velas incompletas, satisfaciendo las directrices de conversión de StockSharp.
- Los cálculos de fuerza y la generación de señales solo funcionan cuando todos los pares reportan la misma fecha de trading, garantizando cestas de divisas sincronizadas.
- Los diccionarios internos rastrean las últimas fechas de operación por dirección y almacenan información de entrada para salidas protectoras.

## Consejos de uso
1. Asignar todos los 26 instrumentos antes de iniciar la estrategia; las entradas faltantes lanzan una excepción para evitar cálculos parciales.
2. Asegurarse de que el proveedor de datos suministre velas diarias para cada par configurado para que las fuerzas de divisas permanezcan sincronizadas.
3. Ajustar `DifferenceThreshold` para controlar la frecuencia de señales. Umbrales más pequeños llevan a más operaciones frecuentes pero también más inversiones.
4. Calibrar los stops basados en pips según la precisión de cotización de su broker; el valor predeterminado asume precios de pips fraccionarios.
