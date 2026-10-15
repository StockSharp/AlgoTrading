# Estrategia COSTAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia COSTAR construye una regresión lineal de los precios de cierre y mide la desviación estándar de los residuos. Las bandas superior e inferior se crean sumando y restando la desviación multiplicada por un factor. Las operaciones intentan operar en contra de las desviaciones extremas y salen cuando el precio regresa a la línea de regresión.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio cruza por encima de la banda inferior.
  - **Corto**: El precio cruza por debajo de la banda superior.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - El precio cruza de vuelta a través de la línea de regresión.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 100
  - `Multiplier` = 1
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Linear Regression, Standard Deviation
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
