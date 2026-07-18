# Estrategia ACB1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia ACB1** es el puerto StockSharp del asesor experto MetaTrader distribuido como `MQL/8586/ACB1.MQ4`. El sistema original opera con el par EURUSD y espera fuertes rupturas diarias antes de ingresar al mercado. Esta conversión reproduce el mismo proceso de decisión con StockSharp primitivas de alto nivel:

- Las velas diarias (`SignalCandleType`) definen la dirección de ruptura y proporcionan los anclajes de parada y toma de ganancias.
- Las velas H4 (`TrailCandleType`) determinan la distancia de seguimiento que se multiplica por `TrailFactor`.
- Las órdenes se ejecutan en el mercado una vez que se cumplen las condiciones de ruptura y la estrategia mantiene solo una posición neta, reflejando las comprobaciones `OrdersTotal()` en el código MQL.
- El stop-loss y el take-profit se gestionan internamente: la estrategia vigila los mejores precios de oferta y demanda y cierra la posición con órdenes de mercado cuando se superan los niveles de protección virtual.

## Reglas comerciales

1. **Configuración larga**
   - Utilice la vela diaria terminada anteriormente.
   - Si `Close > (High + Low) / 2` *y* el precio de venta actual está por encima del máximo anterior, abra una posición larga en el mercado.
   - El stop-loss se coloca en el mínimo anterior (redondeado al nivel del precio del instrumento).
   - La toma de ganancias es igual al precio de entrada más `(High − Low) × TakeFactor`.

2. **Configuración corta**
   - Si `Close < (High + Low) / 2` *y* el precio de oferta actual está por debajo del mínimo anterior, abra una posición corta en el mercado.
   - El stop-loss se fija en el máximo anterior; la toma de ganancias resta `(High − Low) × TakeFactor` del precio de entrada.

3. **Parada de seguimiento**
   - Las `TrailCandleType` velas terminadas más recientes suministran `(High − Low) × TrailFactor`.
   - Para posiciones largas, el stop sigue a `Bid − TrailDistance` mientras el precio permanece por debajo del nivel de obtención de beneficios menos el stop del corredor.
   - Para posiciones cortas, el stop sigue `Ask + TrailDistance` mientras que el precio se mantiene por encima del nivel de toma de ganancias más el stop del corredor.

4. **Guardia de riesgos**
   - La estrategia rastrea el capital máximo observado de la cartera. La negociación se detiene cada vez que el capital actual cae por debajo del 50% de ese pico, exactamente como en el asesor original.
   - Un tiempo de reutilización de cinco segundos (`CooldownSeconds`) evita nuevos pedidos o detiene las actualizaciones con demasiada frecuencia, reproduciendo el acelerador `TimeLocal()` de MQL.

## Dimensionamiento de posiciones y control de riesgos

- El volumen por operación se deriva de `Portfolio.CurrentValue × RiskFraction`.
- El riesgo monetario por contrato se calcula a partir de la distancia de parada y los metadatos de seguridad (`PriceStep` y `StepPrice`).
- El tamaño resultante se alinea con `Security.VolumeStep` y se fija en `[Security.MinVolume, Security.MaxVolume]`, luego se limita por el parámetro `MaxVolume` (5 lotes predeterminados).
- Las órdenes se omiten cuando el volumen normalizado es cero o cuando la distancia de parada viola `MinStopDistancePoints`, lo que emula la verificación MetaTrader `MODE_STOPLEVEL`.

## Parámetros

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `SignalCandleType` | Diariamente | Tipo de vela utilizado para la detección de fugas. |
| `TrailCandleType` | 4 horas | Tipo de vela que suministra la distancia del trailing stop. |
| `TakeFactor` | 0,8 | Multiplicador aplicado al rango diario para calcular la obtención de beneficios. |
| `TrailFactor` | 10 | Multiplicador aplicado al rango final al actualizar la parada. |
| `RiskFraction` | 0,05 | Fracción del capital de la cartera arriesgada en cada operación (5%). |
| `MaxVolume` | 5 | Límite estricto para el volumen final del pedido. |
| `MinStopDistancePoints` | 0 | Distancia mínima de parada/toma expresada en puntos de precio; configúrelo en el corredor `MODE_STOPLEVEL`. |
| `CooldownSeconds` | 5 | Retraso mínimo entre acciones comerciales consecutivas. |

## Notas de implementación

- La estrategia requiere metadatos de instrumentos adecuados: `Security.PriceStep`, `Security.StepPrice`, `Security.VolumeStep`, `Security.MinVolume` y (si está disponible) `Security.MaxVolume`.
- Los niveles de protección son virtuales. StockSharp cierra posiciones mediante órdenes de mercado cuando la oferta/demanda toca el límite de pérdidas o la toma de ganancias calculados.
- El seguimiento de acciones utiliza `Portfolio.CurrentValue`. Si el conector no proporciona este campo, la protección contra riesgos mantendrá las operaciones desactivadas hasta que esté disponible.
- Sólo se mantiene una única posición neta. Las señales opuestas mientras una operación está activa se ignoran hasta que la posición se cierra por completo.
- No se incluye ningún puerto Python; este directorio solo contiene la implementación y documentación de C#.
