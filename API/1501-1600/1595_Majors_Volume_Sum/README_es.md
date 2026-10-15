# Estrategia de Suma de Volumen de Principales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia suma el volumen con signo de las velas recientes y opera cuando la suma a corto plazo supera una fracción de su máximo histórico.

## Detalles

- **Criterios de entrada**:
  - La suma de volumen con signo de 10 períodos está por encima de `Threshold` × máximo y no hay posición: entrar largo.
  - La suma de volumen con signo de 10 períodos está por debajo de `-Threshold` × máximo y no hay posición: entrar corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal opuesta cierra la posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Threshold` = 0.75
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
