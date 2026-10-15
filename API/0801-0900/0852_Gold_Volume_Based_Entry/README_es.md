# Estrategia de Entrada del Oro Basada en Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra cuando dos barras de volumen alcistas consecutivas superan la media móvil de volumen. La segunda barra también debe tener mayor volumen que la primera. Un objetivo de ganancia fijo cierra la posición una vez que el precio se mueve una cantidad predefinida a favor.

## Detalles

- **Criterios de entrada**:
  - Dos barras de volumen alcistas por encima de la media móvil de volumen con volumen creciente.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Objetivo de ganancia fijo en `entry price + Target Move`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Volume MA Period` = 20.
  - `Target Move` = 5.
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Largo
  - Indicadores: Único
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
