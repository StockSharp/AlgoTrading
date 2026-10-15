# Estrategia GLFX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MetaTrader 4 asesor experto **GLFX** reescrito para el alto nivel de StockSharp API. El puerto conserva la idea original de combinar confirmaciones con plazos más altos con controles estrictos de administración de dinero, al tiempo que elimina la colección masiva de filtros raramente utilizados que dependían de indicadores externos.

## Lógica comercial

1. La estrategia funciona en un período de tiempo principal (predeterminado **M15**) y, opcionalmente, crea un período de tiempo de confirmación subiendo por la clásica escalera MetaTrader (`M15 → M30 → H1 → H4 → D1 → W1 → MN`).
2. Un período de tiempo más alto **RSI** (período predeterminado 57) rastrea si el impulso aumenta o disminuye. Aparece una confirmación de compra cuando RSI sube pero permanece por debajo del límite de sobrecompra configurado. Una confirmación de venta requiere que RSI baje mientras se mantiene por encima del piso de sobreventa.
3. Un **promedio móvil simple** de marco temporal más alto (período predeterminado 60) detecta si el precio se está alejando de la media. Una confirmación alcista necesita que el MA suba mientras se mantiene por encima del cierre actual (el precio regresa a una tendencia alcista). Una confirmación bajista refleja esta lógica.
4. Cada filtro habilitado contribuye `+1` para el sentimiento alcista o `-1` para el sentimiento bajista. El total debe alcanzar el número de filtros activos para contar como una señal válida. Los contadores recuerdan cuántas señales consecutivas de potencia completa aparecieron (`SignalsRepeat`). Si la fuerza combinada cae por debajo del umbral y `SignalsReset` está habilitado, los contadores se reinician.
5. Cuando la estrategia es plana y los interruptores de entrada larga/corta lo permiten, el siguiente contador completado activa una orden de mercado con el `Volume` configurado. Los niveles estáticos de stop-loss y take-profit se convierten de pips en compensaciones de precios utilizando el tamaño del tick del instrumento.
6. Si una posición ya está abierta, fuertes señales opuestas pueden cerrarla antes (`AllowLongExit` / `AllowShortExit`). De lo contrario, las salidas dependen de la parada o el destino administrado por `StartProtection()`.

El puerto **no** reproduce el Quantum opcional, el sentimiento de Twitter, la correlación de barra abierta, las pruebas de conjuntos o las escaleras avanzadas de administración de dinero del EA original. Esos módulos requerían indicadores personalizados adicionales o estados de intermediario que no existen en StockSharp.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | M15 | Plazo de trabajo para la evaluación de precios. |
| `HigherTimeFrameShift` | 1 | Número de pasos de MT4 utilizados para crear el plazo de confirmación. `0` mantiene el período de tiempo actual. |
| `UseRsiSignal` | cierto | Habilite la confirmación de plazo mayor RSI. |
| `RsiPeriod` | 57 | Periodo de la confirmación RSI. |
| `RsiUpperThreshold` | 65 | Deshabilite nuevos longs una vez que RSI supere este valor. |
| `RsiLowerThreshold` | 25 | Deshabilite nuevos cortos una vez que RSI caiga por debajo de este valor. |
| `UseMaSignal` | cierto | Habilite la confirmación de promedio móvil de períodos de tiempo más altos. |
| `MaPeriod` | 60 | Período de la media móvil de confirmación. |
| `SignalsRepeat` | 1 | Número de señales consecutivas de máxima potencia necesarias antes de abrir una operación. |
| `SignalsReset` | cierto | Reinicie los contadores cuando la señal combinada pierda impulso. |
| `TakeProfitPips` | 308 | Distancia de toma de ganancias expresada en pips. Establezca en `0` para desactivar. |
| `StopLossPips` | 290 | Distancia de stop-loss expresada en pips. Establezca en `0` para desactivar. |
| `Volume` | 0.1 | Tamaño de pedido utilizado para nuevas posiciones (lotes). |
| `AllowLongEntry` / `AllowShortEntry` | cierto | Cambios de permiso para abrir operaciones largas o cortas. |
| `AllowLongExit` / `AllowShortExit` | cierto | Permitir el cierre automático de la exposición existente en señales opuestas. |

## Notas de uso

- Elija instrumentos con un tamaño de tick confiable para que la conversión de pips siga siendo precisa. Los pares de Forex con tres o cinco decimales se asignan automáticamente a MetaTrader "puntos" multiplicando el paso del precio por diez.
- Establezca `HigherTimeFrameShift` en `0` si desea ejecutar todo en el mismo período de tiempo. En este caso, los indicadores se alimentan del flujo de velas principal para evitar suscripciones duplicadas.
- Si necesita el comportamiento heredado de mantener las operaciones abiertas independientemente de las señales opuestas, desactive el indicador `Allow*Exit` correspondiente.
- Se omitieron intencionalmente la escala de administración de dinero (configuraciones `MMC_*`), los módulos de seguimiento y los filtros de salida exóticos del script original. Implementarlos encima de este núcleo limpio si es necesario.

## Diferencias con el EA original

| Grupo de funciones | MetaTrader EA | StockSharp puerto |
|---------------|---------------|-----------------|
| Filtros de confirmación | RSI, MA, Quantum opcional, TSI, correlación multidivisa | RSI y solo MA (comportamiento principal) |
| Puerta de entrada | Repetición de señal más filtros temporales. | Repetición de señal más reinicio opcional |
| control de riesgos | TP/SL estático con gran biblioteca de módulos finales | TP/SL estático vía `StartProtection()` |
| gestión del dinero | Escalamiento incremental de lotes y escalas de pérdidas | Parámetro de volumen fijo |
| Dependencias externas | Indicadores personalizados (`Quantum`, `TSI`, carga de conjuntos basada en archivos) | Ninguno |

El resultado es una estrategia compacta y fácil de mantener que mantiene el comportamiento reconocible de GLFX (esperando la confirmación de la tendencia en un gráfico más lento y entrando solo después de un acuerdo repetido) y al mismo tiempo es fácil de ampliar utilizando el marco StockSharp.
