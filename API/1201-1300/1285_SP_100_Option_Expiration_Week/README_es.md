# Estrategia de la Semana de Vencimiento de Opciones S&P 100
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra al comienzo de la semana de vencimiento de opciones (la semana que contiene el tercer viernes del mes) y cierra la posición en ese tercer viernes.

## Detalles

- **Criterios de entrada**:
  - **Largo**: abrir una posición larga el lunes de la semana de vencimiento de opciones.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - cerrar la posición larga el tercer viernes del mes.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
