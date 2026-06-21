# Estrategia de Umbral UltraFATL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el oscilador UltraFATL para detectar cambios en la fuerza de la tendencia. El indicador genera niveles discretos del 0 al 8. Se abre una posición larga cuando el valor anterior supera el nivel 4 y el valor actual cae por debajo de 5 manteniéndose positivo. Se abre una posición corta cuando el valor anterior es inferior a 5 pero mayor que cero y el valor actual sube por encima de 4. El algoritmo trabaja con velas de 4 horas por defecto, pero el marco temporal puede ajustarse.

El enfoque espera la continuación de la tendencia tras un retroceso desde lecturas extremas de UltraFATL. Las posiciones se invierten cuando aparece la condición opuesta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `UltraFATL(prev) > 4` y `UltraFATL(curr) < 5` y `UltraFATL(curr) != 0`.
  - **Corto**: `UltraFATL(prev) < 5` y `UltraFATL(prev) != 0` y `UltraFATL(curr) > 4`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: La señal opuesta invierte la posición.
- **Stops**: No se usan por defecto.
- **Valores predeterminados**:
  - `Candle Type` = velas de 4 horas.
  - `Length` = 3.
  - `Signal Bar` = 1 (usar la barra anterior para señales).
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único (UltraFATL)
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
