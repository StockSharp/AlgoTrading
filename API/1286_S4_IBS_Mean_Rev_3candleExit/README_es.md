# Estrategia S4 IBS de Reversión a la Media con Salida en 3 Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compra cuando la Fuerza Interna de la Barra (IBS) de la vela anterior está por debajo de un umbral, esperando una reversión a la media. Sale cuando el precio cierra por encima de la entrada o después de tres velas si la operación sigue siendo perdedora.

## Detalles

- **Criterios de entrada**: IBS anterior <= umbral
- **Largo/Corto**: Solo largos
- **Criterios de salida**: cierre por encima del precio de entrada o después de 3 velas si sigue por debajo de la entrada; salida forzada al final del período
- **Stops**: No
- **Valores predeterminados**:
  - `IbsThreshold` = 0.25
  - `StartTime` = 2024-01-01 05:00:00 UTC
  - `EndTime` = 2024-12-31 00:00:00 UTC
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
