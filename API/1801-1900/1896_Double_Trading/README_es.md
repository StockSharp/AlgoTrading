# Estrategia de Doble Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de trading de pares que abre posiciones opuestas en dos instrumentos correlacionados y las cierra cuando el beneficio combinado alcanza un objetivo.

## Detalles

- **Criterios de entrada**: abrir simultáneamente la primera y la segunda posición en direcciones opuestas
- **Largo/Corto**: Largo y Corto
- **Criterios de salida**: beneficio combinado >= ProfitTarget
- **Stops**: No
- **Valores predeterminados**:
  - `Volume1` = 1
  - `Volume2` = 1.3
  - `ProfitTarget` = 20
  - `SecondSecurity` = requerido
- **Filtros**:
  - Categoría: Trading de pares
  - Dirección: Cubierto
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
