# Estrategia de Estacionalidad Intradía de Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que toma posiciones largas en Bitcoin durante horas intradía predefinidas de alta actividad.

Las pruebas indican un rendimiento anual promedio de aproximadamente 45%. Funciona mejor en el mercado de criptomonedas.

El sistema monitorea velas horarias. Durante las horas UTC seleccionadas mantiene una posición larga dimensionada al valor del portafolio. Fuera de esas horas sale a efectivo. Se omiten las órdenes inferiores a un valor mínimo en USD.

## Detalles

- **Criterios de entrada**: Mantener BTC largo durante las horas UTC especificadas.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Salir fuera de las horas especificadas.
- **Stops**: No.
- **Valores predeterminados**:
  - `HoursLong` = [0, 1, 2, 3]
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1h)
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
