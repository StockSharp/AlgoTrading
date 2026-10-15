# Estrategia de IMF AH HM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

La estrategia AH HM MFI negocia patrones de velas de martillo y hombre colgado que son confirmados por el Índice de flujo de dinero (IMF). Cuando aparece un martillo alcista en una tendencia bajista a corto plazo y la IMF se mantiene por debajo del umbral de sobreventa, la estrategia abre una posición larga. Cuando se forma un hombre colgado bajista en una tendencia alcista mientras el MFI está por encima de un umbral de sobrecompra, abre una posición corta. Las salidas protectoras se activan cuando la IMF cruza límites superiores o inferiores predefinidos.

## Lógica principal

1. Suscríbase a las velas de marco temporal configuradas y calcule dos indicadores:
   - **Índice de flujo de dinero** con un período configurable (predeterminado: 47).
   - **Promedio móvil simple** de precios de cierre para aproximar el filtro de tendencia de la estrategia MQL original (longitud predeterminada: 5).
2. Detecta patrones de **martillo** y **hombre colgado**:
   - Cuerpo de vela situado en el tercio superior de la gama.
   - Sombra inferior larga en relación con el cuerpo real.
   - Brecha en la dirección de la tendencia en comparación con la vela anterior.
   - Confirmación de tendencia utilizando el punto medio de la vela anterior frente a la media móvil.
3. Confirmar entradas con umbrales de IMF:
   - Ingrese largo si se detecta un martillo y la MFI está en o por debajo del nivel de sobreventa configurado (predeterminado: 40).
   - Ingrese en corto si se detecta un ahorcado y la MFI está en o por encima del nivel de sobrecompra configurado (predeterminado: 60).
4. Gestione las salidas mediante los cruces de las IMF:
   - Cierre las posiciones cortas cuando la IMF cruce hacia arriba por encima de los niveles de salida inferior o superior (valores predeterminados: 30 y 70).
   - Cierre las posiciones largas cuando la IMF cruce hacia arriba por encima del nivel de salida superior o hacia abajo por debajo del nivel de salida inferior.
5. Inicie el módulo de protección de riesgos incorporado para manejar paradas de emergencia.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de datos de vela y período de tiempo utilizados para la detección de patrones. | plazo de 30 minutos |
| `MfiPeriod` | Período retrospectivo para el cálculo de la IMF. | 47 |
| `MaPeriod` | Longitud del SMA aplicado a los precios de cierre para confirmación de tendencia. | 5 |
| `HammerEntryThreshold` | Valor máximo de MFI permitido antes de entrar en una señal de martillo. | 40 |
| `HangingEntryThreshold` | Valor mínimo de MFI requerido antes de entrar en una señal de ahorcado. | 60 |
| `MfiUpperExitLevel` | Límite superior de las IMF; cruzar por encima cierra cualquier posición abierta. | 70 |
| `MfiLowerExitLevel` | Límite inferior de las IMF; cruzar por debajo cierra posiciones largas, mientras que cruzar por encima cierra posiciones cortas. | 30 |

## Notas

- La estrategia evalúa solo velas terminadas para evitar actuar sobre información incompleta.
- La detección del martillo y del ahorcado es conservadora: se requiere tanto una larga sombra inferior como un cuerpo situado cerca de la vela alta.
- La media móvil reemplaza el filtro MetaTrader 5 `CloseAvg` del asesor experto original, lo que garantiza que las entradas se alineen con la tendencia más amplia.
