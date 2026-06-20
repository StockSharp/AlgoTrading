# Bollinger Percent B Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Este enfoque opera contra los extremos de precio más allá de las Bollinger Bands usando el indicador Percent B. Los movimientos por encima de la banda superior o por debajo de la banda inferior sugieren sobreextensión.

Las pruebas indican un rendimiento anual promedio de aproximadamente 142%. Funciona mejor en el mercado de acciones.

Cuando Percent B es menor que cero o mayor que uno, el sistema apuesta por un retorno al centro de la banda. Un umbral de salida cierra las operaciones una vez que el momentum se normaliza.

Los stops se colocan a un porcentaje fijo desde la entrada.

## Detalles

- **Criterios de entrada**: Percent B fuera del rango 0–1.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Percent B cruza `ExitValue` o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `ExitValue` = 0.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

