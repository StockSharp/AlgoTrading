# Estrategia de Alerta de Señal del Oscilador Aroon
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el Oscilador Aroon para generar señales de trading cuando el oscilador cruza niveles predefinidos. Se abre una posición larga cuando el oscilador cruza hacia arriba el nivel bajo (por defecto -50). Se abre una posición corta cuando cruza hacia abajo el nivel alto (por defecto +50). Las señales opuestas cierran o revierten la posición.

## Detalles

- **Criterios de entrada:**
  - **Largo**: El oscilador Aroon cruza hacia arriba el nivel bajo.
  - **Corto**: El oscilador Aroon cruza hacia abajo el nivel alto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal inversa cierra o revierte automáticamente la posición actual.
- **Stops**: Ninguno.
- **Filtros**: Ninguno.
- **Marco temporal**: Velas de 4 horas por defecto (configurable).

## Parámetros

- `AroonPeriod` – período de lookback para el oscilador Aroon (por defecto 9).
- `UpLevel` – umbral superior para señales de venta (por defecto +50).
- `DownLevel` – umbral inferior para señales de compra (por defecto -50).
- `CandleType` – marco temporal de velas para los cálculos (por defecto 4 horas).
