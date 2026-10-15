# Estrategia de Bollinger Bands con DEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina las Bollinger Bands calculadas en velas de 30 minutos con una Media Móvil Exponencial Doble (DEMA) de datos diarios para operar rupturas con confirmación de tendencia.

Una configuración larga ocurre cuando una vela alcista cruza por encima de la banda inferior mientras la DEMA sube, confirmando el impulso alcista. Una configuración corta ocurre cuando una vela bajista cruza por debajo de la banda superior mientras la DEMA baja. Las posiciones se cierran cuando una vela de color opuesto cruza la banda exterior en contra de la operación.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La vela cierra por encima de la banda inferior y abre por debajo de ella Y la DEMA diaria está aumentando durante tres días consecutivos.
  - **Corto**: La vela cierra por debajo de la banda superior y abre por encima de ella Y la DEMA diaria está disminuyendo durante tres días consecutivos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Largo**: Una vela bajista cierra por debajo de la banda superior después de abrir por encima de ella.
  - **Corto**: Una vela alcista cierra por encima de la banda inferior después de abrir por debajo de ella.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `DemaPeriod` = 20
  - `Deviation` = 2
  - `CandleType` = Marco temporal de 30 minutos
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, DEMA
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Intradía con filtro de tendencia diaria
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
