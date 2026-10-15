# Experto Master Estrategia EURUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Expert Master EURUSD replica el MetaTrader 4 Expert Advisor *Expert Master*.
Evalúa un patrón de cuatro velas en las líneas principal y de señal MACD (rápida EMA = 5, lenta EMA = 15, señal EMA = 3).
El algoritmo espera que el indicador genere impulso en una dirección antes de desencadenar una entrada de ruptura en la dirección opuesta.

## Lógica de trading

### Configuración larga
1. La línea de señal MACD forma una secuencia descendente en las tres velas anteriores y gira hacia arriba en la vela actual.
2. La línea principal MACD forma una "V" donde el valor actual está por encima de las tres lecturas anteriores.
3. El valor de la línea principal anterior está por debajo del umbral inferior configurable (predeterminado −0,00020).
4. El valor de la línea principal más antiguo está por debajo de cero, mientras que el valor actual está por encima del umbral superior (predeterminado 0,00020).

### Configuración corta
1. La línea de señal MACD forma una secuencia ascendente en las tres velas anteriores y gira hacia abajo en la vela actual.
2. La línea principal MACD forma una "V" invertida donde el valor actual está por debajo de las tres lecturas anteriores.
3. El valor de la línea principal anterior supera el umbral superior (predeterminado 0,00020).
4. El valor de la línea principal más antiguo está por encima de cero, mientras que el valor actual cae por debajo del umbral corto (predeterminado −0,00035).

## Gestión de Puestos

- **Salir con pérdida de impulso:** Una posición larga se cierra cuando el valor principal actual de MACD cae por debajo del anterior.
Las posiciones cortas se cierran cuando el valor principal actual MACD supera el anterior.
- **Trailing Stop:** Después de que el precio se mueve por el número configurado de puntos a favor de la operación, se activa un trailing stop.
La parada se actualiza en cada vela terminada utilizando el cierre de la vela menos/más la distancia final.
Si el precio retrocede hasta el trailing stop, la estrategia sale mediante una orden de mercado.

## Gestión del riesgo

- El volumen de operaciones por defecto es el tamaño de lote fijo, pero se puede ajustar dinámicamente a través del parámetro **Porcentaje de riesgo**.
Cuando se habilita el tamaño del riesgo, la estrategia arriesga una fracción del valor de la cartera en cada entrada, imitando el comportamiento original de EA.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TrailingPoints` | Distancia del trailing stop en puntos de precio. | 25 |
| `FixedVolume` | Volumen de operaciones alternativo cuando el tamaño del riesgo no está disponible. | 1 |
| `RiskPercent` | Porcentaje del valor de la cartera utilizado para dimensionar las posiciones. | 0,01 |
| `MacdFastPeriod` | Longitud rápida de EMA para la línea principal MACD. | 5 |
| `MacdSlowPeriod` | Longitud lenta de EMA para la línea principal MACD. | 15 |
| `MacdSignalPeriod` | Longitud de la señal EMA para el indicador MACD. | 3 |
| `UpperMacdThreshold` | Umbral positivo MACD requerido para las entradas. | 0.00020 |
| `LowerMacdThreshold` | Umbral negativo MACD utilizado en señales largas. | −0,00020 |
| `ShortCurrentThreshold` | Umbral negativo MACD aplicado al valor actual para cortos. | −0,00035 |
| `CandleType` | Tipo de vela utilizado para los cálculos del indicador. | marco de tiempo de 1 minuto |

## Notas

- Opere únicamente con velas terminadas para mantenerse alineado con el nivel alto StockSharp API.
- La conversión mantiene la lógica EA original, incluido el tamaño del lote basado en el riesgo y el comportamiento de trailing-stop, al tiempo que agrega una parametrización exhaustiva para facilitar la optimización.
