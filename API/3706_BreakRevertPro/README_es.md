# Estrategia BreakRevert Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

BreakRevert Pro es la conversión StockSharp del asesor experto MetaTrader 5 *BreakRevertPro.mq5*. La estrategia combina la confirmación de ruptura en el marco temporal de un minuto con un contexto más amplio de tendencia y volatilidad en los gráficos de 15 minutos y una hora. Las puntuaciones de estilo de probabilidad se reproducen mediante aproximaciones basadas en indicadores para que el comportamiento se mantenga cercano al EA original mientras se siguen StockSharp patrones de alto nivel API.

## Lógica principal

1. **Período de tiempo principal (1 minuto)**
   - El rango verdadero promedio (ATR) estima la volatilidad intradía.
   - Un promedio móvil de precios de cierre mide el sesgo direccional a corto plazo.
   - Un segundo promedio móvil rastrea la frecuencia de grandes movimientos de vela a vela, lo que representa la probabilidad de ruptura de Poisson del código MQL.
   - Una media móvil exponencial de movimientos de precios absolutos produce la probabilidad de estilo exponencial utilizada por el filtro de seguridad original.
2. **Plazo de confirmación (15 minutos)**
   - Una media móvil simple mide la dirección de la tendencia a medio plazo y bloquea las operaciones contra el flujo dominante.
3. **Plazo de contexto (1 hora)**
   - Las velas horarias proporcionan la tendencia de marco temporal más alto y el rango de volatilidad requerido para la validación de ruptura y las comprobaciones de aplanamiento de reversión a la media.

Cuando las probabilidades proxy de Poisson y Weibull superan el umbral de ruptura, las tendencias de 1 minuto y 15 minutos se alinean al alza y la volatilidad horaria es elevada, la estrategia entra en una operación de ruptura larga. Por el contrario, cuando las probabilidades caen por debajo del umbral de reversión a la media y la tendencia horaria es plana, la estrategia vende en corto, apuntando a retrocesos de regreso al rango. Las órdenes de mercado se utilizan para reflejar el estilo de ejecución inmediata del asesor experto original.

## Gestión del riesgo

- Un retraso comercial configurable evita el exceso de comercio al imponer una pausa entre entradas consecutivas.
- `MaxPositions` limita el número de posiciones abiertas simultáneas. Al revertir una operación opuesta, la estrategia cierra la exposición actual y abre la nueva dirección en una orden de mercado única.
- La estimación de volumen dinámico utiliza el saldo de la cuenta, la distancia de parada derivada de ATR y el porcentaje de `RiskPerTrade` para producir un tamaño de lote conservador. Si el cálculo falla, el volumen de paso mínimo se utiliza como valor predeterminado seguro.
- Se pueden habilitar intercambios de seguridad opcionales para entornos de validación o prueba donde debe aparecer al menos un intercambio. La dirección del comercio de seguridad sigue la estimación combinada de la tendencia a corto y mediano plazo.
- `StartProtection()` activa el bloque de protección integrado de StockSharp para que problemas de conexión inesperados no dejen posiciones sin administrar.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `RiskPerTrade` | Riesgo por operación en porcentaje del valor de la cartera (utilizado para el cálculo dinámico del lote). |
| `LookbackPeriod` | Número de velas terminadas utilizadas para los promedios móviles y los cálculos de ATR en todos los períodos de tiempo. |
| `BreakoutThreshold` | Probabilidad compuesta mínima requerida para una entrada de ruptura. |
| `MeanReversionThreshold` | Máxima probabilidad que todavía permite posiciones cortas de reversión a la media. |
| `TradeDelaySeconds` | Número mínimo de segundos entre entradas consecutivas. |
| `MaxPositions` | Posiciones máximas simultáneas (utilizadas tanto para exposiciones largas como cortas). |
| `EnableSafetyTrade` | Habilita operaciones de seguridad de validación opcionales cuando no hay posiciones abiertas. |
| `SafetyTradeIntervalSeconds` | Período de espera entre controles comerciales de seguridad. |
| `CandleType` | Periodo de tiempo principal utilizado para la suscripción de la señal principal (predeterminado: 1 minuto). |

## Notas de uso

1. Adjunte la estrategia a un instrumento que admita datos de 1 minuto y proporcione velas de 15 minutos y 1 hora (StockSharp agregará marcos más altos automáticamente cuando el corredor proporcione barras de minutos).
2. Establezca la propiedad `Volume` si se requiere un tamaño de pedido fijo. De lo contrario, la estrategia deriva un tamaño conservador del saldo de la cuenta y ATR.
3. Ajuste los umbrales y las duraciones retrospectivas de acuerdo con el perfil de volatilidad del mercado objetivo. Los pares de mayor volatilidad pueden beneficiarse de umbrales más altos para evitar frecuentes rupturas falsas.
4. Las operaciones de seguridad están destinadas principalmente a escenarios de validación en los que el EA original ejecutó al menos una operación incluso sin una señal. Desactívelos para entornos comerciales normales en vivo.

La conversión conserva la idea original de combinar la detección de rupturas con salvaguardas de reversión mientras se basa en el marco de indicadores de alto nivel de StockSharp para seguir siendo eficiente y fácil de realizar pruebas.
