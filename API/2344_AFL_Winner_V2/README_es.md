# Estrategia AFL Winner V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia de ejemplo replica la lógica del indicador AFL Winner V2 utilizando la API de alto nivel de StockSharp. El indicador es aproximado por un oscilador estocástico y las señales se derivan de su posición relativa y niveles de umbral predefinidos.

## Lógica de la estrategia

- Usa un `StochasticOscillator` para emular el comportamiento de AFL Winner.
- Abre una posición larga cuando el oscilador indica fuerte impulso alcista.
- Abre una posición corta cuando el oscilador señala fuerte impulso bajista.
- Cierra largos cuando el estado de color cae por debajo de la zona neutral.
- Cierra cortos cuando el estado de color sube por encima de la zona neutral.
- Los parámetros permiten optimizar los períodos del oscilador y los niveles de umbral.

## Parámetros

| Parámetro   | Descripción                                         |
|-------------|-----------------------------------------------------|
| `KPeriod`   | Período %K del oscilador estocástico.               |
| `DPeriod`   | Período %D del oscilador estocástico.               |
| `HighLevel` | Umbral superior para señales alcistas.              |
| `LowLevel`  | Umbral inferior para señales bajistas.              |

## Archivos

- `CS/AflWinnerV2Strategy.cs` – implementación principal de la estrategia.

## Notas

La estrategia opera solo en velas completadas y usa protección automática de posiciones para evitar exposición no deseada.
