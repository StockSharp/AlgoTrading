# Estrategia de Ruptura de Vela Anterior 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura que replica el experto MetaTrader "Previous Candle Breakdown 2". El algoritmo observa la vela completada más recientemente en un marco temporal configurable y activa operaciones cuando el precio supera su máximo o mínimo por un desplazamiento de pips definido por el usuario. Filtrado opcional por medias móviles, horas de negociación estrictas, dimensionamiento de posición por volumen fijo o riesgo porcentual, y salidas de protección en capas replican el comportamiento de la versión MQL original dentro de StockSharp.

## Descripción general
- **Lógica de entrada**: Entrar largo cuando el precio supera el máximo de la vela anterior más un desplazamiento. Entrar corto cuando el precio rompe por debajo del mínimo de la vela anterior menos el mismo desplazamiento.
- **Filtros**: Medias móviles rápidas/lentas opcionales con parámetros de desplazamiento requieren confirmación direccional antes de operar. La negociación también está limitada a una ventana de tiempo de inicio/fin.
- **Dimensionamiento de posición**: Elegir entre un volumen de orden fijo o dimensionamiento dinámico basado en el valor del portafolio y la distancia del stop-loss.
- **Controles de riesgo**: Niveles de stop-loss y take-profit estáticos en pips, trailing stop con un filtro de paso, y un objetivo de beneficio global que cierra todas las posiciones.
- **Escala**: El límite `MaxPositions` limita el tamaño absoluto de la posición neta para cada dirección.

## Valores predeterminados
- `IndentPips` = 10
- `FastPeriod` = 10, `FastShift` = 3, `SlowPeriod` = 30, `SlowShift` = 0, `MaMethod` = Simple
- `StopLossPips` = 50, `TakeProfitPips` = 150
- `TrailingStopPips` = 15, `TrailingStepPips` = 5
- `ProfitClose` = 100 (unidades de moneda de PnL realizado + no realizado)
- `MaxPositions` = 10 (recuento absoluto de contratos/lotes por lado)
- `OrderVolume` = 0 (deshabilitado), `RiskPercent` = 5 (usado cuando `OrderVolume` es cero y el stop-loss está activo)
- `StartTime` = 09:09, `EndTime` = 19:19
- `CandleType` = marco temporal de 4 horas

## Reglas de negociación
1. Suscribirse a la serie de velas configurada y registrar cada vela finalizada.
2. Comprobar si la hora actual cae dentro de la sesión de negociación permitida. Si se alcanza `ProfitClose`, aplanar inmediatamente.
3. Calcular los niveles de ruptura añadiendo/restando el desplazamiento en pips del máximo y mínimo de la vela anterior.
4. Cuando el precio rompe esos niveles y se satisfacen las condiciones de MA (si están habilitadas), abrir operaciones respetando el límite `MaxPositions`.
5. Establecer distancias iniciales de stop-loss y take-profit desde el precio de entrada y activar trailing stops una vez que el precio se haya movido a favor de la operación al menos la distancia de trailing más el paso.
6. Monitorear continuamente las velas: activar salidas de stop-loss/take-profit cuando se toquen, arrastrar stops a medida que avanza el precio y restablecer niveles de protección una vez que las posiciones estén cerradas.

## Notas
- Los cálculos de pips se ajustan automáticamente para instrumentos de 3 o 5 decimales para imitar la conversión de punto a pip de MetaTrader.
- Al usar dimensionamiento de riesgo porcentual, el algoritmo estima el volumen a partir del valor actual del portafolio y el stop-loss configurado.
- La comprobación de ruptura usa velas finalizadas, por lo que los picos dentro de la barra se evalúan a los niveles de cierre/máximo/mínimo de la vela.
- `MaxPositions` trabaja con la posición neta de la estrategia. Si se usan volúmenes fraccionarios, el parámetro representa el tamaño neto absoluto máximo permitido por dirección.
- Los gráficos muestran velas, las medias móviles activas cuando están habilitadas, y las operaciones ejecutadas para confirmación visual.
