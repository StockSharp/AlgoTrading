# Estratégia OBV Traffic Lights
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza um On-Balance Volume baseado em Heikin Ashi e três EMAs coloridas como semáforos. Comprado quando OBV e a EMA rápida estão acima da EMA lenta; vendido quando ambas estão abaixo. As posições são fechadas quando as condições desaparecem.

- **Critérios de entrada**: OBV > EMA lenta e EMA rápida > EMA lenta para comprado; OBV < EMA lenta e EMA rápida < EMA lenta para vendido.
- **Critérios de saída**: Sinal oposto ou perda de concordância.
- **Indicadores**: OBV, EMA, Highest/Lowest
