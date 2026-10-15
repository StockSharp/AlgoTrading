# Estrategia Fibonacci Retracement Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Fibonacci Retracement Momentum** es una conversión del asesor experto original de MetaTrader "FIBONACCI.mq4" a la API de alto nivel de StockSharp. La estrategia combina niveles de retroceso de Fibonacci multi-marcos temporales con filtros de momentum y MACD para temporizar las entradas en pullback en la dirección de la tendencia prevaleciente. La lógica de trading principal se ejecuta en el marco temporal base, mientras que los datos de confirmación se derivan de períodos de agregación superiores.

El algoritmo fue reescrito desde cero usando expresiones idiomáticas de StockSharp: suscripciones de velas, enlaces de indicadores y los helpers de gestión de órdenes integrados. La lógica de trailing de la fuente EA se simplificó para centrarse en el comportamiento central de ruptura de retroceso, mientras se preserva la estructura de señal original (toque de Fibonacci + impulso de momentum + filtro de tendencia).

## Cómo funciona
1. **Marco temporal primario** — la estrategia se suscribe a las velas base seleccionadas (15 minutos por defecto) y calcula dos medias móviles ponderadas (rápida y lenta) para evaluar la dirección local.
2. **Marco temporal de anclaje Fibonacci** — el marco temporal superior (por defecto: 1 hora) proporciona la vela completada más reciente. Su máximo/mínimo se usa para construir la cuadrícula de retroceso de Fibonacci de 0%–100%. El mismo flujo de velas alimenta un indicador de momentum (retrospectiva 14) y la desviación absoluta del nivel neutral 100 se almacena para las últimas tres barras.
3. **Marco temporal del filtro MACD** — un MACD a largo plazo (por defecto: 12/26/9) se calcula en velas mensuales (aproximación de 30 días) y actúa como filtro de confirmación de tendencia.
4. En cada vela base finalizada, el algoritmo verifica si el precio retrocedió a cualquier nivel de Fibonacci mientras los cierres anteriores se mantuvieron en el lado opuesto de ese nivel. Combinado con alineación de medias móviles, impulso de momentum y confirmación MACD, se abre una operación.
5. Las salidas protectoras dependen de distancias de stop-loss y take-profit expresadas en pasos de precio. Si el precio se mueve contra la posición o alcanza el objetivo, la posición se liquida.

## Reglas de entrada
### Configuración larga
- La última vela del marco temporal superior define los niveles de Fibonacci; el mínimo de la vela base actual toca o penetra cualquier nivel mientras al menos uno de los tres cierres anteriores permaneció por encima de él.
- La media móvil ponderada rápida está por encima de la media móvil ponderada lenta en el marco temporal base.
- La desviación de momentum `|Momentum - 100|` en el marco temporal superior supera el umbral configurado para cualquiera de los últimos tres valores.
- La línea principal del MACD está por encima de la línea de señal en el marco temporal MACD.
- Verificación estructural: el máximo de la vela base anterior está por encima del mínimo de dos barras atrás (refleja `Low[2] < High[1]` del EA).

### Configuración corta
- El máximo de la vela base actual toca cualquier nivel de Fibonacci mientras al menos uno de los últimos tres cierres se mantuvo por debajo de él.
- La media móvil ponderada rápida está por debajo de la media móvil ponderada lenta.
- La desviación de momentum supera el umbral para cualquiera de las últimas tres lecturas.
- La línea principal del MACD está por debajo de la línea de señal en el marco temporal MACD.
- Verificación estructural: el máximo de la vela anterior está por encima del mínimo de la barra inmediatamente anterior (análogo a `Low[1] < High[2]`).

### Gestión de posición
- Si aparece una señal opuesta mientras hay una posición abierta, la estrategia primero cierra la posición existente y espera a la siguiente barra para iniciar la reversión. Esto refleja el manejo conservador de órdenes del código MQL original.

## Gestión de riesgo
- **Stop loss / Take profit** — configurado en múltiplos del paso de precio del instrumento. Cero deshabilita la salida correspondiente.
- **Seguimiento del precio de entrada** — el precio de llenado se aproxima por el cierre de la vela de señal y se usa para calcular las distancias protectoras.

## Parámetros
| Parámetro | Por defecto | Descripción |
|-----------|---------|-------------|
| `FastMaLength` | 6 | Longitud de la media móvil ponderada rápida en el marco temporal base. |
| `SlowMaLength` | 85 | Longitud de la media móvil ponderada lenta. |
| `MomentumLength` | 14 | Período de retrospectiva del momentum en el marco temporal Fibonacci. |
| `MomentumThreshold` | 0.3 | Desviación absoluta mínima de 100 requerida para validar el momentum. |
| `StopLossSteps` | 20 | Distancia de stop-loss en pasos de precio (0 deshabilita). |
| `TakeProfitSteps` | 50 | Distancia de take-profit en pasos de precio (0 deshabilita). |
| `MacdFastLength` | 12 | Longitud de la EMA rápida usada dentro del MACD. |
| `MacdSlowLength` | 26 | Longitud de la EMA lenta usada dentro del MACD. |
| `MacdSignalLength` | 9 | Longitud de la EMA de señal usada dentro del MACD. |
| `CandleType` | Velas de 15 minutos | Marco temporal de ejecución primario. |
| `FibonacciCandleType` | Velas de 1 hora | Marco temporal que suministra anclas de Fibonacci y momentum. |
| `MacdCandleType` | Velas de 30 días | Marco temporal que suministra el filtro de tendencia MACD. |

## Notas de uso
- Ajuste los parámetros de marco temporal para coincidir con el mapeo original del EA (p.ej., M5 → M30, M15 → H1). StockSharp permite cualquier tipo de vela, incluyendo barras de rango o tick.
- Debido a que la estrategia usa `ClosePosition()` para liquidar, la propiedad `Volume` debe coincidir con el tamaño de operación deseado (por defecto: equivalente a 1 lote).
- La conversión se centra en la lógica impulsada por indicadores; los extras de gestión monetaria de la versión MQL (equity stop, trailing por saldo de cuenta, etc.) fueron omitidos intencionalmente para mayor claridad. Puede extender la clase con protección adicional conectando `ManageRisk`.
- Ejecute la estrategia dentro de StockSharp Designer, Shell o Runner con los adaptadores de datos de mercado necesarios configurados.
