# Estrategia de Acumulación y Reducción Gradual de Posición
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia construye gradualmente una posición invirtiendo un porcentaje fijo del efectivo disponible en cada barra. Cuando el valor de la posición alcanza un nivel de beneficio configurable, vende una parte de la posición y opcionalmente reserva una parte del beneficio realizado.

## Detalles

- **Criterios de entrada**: Comprar siempre que haya efectivo disponible.
- **Criterios de salida**: Vender cuando el porcentaje de beneficio supera el umbral.
- **Largo/Corto**: Solo largos.
- **Valores predeterminados**:
  - `Buy Scaling Size %` = 2
  - `Take Profit Level %` = 50
  - `Take Profit Size %` = 1
  - `Retain Profit Portion %` = 50
  - `Minimum Position Value` = 200000
  - `Minimum Buy Value` = 100
- **Filtros**:
  - Categoría: Otro
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
