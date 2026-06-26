# Estrategia Tunnel Method
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Tunnel Method es un port de StockSharp del asesor experto "Tunnel Method" publicado originalmente para MetaTrader 5. Utiliza tres medias móviles simples (SMA) desplazadas para detectar rupturas direccionales. La media rápida debe atravesar un "túnel" de precio creado por las medias lenta y media con una sangría configurable para confirmar una operación. La estrategia incluye reglas de gestión de posición idénticas a la versión MQL, incluidos niveles de stop-loss y take-profit fijos basados en pips, un trailing stop que bloquea ganancias con un filtro de paso, y un tiempo de espera mínimo entre evaluaciones de entrada.

## Lógica de la estrategia

- **Indicadores**: tres medias móviles simples en el mismo instrumento y marco temporal.
  - *Primera SMA* (línea lenta): período largo con desplazamiento cero. Define el límite inferior del túnel alcista y el límite superior del túnel bajista.
  - *Segunda SMA* (línea media): período medio con desplazamiento positivo. Se usa principalmente para señales cortas, creando una barrera proyectada hacia adelante.
  - *Tercera SMA* (línea rápida): período corto con el mayor desplazamiento positivo. Las rupturas de esta línea a través del túnel activan órdenes.
- **Sangría**: las medias móviles deben estar separadas por al menos `IndentPips` (convertidos a unidades de precio) para evitar condiciones volátiles. La media rápida debe cruzar de abajo hacia arriba la media lenta más la mitad de la sangría para abrir posiciones largas, y cruzar de arriba hacia abajo la media media menos la mitad de la sangría para abrir posiciones cortas.
- **Cadencia de entrada**: una nueva señal se evalúa solo cuando han transcurrido `PauseSeconds` desde la evaluación anterior. Esto refleja el EA original, que limita el procesamiento de OnTick para reducir el ruido.
- **Modo de posición única**: la estrategia mantiene solo una posición a la vez. Una nueva orden se ignora si ya hay otra posición abierta.

## Gestión de riesgo

- **Stop Loss**: distancia fija opcional por debajo (para posiciones largas) o por encima (para posiciones cortas) del precio de entrada, medida en pips mediante `StopLossPips`.
- **Take Profit**: objetivo fijo opcional en pips mediante `TakeProfitPips`.
- **Trailing Stop**: habilitado cuando tanto `TrailingStopPips` como `TrailingStepPips` son positivos. Una vez que el precio se mueve a favor de la operación por `TrailingStopPips + TrailingStepPips`, el stop se lleva a `TrailingStopPips` detrás del cierre actual. El trailing stop se actualiza solo cuando el precio avanza al menos el paso de trailing, evitando ajustes demasiado frecuentes.
- **Salida de posición**: la estrategia cierra posiciones al mercado cuando se violan stops, take profits o niveles de trailing. Esto replica cómo el EA original reaccionaría después de que el broker ejecute órdenes de protección.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `TradeVolume` | 1 | Volumen de orden por operación. |
| `StopLossPips` | 50 | Distancia de stop-loss en pips. Use `0` para deshabilitar. |
| `TakeProfitPips` | 50 | Distancia de take-profit en pips. Use `0` para deshabilitar. |
| `TrailingStopPips` | 5 | Distancia de trailing base en pips. Requiere `TrailingStepPips > 0`. |
| `TrailingStepPips` | 5 | Ganancia incremental mínima antes de que el trailing stop pueda moverse. |
| `FirstMaPeriod` | 160 | Período de la SMA lenta. |
| `FirstMaShift` | 0 | Desplazamiento hacia adelante de la SMA lenta. |
| `SecondMaPeriod` | 80 | Período de la SMA media usada para señales cortas. |
| `SecondMaShift` | 1 | Desplazamiento hacia adelante de la SMA media. |
| `ThirdMaPeriod` | 20 | Período de la SMA rápida. |
| `ThirdMaShift` | 2 | Desplazamiento hacia adelante de la SMA rápida. |
| `IndentPips` | 1 | Brecha mínima entre medias para validar una ruptura. |
| `PauseSeconds` | 45 | Retraso entre comprobaciones de entrada consecutivas. |
| `CandleType` | Marco temporal de 5 minutos | Serie de velas usada para cálculos del indicador. |

Todos los parámetros basados en pips se convierten automáticamente a unidades de precio usando el `PriceStep` del instrumento y la precisión decimal, con manejo especial para símbolos FX de 3 y 5 dígitos como en la versión de MetaTrader.

## Notas prácticas

1. **Configuración del instrumento**: asegúrese de que el `Security` asignado a la estrategia tenga `PriceStep` y `Decimals` correctos. Las distancias de pips convertidas serán de lo contrario imprecisas.
2. **Alineación de marcos temporales**: el `CandleType` predeterminado usa velas de 5 minutos, pero puede alinearlo con el marco temporal usado en MetaTrader (por ejemplo M1) cambiando el parámetro.
3. **Manejo de volumen**: `TradeVolume` define el tamaño total por entrada. La estrategia cierra posiciones con órdenes de mercado simétricas para que el tamaño de la posición se mantenga consistente.
4. **Requisitos de trailing**: el constructor aplica la regla del EA original: si `TrailingStopPips` es positivo mientras `TrailingStepPips` es cero, la estrategia lanza un error de inicialización para evitar configuraciones inconsistentes.
5. **Optimización**: el diseño de parámetros sigue las convenciones de StockSharp. Cada parámetro puede optimizarse o vincularse a controles de UI en Designer, facilitando el ajuste de períodos, sangría o valores de trailing.

## Archivos

- `CS/TunnelMethodStrategy.cs` – implementación principal de la estrategia.
- `README.md` – documentación en inglés (este archivo).
- `README_ru.md` – documentación en ruso.
- `README_zh.md` – documentación en chino.

La traducción a Python se omite intencionalmente, coincidiendo con la solicitud de entregar solo la versión de C# en esta etapa.
