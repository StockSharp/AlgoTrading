# TwentyPipsOnceADayEstrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puerto del experto MetaTrader **20pipsOnceADayOppositeLastNHourTrend** implementado con la API de alto nivel de StockSharp. La estrategia se negocia una vez por hora configurada y abre una posición contraria contra la deriva de las últimas `N` velas horarias. El tamaño de la posición sigue una escalera de martingala que aumenta el lote sólo cuando una operación reciente terminó con una pérdida. La implementación también impone un cronograma de negociación diario, protección de seguimiento opcional y un período de tenencia máximo.

## Lógica de trading

1. La estrategia se suscribe a velas horarias (configurables a través de `CandleType`).
2. Cuando una vela se cierra y la siguiente hora coincide con `TradingHour`, la estrategia evalúa la dirección:
   - Compare el precio de cierre de la última hora completa con el precio de cierre de hace `HoursToCheckTrend` horas.
   - Si el mercado cayó durante ese intervalo, abra una posición larga (desvanezca la tendencia bajista).
   - Si el mercado subió, abra una posición corta.
3. Sólo una posición puede estar activa a la vez (controlada por `MaxOrders`).
4. Cada operación hereda una toma de ganancias fija y un stop-loss/trailing stop opcional, ambos expresados en pips en relación con el tamaño del pip del instrumento.
5. Si la posición permanece abierta por más de `OrderMaxAgeSeconds` o la siguiente hora está fuera de la sesión permitida definida por `TradingDayHours`, la estrategia cierra la operación a la fuerza.

## Gestión monetaria

- `FixedVolume` define el lote base. Configúrelo en `0` para derivar el lote del valor de la cartera usando `RiskPercent`. El tamaño basado en el riesgo refleja la lógica EA original: `(portfolio value * RiskPercent) / 1000`.
- Una vez calculado el lote base, se sujeta tanto por los límites `VolumeMin/VolumeMax/VolumeStep` del instrumento como por los límites `MinVolume`/`MaxVolume` definidos por el usuario.
- Una escalera martingala aumenta el siguiente lote sólo si la respectiva operación histórica cerró con pérdidas:
  - `FirstMultiplier` se aplica cuando se perdió la operación más reciente.
  - `SecondMultiplier` se aplica cuando la última operación ganó pero la anterior perdió.
  - La cadena continúa hasta `FifthMultiplier`, coincidiendo con la escalada original de cinco pasos.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `FixedVolume` | Volumen de negociación fijo. Utilice `0` para habilitar el dimensionamiento basado en el riesgo. |
| `MinVolume` / `MaxVolume` | Límites superior e inferior aplicados después del dimensionamiento. |
| `RiskPercent` | Porcentaje de cartera convertido en volumen cuando `FixedVolume` es igual a cero. |
| `MaxOrders` | Número máximo de posiciones abiertas simultáneamente (por defecto `1`). |
| `TradingHour` | Hora del día (0-23) en la que pueden comenzar nuevas operaciones. |
| `TradingDayHours` | Horas o rangos separados por comas (por ejemplo, `0-7,13-22`) que siguen siendo elegibles para puestos vacantes. Cuando la siguiente hora está fuera de este conjunto, la estrategia sale. |
| `HoursToCheckTrend` | Mirada retrospectiva en velas horarias utilizadas para la comparación contraria. |
| `OrderMaxAgeSeconds` | Tiempo máximo de espera en segundos antes de forzar una salida. |
| `FirstMultiplier` … `FifthMultiplier` | Martingale multiplicadores asignados a las pérdidas encontradas en las últimas cinco operaciones cerradas. |
| `StopLossPips` | Distancia inicial de stop loss en pips. Establezca en `0` para desactivar. |
| `TrailingStopPips` | Distancia del trailing stop en pips. Establezca en `0` para desactivar. |
| `TakeProfitPips` | Tomar distancia de ganancias en pips. |
| `CandleType` | Tipo de vela utilizado para la generación de señales (el valor predeterminado es un período de tiempo de 1 hora). |

## Controles de Riesgos y Salidas

- **Take Profit / Stop Loss**: Configurado a través de `TakeProfitPips` y `StopLossPips` con conversión automática a unidades de precio de instrumento.
- **Parada dinámica**: si está habilitado, la parada se rastrea una vez que la operación gana más que el número de pips configurado.
- **Salida de tiempo de espera**: Las posiciones anteriores a `OrderMaxAgeSeconds` se cierran al precio de cierre de vela actual.
- **Filtro de sesión**: Las posiciones se cierran cuando la próxima hora no está incluida en `TradingDayHours`.

## Notas de uso

- La estrategia funciona con cualquier instrumento que proporcione velas horarias y un `PriceStep` válido. Cuando el instrumento utiliza pips fraccionarios (3 o 5 decimales), el tamaño del pip se ajusta automáticamente.
- Para replicar el comportamiento de MetaTrader, ejecute la estrategia en un solo instrumento con `CandleType` configurado en un período de tiempo por hora y mantenga el `TradingDayHours` predeterminado (0-23) para permitir el comercio durante todo el día.
- La escalera martingala supone como máximo cinco oficios históricos relevantes. Restablecer la estrategia borra este historial.
- Debido a que la estrategia opera al inicio de la hora configurada utilizando datos de velas cerradas, los llenados se producen al precio disponible cuando comienza la nueva hora.

## Archivos

- `CS/TwentyPipsOnceADayStrategy.cs` – implementación principal de C#.
- `README.md` – Documentación en inglés (este archivo).
- `README_zh.md` – Documentación china.
- `README_ru.md` – Documentación rusa.

Los puertos de Python se omiten intencionalmente para esta conversión.
