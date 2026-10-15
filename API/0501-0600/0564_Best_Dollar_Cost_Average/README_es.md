# Estrategia de Promedio de Costo en Dólares Óptimo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia acumula una posición invirtiendo una cantidad fija de capital a intervalos regulares entre fechas de inicio y fin definidas por el usuario. Cada compra se realiza al precio de cierre del marco temporal seleccionado independientemente del precio, implementando un enfoque clásico de promedio de costo en dólares.

## Detalles

- **Criterios de entrada**:
  - En cada intervalo (diario, semanal o mensual) entre las fechas de inicio y fin, la
    estrategia compra al precio de cierre por la cantidad configurada.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Las posiciones se mantienen; no se incluye lógica de salida automática.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - Monto por período = 100.
  - Intervalo = Semanal.
  - Fecha de inicio = 2018-01-01, Fecha de fin = 2020-01-28.
- **Filtros**:
  - Categoría: Acumulación.
  - Dirección: Largo.
  - Indicadores: Ninguno.
  - Stops: No.
  - Complejidad: Bajo.
  - Marco temporal: Cualquiera.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Bajo.
