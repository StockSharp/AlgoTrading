# Estrella matutina/vespertina con estrategia de confirmación de IMF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica la lógica del MetaTrader experto `Expert_AMS_ES_MFI`, combinando patrones de reversión de múltiples velas con la confirmación del impulso del Índice de Flujo de Dinero (MFI). Supervisa las formaciones Morning Star y Evening Star de tres velas en el período de tiempo seleccionado y filtra las señales utilizando umbrales MFI para confirmar el agotamiento de la oscilación actual antes de iniciar operaciones. Las reversiones de impulso detectadas por los cruces de IMF también se utilizan para cerrar posiciones abiertas.

## Lógica de trading
- **Fuente de datos**: Velas terminadas del período de tiempo configurado y sus valores de MFI asociados.
- **Indicadores**:
  - Índice de flujo de dinero (IMF): el período es configurable (predeterminado 49).
- **Reglas de entrada**:
  - **Largo**: Detecta un patrón Morning Star (vela bajista fuerte, vela intermedia de cuerpo pequeño, vela alcista fuerte que cierra por encima del punto medio de la primera) y requiere que el MFI de la vela anterior esté por debajo del umbral de confirmación alcista (predeterminado 40).
  - **Corto**: Detecta un patrón Evening Star (vela alcista fuerte, vela intermedia de cuerpo pequeño, vela bajista fuerte que cierra por debajo del punto medio de la primera) y requiere que el MFI de la vela anterior esté por encima del umbral de confirmación bajista (predeterminado 60).
  - Al invertir posiciones, la estrategia primero cierra la exposición opuesta antes de abrir la nueva operación.
- **Reglas de salida**:
  - **Salida larga**: cierre la posición cuando la IMF cruce por encima del nivel de salida superior (predeterminado 70) o caiga por debajo del nivel de salida inferior (predeterminado 30), lo que indica un impulso de sobrecompra o una reversión fallida.
  - **Salida corta**: cierre la posición cuando la IMF cruce por encima del nivel de salida inferior (predeterminado 30) o por encima del nivel de salida superior (predeterminado 70), lo que indica un creciente impulso alcista.
- **Tipo de orden**: Órdenes de mercado utilizando el volumen de estrategia configurado en el entorno StockSharp.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Periodo de tiempo de las velas utilizadas para el análisis. | velas de 1 hora |
| `MfiPeriod` | Período del indicador IFM. | 49 |
| `BullishMfiThreshold` | Nivel de IMF que confirma las señales del Morning Star. | 40 |
| `BearishMfiThreshold` | Nivel de MFI que confirma las señales del Evening Star. | 60 |
| `UpperExitLevel` | Nivel de MFI utilizado para la detección de salidas de sobrecompra. | 70 |
| `LowerExitLevel` | Nivel de MFI utilizado para la detección de salida de sobreventa. | 30 |

Todos los parámetros se pueden optimizar dentro de StockSharp Designer/Optimizer.

## Notas de uso
1. Adjunte la estrategia a la seguridad deseada y configure el `CandleType` para que coincida con el período de tiempo del gráfico del experto MQL original.
2. Configure los parámetros de riesgo, como el volumen de la estrategia o el tamaño de la orden específica del corredor, a través de la plataforma StockSharp.
3. Habilite la estrategia. Se suscribirá automáticamente a velas, calculará los valores de MFI y gestionará las posiciones de acuerdo con las reglas anteriores.

## Origen
La estrategia es una conversión directa del MQL5 asesor experto ubicado en `MQL/323`, preservando su patrón y lógica de decisión basada en IMF mientras lo adapta al API de alto nivel de StockSharp.
