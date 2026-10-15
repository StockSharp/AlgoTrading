# Estrategia SPY TLT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compra el valor principal cuando el precio de TLT cruza por encima de su SMA y sale cuando TLT cierra por debajo de la SMA. Las operaciones solo se permiten dentro de la ventana de tiempo especificada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: TLT cierra por encima de su SMA dentro de la ventana de tiempo.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - TLT cierra por debajo de su SMA.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Start Time` = 2014-01-01
  - `End Time` = 2099-01-01
  - `TLT Symbol` = TLT
  - `SMA Length` = 20
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
