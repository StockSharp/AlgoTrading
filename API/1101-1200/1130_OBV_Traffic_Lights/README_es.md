# Estrategia OBV Traffic Lights
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza un On-Balance Volume basado en Heikin Ashi y tres EMA coloreadas como semáforos. Largo cuando OBV y la EMA rápida están por encima de la EMA lenta; corto cuando ambas están por debajo. Las posiciones se cierran cuando las condiciones desaparecen.

- **Criterios de entrada**: OBV > EMA lenta y EMA rápida > EMA lenta para largo; OBV < EMA lenta y EMA rápida < EMA lenta para corto.
- **Criterios de salida**: Señal opuesta o pérdida de acuerdo.
- **Indicadores**: OBV, EMA, Highest/Lowest
