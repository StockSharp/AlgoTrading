# Estrategia de Apertura y Cierre en Hora Exacta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia simple basada en tiempo que abre una posición de mercado a una hora específica del día y la cierra en otro momento predefinido. La dirección (compra o venta) y el volumen de la orden son configurables. Este ejemplo demuestra la ejecución programada de operaciones sin usar indicadores ni filtros adicionales.

## Detalles

- **Criterios de entrada**:
  - **Largo**: En `Open Time` cuando `Is Buy` está habilitado.
  - **Corto**: En `Open Time` cuando `Is Buy` está deshabilitado.
- **Largo/Corto**: Ambos, dependiendo de `Is Buy`.
- **Criterios de salida**:
  - La posición se cierra en `Close Time` independientemente del beneficio o la pérdida.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Open Time` = 13:00.
  - `Close Time` = 13:01.
  - `Volume` = 1.
  - `Is Buy` = true.
  - `Candle Type` = 1 minuto.
- **Filtros**:
  - Categoría: Tiempo
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
