# Patrón Estrella Fugaz (Shooting Star Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La vela estrella fugaz (Shooting Star) suele aparecer después de un avance y advierte de una reversión. Esta estrategia busca una sombra superior larga en relación con el cuerpo y poca sombra inferior.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 67%. Funciona mejor en el mercado de acciones.

Si se requiere confirmación, la siguiente vela debe cerrar más baja antes de entrar en corto. De lo contrario, la operación puede tomarse inmediatamente. Los stops se colocan por encima del máximo del patrón.

## Detalles

- **Criterios de entrada**: Estrella fugaz detectada y confirmación si está activada.
- **Largo/Corto**: Solo cortos.
- **Criterios de salida**: Stop-loss o salida discrecional.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `ShadowToBodyRatio` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
  - `ConfirmationRequired` = true
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Solo cortos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
