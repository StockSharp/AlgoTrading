# Estrategia de Reversión Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Hull Moving Average responde rápidamente a los cambios de precio manteniéndose suave. Un cambio en su dirección puede anticipar una reversión a corto plazo. Esta estrategia monitorea valores consecutivos de Hull MA y opera cuando la pendiente cambia.

Las pruebas indican una rentabilidad anual media de aproximadamente el 154%. Funciona mejor en el mercado de acciones.

Cuando la media móvil pasa de caer a subir, se abre una posición larga. Un cambio de subir a caer inicia una posición corta. El riesgo se controla mediante un stop basado en ATR colocado más allá de la vela reciente.

Las salidas dependen de ese stop de protección, capturando una porción del movimiento que sigue al cambio de momentum resaltado por la Hull MA.

## Detalles

- **Criterios de entrada**: La pendiente de la Hull MA cambia de dirección.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss.
- **Stops**: Sí, basado en ATR.
- **Valores predeterminados**:
  - `HmaPeriod` = 9
  - `AtrMultiplier` = 2 ATR
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Hull MA, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

