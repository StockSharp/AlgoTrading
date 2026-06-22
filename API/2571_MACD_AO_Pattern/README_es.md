# Estrategia de Patrón MACD AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una fiel conversión a StockSharp del asesor experto FORTRADER `MACD.mq5`. Implementa el patrón "AOP" que observa el oscilador MACD en busca de excursiones profundas alejadas de la línea cero seguidas de un gancho de vuelta hacia la neutralidad. Cuando el gancho se confirma, la estrategia entra en la dirección de la reversión esperada y aplica inmediatamente objetivos fijos de stop-loss y take-profit expresados en pips.

## Lógica de la estrategia
### Preparación de datos
- Opera sobre la serie de velas seleccionada por el parámetro `CandleType` (velas de 5 minutos por defecto).
- Utiliza un indicador MACD estándar con períodos rápido, lento y señal configurables (valores predeterminados 12/26/9).
- Almacena los valores de la línea principal MACD de las tres velas completadas más recientes para reproducir el acceso basado en índice de MQL (`iMACD(...,1..3)`).

### Configuración corta (gancho bajista)
1. **Armado** – una vez que la línea principal MACD de la última vela cerrada cae por debajo de `BearishExtremeLevel` (predeterminado −0.0015), la estrategia comienza a vigilar una reversión.
2. **Retroceso neutral** – cuando el MACD sube de vuelta por encima de `BearishNeutralLevel` (predeterminado −0.0005), la etapa de validación del gancho se activa.
3. **Confirmación del gancho** – los tres valores MACD anteriores deben formar un máximo local (`macd₁ < macd₂ > macd₃`) mientras el valor más reciente permanece por debajo del nivel neutral y el anterior permanece por encima. Esto recrea el patrón original que asegura que el momentum está disminuyendo.
4. **Entrada** – si no hay posición larga abierta (`Position <= 0`), se envía una orden de venta a mercado de `OrderVolume`. Los niveles de protección se calculan inmediatamente: stop-loss por encima de la entrada en `StopLossPips` y take-profit por debajo en `TakeProfitPips` (convertidos a precio mediante `GetPipSize`).
5. Cualquier lectura positiva de MACD cancela la configuración y reinicia la máquina de estado bajista hasta que aparezca un nuevo tramo negativo profundo.

### Configuración larga (gancho alcista)
1. **Armado** – una vez que el MACD sube por encima de `BullishExtremeLevel` (predeterminado +0.0015), se activa el modo de vigilancia alcista.
2. **Cancelación inmediata** – si el MACD cae por debajo de cero, el escenario alcista se abandona, reflejando la lógica de MQL.
3. **Retroceso neutral** – una caída de vuelta por debajo de `BullishNeutralLevel` (predeterminado +0.0005) prepara la confirmación del gancho.
4. **Confirmación del gancho** – los tres valores MACD almacenados deben crear un mínimo local (`macd₁ > macd₂ < macd₃`) respetando los umbrales neutrales.
5. **Entrada** – si no hay exposición corta (`Position >= 0`), la estrategia compra a mercado con `OrderVolume` y establece stop-loss y take-profit alrededor de la entrada simétricamente a las reglas cortas.

### Gestión de riesgo
- El stop-loss y el take-profit siempre están activos mediante `_stopPrice` y `_takePrice`. Se evalúan en cada vela completada usando el máximo/mínimo registrado para emular la ejecución del lado del broker en el EA original.
- Los pips se convierten a precios absolutos usando `Security.PriceStep`. Para símbolos FX de 3 y 5 dígitos, el paso se multiplica por 10 para coincidir con el ajuste de MQL para pips fraccionarios.
- Cuando la estrategia sale de una posición por los niveles de protección, los limpia inmediatamente y espera una nueva configuración en las siguientes velas.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `CandleType` | Serie de datos de velas procesada por la estrategia. | Marco temporal de 5 minutos |
| `OrderVolume` | Volumen enviado con cada orden de mercado. | 0.1 |
| `TakeProfitPips` | Distancia al objetivo de beneficio en pips. Marcado para optimización. | 60 |
| `StopLossPips` | Distancia al stop-loss en pips. Marcado para optimización. | 70 |
| `MacdFastPeriod` | Longitud de la EMA rápida para MACD. | 12 |
| `MacdSlowPeriod` | Longitud de la EMA lenta para MACD. | 26 |
| `MacdSignalPeriod` | Longitud de la EMA señal para MACD. | 9 |
| `BearishExtremeLevel` | Umbral negativo de MACD que arma oportunidades cortas. | −0.0015 |
| `BearishNeutralLevel` | Umbral negativo de MACD usado para validar el gancho bajista. | −0.0005 |
| `BullishExtremeLevel` | Umbral positivo de MACD que arma oportunidades largas. | +0.0015 |
| `BullishNeutralLevel` | Umbral positivo de MACD usado para validar el gancho alcista. | +0.0005 |

## Notas adicionales
- La estrategia solo reacciona una vez por vela terminada, imitando el guardián `PrevBars` original en MQL.
- La gestión de stop-loss/take-profit es puramente basada en precio; no hay ajustes de trailing ni reentradas hasta que el ciclo completo de la máquina de estado se complete de nuevo.
- Diseñado para cuentas de cobertura en el EA fuente, pero este port impone una única posición neta verificando `Position` antes de enviar nuevas órdenes.
- No se proporcionó versión Python según lo solicitado.
