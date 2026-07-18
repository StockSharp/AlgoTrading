# Estrategia Burg Extrapolator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La Estrategia Burg Extrapolator replica el asesor experto de MetaTrader "Burg Extrapolator" usando el API de alto nivel de StockSharp. El sistema aplica un modelo autorregresivo (AR) resuelto con el método Burg para pronosticar futuros precios de apertura. Las decisiones de trading se impulsan por la amplitud de la trayectoria del pronóstico: cuando la distancia prevista entre los futuros máximos y mínimos supera los umbrales configurados, la estrategia abre o cierra posiciones.

## Lógica principal

1. **Preparación de datos**
   - Recopila `PastBars` precios de apertura en cada vela finalizada.
   - Opcionalmente transforma la serie en valores de momentum logarítmico o de tasa de cambio.
   - Normaliza los precios restando el promedio móvil cuando se utilizan precios brutos.
2. **Modelado autorregresivo**
   - Estima los coeficientes AR mediante el método Burg con un orden determinado por `ModelOrderFraction`.
   - Extrapola varios pasos hacia adelante (horizonte de pronóstico = `PastBars - order - 1`) y reconstruye las predicciones de precio.
3. **Generación de señales**
   - Rastrea los precios máximos y mínimos previstos.
   - Si el swing del pronóstico supera `MinProfitPips`, genera una señal de entrada en la dirección respectiva.
   - Si el swing del pronóstico supera `MaxLossPips`, emite una señal de salida para posiciones existentes.
4. **Ejecución de órdenes**
   - Las posiciones se abren con órdenes de mercado usando el volumen calculado basado en riesgo.
   - Cuando ocurre un stop o señal opuesta, la estrategia cierra posiciones con órdenes de mercado.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `RiskPercent` | Porcentaje del capital arriesgado por operación. Se utiliza para dimensionar órdenes cuando hay una distancia de stop-loss disponible. |
| `MaxPositions` | Volumen acumulativo máximo expresado como múltiplos del tamaño de orden permitido por dirección. |
| `MinProfitPips` | Swing de beneficio previsto mínimo (en pips) requerido para abrir nuevas posiciones. |
| `MaxLossPips` | Máximo drawdown previsto permitido (en pips) que desencadenará salidas de posición. |
| `TakeProfitPips` | Distancia de take-profit estático (en pips). Establecer en cero para desactivar. |
| `StopLossPips` | Distancia de stop-loss estático (en pips). Requerido para dimensionamiento de riesgo. |
| `TrailingStopPips` | Distancia de trailing stop (en pips). Funciona solo cuando el stop-loss está habilitado. |
| `PastBars` | Número de barras históricas utilizadas como entrada al modelo Burg. |
| `ModelOrderFraction` | Fracción de `PastBars` que define el orden AR (truncamiento entero). |
| `UseMomentum` | Habilita el preprocesamiento de momentum logarítmico (`log(p[i]/p[i-1])`). |
| `UseRateOfChange` | Habilita el preprocesamiento de tasa de cambio (`p[i]/p[i-1]-1`) cuando el momentum está desactivado. |
| `OrderVolume` | Tamaño de orden de reserva cuando el dimensionamiento basado en riesgo no puede calcularse. |
| `CandleType` | Tipo de datos (marco temporal) de las velas utilizadas para los cálculos. |

## Reglas de Trading

- **Entrada**: Cuando la trayectoria prevista indica un swing mayor que `MinProfitPips`, abrir una posición larga si el precio proyectado más alto aparece primero, o abrir una posición corta si la proyección más baja aparece primero.
- **Salida**: Cerrar posiciones cuando el swing del pronóstico supera `MaxLossPips` o cuando se detecta la señal de entrada opuesta.
- **Protección**: Usa `StartProtection` para configurar stop-loss, take-profit y trailing stop opcionales en unidades de precio absolutas derivadas de pips.
- **Dimensionamiento de posición**: Si tanto `StopLossPips` como `RiskPercent` son positivos, el volumen de la operación se calcula como `risk_amount / (stop_distance)`. De lo contrario, se usa `OrderVolume`.

## Notas de Implementación

- Trabaja exclusivamente con velas finalizadas para evitar sesgos de anticipación.
- Evita las llamadas `GetValue` de indicadores procesando valores directamente dentro del callback `Bind`.
- Respeta las convenciones del API de alto nivel de StockSharp, usando `SubscribeCandles` y `StartProtection` para la gestión de riesgo.
- La lógica de trailing refleja el EA original al habilitar trailing stops gestionados por la plataforma.

## Consejos de Uso

- Elija `PastBars` y `ModelOrderFraction` cuidadosamente; los órdenes altos pueden llevar a sobreajuste o pronósticos inestables.
- El horizonte de pronóstico es igual a `PastBars - order - 1`; asegúrese de que el horizonte sea de al menos unas pocas barras manteniendo `ModelOrderFraction` por debajo de 1.
- Los modos de Momentum y ROC requieren precios positivos. Los instrumentos que pueden cruzar cero deben usar el modo de precio bruto.
- Para mercados con pips fraccionados, la estrategia escala automáticamente el tamaño del pip usando los decimales del instrumento (×10 para 3 o 5 decimales).

## Limitaciones

- El modelo AR asume estacionariedad; las tendencias fuertes o los cambios de régimen pueden reducir la precisión.
- Las señales basadas en pronóstico son sensibles al ruido—considere combinarlas con filtros adicionales si se usa en trading en vivo.
- El dimensionamiento de riesgo preciso requiere valoración de cartera y una distancia de stop-loss válida; de lo contrario se usan volúmenes predeterminados.
