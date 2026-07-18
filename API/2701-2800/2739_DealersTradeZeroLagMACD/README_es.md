# Estrategia DealersTradeZeroLag MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia porta el asesor experto de MetaTrader "Dealers Trade v 7.91 ZeroLag MACD" a la API de alto nivel de StockSharp. Rastrea la pendiente de un MACD de cero retraso para decidir si el mercado está en una fase de acumulación para largos o cortos y construye una cuadrícula de posiciones con espaciado adaptativo y gestión de riesgos. El marco temporal predeterminado son velas de cuatro horas según las recomendaciones del autor original, pero se puede seleccionar cualquier tipo de vela soportado por StockSharp.

## Lógica de trading
- **Detección de señal.** Dos medias móviles exponenciales de cero retraso (rápida y lenta) generan una línea MACD. Cuando el MACD sube en comparación con la barra anterior, la estrategia trata el mercado como alcista; cuando baja, lo trata como bajista. La señal puede invertirse mediante el parámetro `ReverseCondition`.
- **Cuadrícula de posiciones.** El algoritmo escala en la dirección detectada. Las distancias entre entradas se miden en pips y se multiplican después de cada llenado por `IntervalCoefficient`. El tamaño del lote se multiplica por `LotMultiplier` en cada entrada adicional, imitando el esquema martingala de la versión MQL.
- **Control de volumen.** Si `BaseVolume` es mayor que cero, se usa como cantidad inicial de la orden. De lo contrario, el motor deriva el tamaño de `RiskPercent`, la distancia del stop y los parámetros de paso del instrumento. Cada volumen calculado se verifica contra los límites del instrumento y se limita por `MaxVolume`.
- **Gestión de órdenes.** Cada entrada puede estar equipada con stop-loss, take-profit y trailing stop (todos en pips). La distancia del take-profit se multiplica por `TakeProfitCoefficient` para entradas sucesivas para ampliar los objetivos.
- **Protección de cuenta.** Cuando el número total de posiciones abiertas supera `PositionsForProtection` y su beneficio combinado alcanza `SecureProfit`, la estrategia cierra la operación con mayor beneficio para asegurar ganancias. Si el número total de posiciones supera `MaxPositions`, cierra la peor operación antes de aceptar nuevas entradas.

## Manejo de posiciones
- Los stops, la lógica de trailing y los objetivos se evalúan en velas terminadas usando precios de cierre, máximo y mínimo.
- Todas las posiciones abiertas se rastrean con su propio volumen, precio de entrada y estado de trailing. El último precio de llenado se reutiliza para hacer cumplir el espaciado mínimo para futuras entradas.
- Cuando el saldo de la cuenta cae por debajo de `MinimumBalance`, la estrategia se detiene para evitar el sobretrading en cuentas pequeñas.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `BaseVolume` | Tamaño inicial de la orden. Establecer en cero para habilitar el dimensionamiento basado en riesgo mediante `RiskPercent`. |
| `RiskPercent` | Porcentaje del capital de la cartera a arriesgar cuando el tamaño de la posición se deriva de la distancia del stop. |
| `MaxPositions` | Número máximo de entradas abiertas simultáneamente. |
| `IntervalPips` | Espaciado inicial entre entradas de cuadrícula en pips. |
| `IntervalCoefficient` | Multiplicador aplicado al espaciado después de cada entrada adicional. |
| `StopLossPips` | Distancia del stop-loss en pips. Establecer en cero para deshabilitar. |
| `TakeProfitPips` | Distancia base del take-profit en pips. Multiplicada por `TakeProfitCoefficient` por entrada. |
| `TrailingStopPips` / `TrailingStepPips` | Distancia del trailing stop y avance requerido antes de que el trailing se ajuste. |
| `TakeProfitCoefficient` | Multiplicador para ampliar las distancias del take-profit en entradas posteriores. |
| `SecureProfit` | Umbral de beneficio que activa la protección de cuenta una vez que hay suficientes posiciones abiertas. |
| `AccountProtection` | Habilita el aseguramiento automático de beneficios cerrando la mejor operación. |
| `PositionsForProtection` | Número mínimo de posiciones abiertas requeridas antes de que la protección de cuenta se active. |
| `ReverseCondition` | Invierte la interpretación de la pendiente del MACD. |
| `FastLength`, `SlowLength`, `SignalLength` | Períodos de las medias móviles exponenciales de cero retraso. |
| `MaxVolume` | Límite para el volumen de una sola entrada. |
| `LotMultiplier` | Factor multiplicativo para escalar el tamaño de posición con cada entrada de cuadrícula. |
| `MinimumBalance` | Saldo mínimo de cuenta requerido para continuar operando. |
| `CandleType` | Tipo de datos de vela usado para los cálculos. |

## Notas de uso
1. Conecte la estrategia a una cartera y seguridad antes de iniciarla.
2. Revise el paso del instrumento y la configuración de precio para asegurar que las conversiones de pips son correctas.
3. Los parámetros predeterminados replican el comportamiento del asesor experto original, pero pueden optimizarse a través de los optimizadores de StockSharp.
4. La traducción a Python no está incluida para esta estrategia.
