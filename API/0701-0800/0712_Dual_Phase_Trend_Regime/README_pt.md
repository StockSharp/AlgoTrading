# Estratégia de Regime de Tendência de Fase Dupla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Regime de Tendência de Fase Dupla alterna entre osciladores de tendência lento e rápido com base na volatilidade atual. A volatilidade é derivada do desvio padrão dos retornos e dividida em regimes baixo ou alto. As inclinações de regressão linear determinam a direção da tendência. As entradas podem ser realizadas em mudanças de regime ou cruzamentos de osciladores.

## Parâmetros
- Tipo de vela
- Direção de negociação
- Fonte de sinal
- Comprimento do oscilador lento
- Comprimento do oscilador rápido
- Intervalo de recálculo de volatilidade
- Período de volatilidade atual
- Comprimento de suavização de volatilidade
