# Estrategia de Área de Recuperación por Zonas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Área de Recuperación por Zonas** es una conversión directa del expert advisor de MetaTrader "Zone Recovery Area" (paquete `MQL/20266`). Recrea la lógica de cobertura original sobre la API de alto nivel de StockSharp y agrega parametrización exhaustiva para que el comportamiento pueda ajustarse sin modificar el código. La estrategia combina un filtro de tendencia con una cuadrícula de recuperación de compra/venta alternante: una vez que se abre una operación primaria, se apilan posiciones adicionales cada vez que el precio sale o vuelve a entrar en la zona predefinida, creando una cesta cubierta que apunta a recuperar las reducciones flotantes.

Características principales:
- Utiliza un cruce de media móvil simple rápida/lenta junto con un filtro MACD mensual para definir el sesgo de trading.
- Implementa la técnica de recuperación por zonas: la primera operación establece un precio base, y las órdenes de cobertura alternantes se activan cada vez que el mercado cruza el límite de la zona o regresa al nivel base.
- Proporciona controles de beneficio basados en dinero, porcentaje y trailing para salir de la cesta una vez que se ha asegurado suficiente beneficio.
- Permite tanto el dimensionamiento de posición multiplicativo (estilo martingala) como aditivo para cada paso de recuperación.

## Datos de mercado e indicadores
- **Velas principales:** marco temporal definido por el usuario (por defecto 30 minutos) para entradas y gestión de recuperación.
- **Velas mensuales:** construidas a partir de marcos temporales inferiores si es necesario; usadas para calcular los valores de MACD (12/26/9).
- **Indicadores:**
  - Media Móvil Simple (rápida y lenta) en el marco temporal principal.
  - Convergencia/Divergencia de Medias Móviles con línea de señal en el marco temporal mensual.

## Lógica de trading
1. **Validación de tendencia**
   - Esperar hasta que ambas SMA y el MACD mensual estén completamente formados.
   - Una configuración alcista requiere que la SMA rápida esté por debajo de la lenta en la barra anterior mientras la línea MACD mensual está por encima de su señal.
   - Una configuración bajista requiere que la SMA rápida esté por encima de la lenta en la barra anterior mientras la línea MACD mensual está por debajo de su señal.
2. **Inicialización del ciclo**
   - Cuando se detecta una configuración alcista (bajista), abrir la posición larga (corta) inicial con `InitialVolume` y almacenar el precio de entrada como base del ciclo.
   - Restablecer los contadores internos y el seguimiento de beneficios para el nuevo ciclo.
3. **Motor de recuperación por zonas**
   - Definir dos niveles críticos: el **límite de zona** (`ZoneRecoveryPips`) alejado del precio base y el **nivel de take-profit** (`TakeProfitPips`) en la dirección favorable.
   - Mientras el ciclo está activo, monitorear cada vela completada:
     - Si el precio alcanza el nivel de take-profit, cerrar toda la exposición neta y terminar el ciclo.
     - Si se alcanzan los objetivos de dinero o porcentaje de beneficio, o se activa el bloqueo de beneficio trailing, cerrar el ciclo.
     - De lo contrario, evaluar si se necesita una nueva cobertura:
       - Para ciclos largos: abrir un corto adicional cuando el precio cae por debajo de `base - zone`, y abrir un largo adicional cuando el precio regresa por encima del precio base.
       - Para ciclos cortos: abrir un largo adicional cuando el precio sube por encima de `base + zone`, y abrir un corto adicional cuando el precio regresa por debajo del precio base.
     - La dirección de cobertura alterna automáticamente; el tamaño de la próxima orden se determina multiplicando el volumen anterior o añadiendo un incremento fijo.
   - El número de operaciones por cesta está limitado por `MaxTrades`.
4. **Gestión de beneficios**
   - `UseMoneyTakeProfit`: cerrar la cesta una vez que el beneficio no realizado alcance el importe de divisa configurado.
   - `UsePercentTakeProfit`: cerrar la cesta una vez que el beneficio no realizado iguale el porcentaje especificado del valor de la cartera.
   - `EnableTrailing`: una vez que el beneficio supera `TrailingStartProfit`, seguir el pico y salir del ciclo si el beneficio cae por `TrailingDrawdown`.

Todas las órdenes se colocan usando los helpers de alto nivel de StockSharp (`BuyMarket`/`SellMarket`), lo que mantiene la implementación consistente con las mejores prácticas del framework.

## Parámetros
| Nombre | Por defecto | Descripción |
| ------ | ----------- | ----------- |
| `CandleType` | Velas de 30 minutos | Marco temporal para entradas y monitoreo de recuperación. |
| `MonthlyCandleType` | Velas de 30 días | Marco temporal superior utilizado para construir el filtro de tendencia MACD. |
| `FastMaLength` | 20 | Período de la SMA rápida. |
| `SlowMaLength` | 200 | Período de la SMA lenta. |
| `TakeProfitPips` | 150 | Distancia desde el precio base para cerrar toda la cesta en beneficio. |
| `ZoneRecoveryPips` | 50 | Semiancho de la zona de cobertura alrededor del precio base. |
| `InitialVolume` | 1 | Volumen de la primera operación en cada ciclo. |
| `UseVolumeMultiplier` | true | Si está habilitado, cada nueva cobertura multiplica el volumen anterior. |
| `VolumeMultiplier` | 2 | Factor aplicado al volumen anterior cuando `UseVolumeMultiplier` es true. |
| `VolumeIncrement` | 0.5 | Incremento de volumen aditivo cuando `UseVolumeMultiplier` es false. |
| `MaxTrades` | 6 | Número máximo de operaciones por ciclo de recuperación (incluida la inicial). |
| `UseMoneyTakeProfit` | false | Habilitar take-profit basado en dinero. |
| `MoneyTakeProfit` | 40 | Objetivo de beneficio en divisa de la cuenta. |
| `UsePercentTakeProfit` | false | Habilitar take-profit basado en porcentaje. |
| `PercentTakeProfit` | 5 | Objetivo de beneficio como porcentaje del valor de la cartera. |
| `EnableTrailing` | true | Habilitar protección de beneficio trailing. |
| `TrailingStartProfit` | 40 | Umbral de beneficio requerido antes de que el trailing se active. |
| `TrailingDrawdown` | 10 | Retroceso de beneficio permitido una vez que el trailing está activo. |

> **Conversión de pips:** `TakeProfitPips` y `ZoneRecoveryPips` se convierten en desplazamientos de precio usando el paso de precio del instrumento. Asegúrese de que el instrumento negociado proporcione valores correctos de `PriceStep` y `StepPrice`.

## Notas de uso
1. Agregue la estrategia a su solución StockSharp (Designer, API, Runner, etc.).
2. Asigne el instrumento y la cartera deseados antes de iniciar.
3. Ajuste los parámetros para que coincidan con la volatilidad del instrumento, la reducción aceptable y el tamaño de la cuenta.
4. Asegúrese de que haya suficientes datos históricos para que tanto las SMA como el MACD mensual puedan calentarse antes de la primera operación.
5. Monitoree cuidadosamente el uso de margen: los pasos de recuperación pueden aumentar rápidamente la exposición, especialmente cuando el multiplicador está habilitado.

## Gestión de riesgos y consideraciones
- Las técnicas de recuperación por zonas/martingala pueden acumular posiciones muy grandes en mercados en tendencia. Siempre pruebe con configuraciones conservadoras y use el parámetro `MaxTrades` para limitar el riesgo.
- Dado que StockSharp mantiene una única posición neta, el cálculo interno de beneficios replica el PnL de la cesta usando información de precio/paso del instrumento. Valide las cifras con el feed de datos de su broker.
- Los objetivos de dinero y porcentaje dependen de la valoración de la cartera. Al realizar backtesting o paper trading, asegúrese de que el modelo de cartera suministre correctamente `BeginValue`/`CurrentValue`.
- No se usa stop-loss duro automático; el riesgo se gestiona a través de la mecánica de recuperación. Considere combinar la estrategia con stops de nivel de cartera externos.

## Archivos
- `CS/ZoneRecoveryAreaStrategy.cs` — implementación de la estrategia.
- `README.md` — documentación en inglés (este archivo).
- `README_ru.md` — documentación en ruso.
- `README_zh.md` — documentación en chino.
