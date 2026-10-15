# Modelo de Momentum Ponderado por Gamma para Futuros BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia calcula un precio promedio ponderado por Gamma (GWAP) para capturar el momentum en futuros de BTC. Se abren posiciones largas cuando el precio permanece por encima del GWAP y los tres últimos cierres suben consecutivamente. Se toman posiciones cortas cuando el precio está por debajo del GWAP y los tres últimos cierres caen consecutivamente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre por encima del GWAP y los tres últimos cierres en ascenso.
  - **Corto**: Cierre por debajo del GWAP y los tres últimos cierres en descenso.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 60
  - `GammaFactor` = 0.75
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: GWAP
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: 1m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
