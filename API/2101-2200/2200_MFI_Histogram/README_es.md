# Estrategia de Histograma MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Histograma MFI usa el Índice de Flujo de Dinero (MFI) para detectar condiciones de sobrecompra y sobreventa mediante umbrales configurables. El MFI combina precio y volumen para medir la intensidad de la entrada y salida de capital. Cuando el indicador cruza hacia arriba el nivel alto desde abajo, la estrategia interpreta esto como un aumento de la presión compradora y abre una posición larga mientras cierra cualquier posición corta existente. Por el contrario, un cruce hacia abajo del nivel bajo desencadena una entrada corta y cierra las posiciones largas existentes. Los valores de stop-loss y take-profit se gestionan en ticks mediante el mecanismo de protección integrado.

La estrategia opera en un marco temporal de velas definido por el usuario (4 horas por defecto) y se basa en un único indicador sin filtros adicionales. Los parámetros permiten optimizar el período del MFI, los niveles umbrales y los límites de riesgo, haciendo el sistema adaptable a varios mercados y regímenes de volatilidad.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `MFI` cruza hacia arriba `HighLevel` desde abajo.
  - **Corto**: `MFI` cruza hacia abajo `LowLevel` desde arriba.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal opuesta genera una reversión.
  - Se alcanza el stop-loss o take-profit.
- **Stops**: `StopLoss` y `TakeProfit` en ticks.
- **Valores predeterminados**:
  - `MFI Period` = 14
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `Candle Type` = 4-hour
  - `StopLoss` = 1000 ticks
  - `TakeProfit` = 2000 ticks
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
