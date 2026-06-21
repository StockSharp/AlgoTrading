# Estrategia de Cruce MA por MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un cruce de media móvil con doble suavizado.
La serie de precios se suaviza mediante una media móvil exponencial (EMA) rápida.
El resultado de la EMA rápida se suaviza de nuevo con una EMA más lenta.
Las dos series se comparan para generar señales:
- Se abre una posición larga cuando la EMA rápida cruza por encima de la EMA lenta.
- Se abre una posición corta cuando la EMA rápida cruza por debajo de la EMA lenta.
Cualquier posición opuesta existente se cierra en el cruce.

La estrategia funciona en cualquier marco temporal de velas.

## Parámetros
- `FastLength` – período de la EMA rápida.
- `SlowLength` – período de la EMA lenta aplicada a la salida de la EMA rápida.
- `EnableLong` – permitir apertura de posiciones largas.
- `EnableShort` – permitir apertura de posiciones cortas.
- `CandleType` – tipo de velas usadas para los cálculos.

## Detalles
- **Criterios de entrada**:
  - **Largo**: la EMA rápida cruza por encima de la EMA lenta.
  - **Corto**: la EMA rápida cruza por debajo de la EMA lenta.
- **Largo/Corto**: Ambas direcciones soportadas.
- **Criterios de salida**:
  - El cruce opuesto cierra una posición existente.
- **Stops**: No se utiliza stop-loss ni take-profit explícito.
- **Valores predeterminados**:
  - `FastLength` = 7
  - `SlowLength` = 7
  - `EnableLong` = true
  - `EnableShort` = true
  - `CandleType` = marco temporal de 12 horas
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Moving averages
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
