# Estrategia de Modelo de Banda de Volatilidad Dinámica Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza bandas de volatilidad estilo Bollinger para operar cruces y salir tras un número fijo de velas.

## Detalles

- **Criterios de entrada**: largo cuando el precio cruza por encima de la banda inferior; corto cuando el precio cruza por debajo de la banda superior
- **Largo/Corto**: Ambos
- **Criterios de salida**: posición cerrada después de `ExitBars` velas
- **Stops**: No
- **Valores predeterminados**:
  - `Length` = 5
  - `Multiplier` = 1.67
  - `ExitBars` = 7
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: BollingerBands
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
