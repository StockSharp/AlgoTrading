# RM Stochastic Estrategia de banda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **RM Stochastic Band Strategy** es una versión StockSharp de alto nivel del MetaTrader asesor experto *EA RM Stochastic Band* de Ronny Maheza. La estrategia observa tres osciladores estocásticos calculados en diferentes períodos de tiempo (base, medio y alto) y abre operaciones solo cuando los tres confirman condiciones de sobreventa o sobrecompra. Al entrar, los niveles de salida se derivan del rango verdadero promedio (ATR) medido en el período de tiempo más alto, replicando los niveles de stop-loss y take-profit basados ​​en ATR en el asesor experto original. Los filtros de ejecución adicionales incluyen un valor mínimo de cartera configurable como proxy de margen y un control de diferencial que adapta su tolerancia en función del diferencial observado.

## Lógica principal

1. **Confirmación estocástica de múltiples períodos**
   - El período de ejecución principal (predeterminado M1) genera la señal comercial.
   - Los plazos de confirmación (por defecto M5 y M15) deben coincidir con la dirección de la señal.
   - Se abre una operación sólo si los valores estocásticos %K en los tres marcos temporales están simultáneamente por debajo del nivel de sobreventa (configuración larga) o por encima del nivel de sobrecompra (configuración corta).

2. **Salidas basadas en volatilidad con ATR**
   - ATR se calcula en el período de tiempo más alto (predeterminado M15).
   - Stop-loss = `entry price ± ATR * StopLossMultiplier`.
   - Toma de ganancias = `entry price ± ATR * TakeProfitMultiplier`.
   - Los precios se monitorean en las velas del marco temporal base; si una vela toca cualquiera de los niveles, la posición se cierra en el mercado.

3. **Filtros de ejecución y seguridad**
   - Las órdenes se omiten cuando el diferencial observado (BestAsk - BestBid) supera el umbral adaptativo. Si el diferencial es superior al límite estándar, se aplica el límite de cuenta de centavos más flexible, reflejando la lógica fuente EA.
   - Las operaciones se bloquean mientras el valor de la cartera sea inferior a `MinMargin`.
   - Sólo se puede abrir una posición a la vez y no se inicia ninguna nueva operación si existen órdenes activas.

## Indicadores y Suscripciones

| Indicador | Plazo | Propósito |
|-----------|-----------|---------|
| Stochastic Oscilador | Plazo base (predeterminado 1 minuto) | Genera señal primaria (solo se usa %K). |
| Stochastic Oscilador | Periodo de tiempo medio (predeterminado 5 minutos) | Confirma la dirección de la señal principal. |
| Stochastic Oscilador | Plazo alto (predeterminado 15 minutos) | Proporciona confirmación a largo plazo. |
| Rango verdadero promedio | Plazo alto (predeterminado 15 minutos) | Define distancias de stop-loss y take-profit ajustadas por la volatilidad. |

Los datos de nivel 1 se suscriben para capturar la mejor oferta y solicitar una evaluación del diferencial.

## Reglas de entrada

- **Configuración larga**: Los tres valores estocásticos de %K están por debajo de `OversoldLevel`. Cuando se activa, la estrategia compra al volumen de mercado `OrderVolume` y almacena niveles de salida basados ​​en ATR.
- **Configuración breve**: Los tres valores estocásticos de %K están por encima de `OverboughtLevel`. Una venta de mercado se ejecuta con el mismo manejo de volumen.

## Reglas de salida

- **Stop-loss**: Para posiciones largas, salga cuando el mínimo de la vela toque `entry - ATR * StopLossMultiplier`. Para posiciones cortas, salga cuando el máximo de la vela alcance `entry + ATR * StopLossMultiplier`.
- **Take-profit**: Para posiciones largas, salga cuando el máximo de la vela toque `entry + ATR * TakeProfitMultiplier`. Para posiciones cortas, salga cuando el mínimo de la vela alcance `entry - ATR * TakeProfitMultiplier`.
- Después de una salida, los marcadores de posición internos de parada y objetivo se borran para que la siguiente señal pueda volver a calcular nuevos niveles.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Volumen de cada orden de mercado. | 0.1 |
| `StochasticLength` | Período retrospectivo de %K. | 5 |
| `StochasticSmoothing` | Suavizado aplicado a %K. | 3 |
| `StochasticSignalLength` | %D longitud. | 3 |
| `AtrPeriod` | periodo ATR en el período de tiempo alto. | 14 |
| `StopLossMultiplier` | multiplicador ATR para el stop-loss. | 1.5 |
| `TakeProfitMultiplier` | multiplicador ATR para la toma de ganancias. | 3.0 |
| `MinMargin` | Valor mínimo de cartera requerido para operar. | 100 |
| `MaxSpreadStandard` | Límite de diferencial para cuentas estándar. | 3 |
| `MaxSpreadCent` | Límite de diferencial utilizado cuando el diferencial actual ya supera el límite estándar. | 10 |
| `OversoldLevel` | Umbral de sobreventa para %K estocástico. | 20 |
| `OverboughtLevel` | Umbral de sobrecompra para %K estocástico. | 80 |
| `BaseCandleType` | Periodo de tiempo principal (velas predeterminadas de 1 minuto). | 1 minuto |
| `MidCandleType` | Plazo de confirmación (velas predeterminadas de 5 minutos). | 5 minutos |
| `HighCandleType` | Confirmación + periodo ATR de tiempo (velas predeterminadas de 15 minutos). | 15 minutos |

Todos los parámetros admiten rangos de optimización idénticos a las entradas MetaTrader cuando corresponda.

## Notas de implementación

- La estrategia utiliza `SubscribeCandles(...).BindEx(...)` para obtener valores de indicadores estrictamente a través del API de alto nivel según lo exigen las pautas del proyecto.
- La propagación se calcula a partir de actualizaciones en vivo de Nivel 1; sin datos de oferta/demanda, el comercio permanece deshabilitado, lo que garantiza una operación segura en fuentes de datos que no proporcionan cotizaciones.
- Las posiciones se gestionan exclusivamente a través de órdenes de mercado, reflejando el EA original que se basaba en entradas de mercado con niveles de stop-loss y take-profit precalculados.
- No existe una lógica de equilibrio o de seguimiento porque la fuente MQL no implementó esas características a pesar de tener parámetros de entrada relacionados.

## Consejos de uso

1. Adjunte la estrategia a la seguridad deseada y asegúrese de que los datos de Nivel 1 (oferta/demanda) estén disponibles para un filtrado de diferencial adecuado.
2. Ajuste los umbrales estocásticos y los multiplicadores ATR para que coincidan con el perfil de volatilidad del instrumento objetivo.
3. Al optimizar, considere probar diferentes combinaciones de marcos temporales si el mercado en el que opera tiene ciclos dominantes diferentes a los de la estructura original M1/M5/M15.
