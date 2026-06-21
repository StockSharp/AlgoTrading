# Estratégia de Inclinação Personalizada TEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de reversão que usa mudanças de inclinação de uma Triple Exponential Moving Average (TEMA). O indicador é calculado no período especificado e a estratégia reage a mudanças de direção.

## Como funciona

- **Critérios de entrada**:
  - **Comprado**: TEMA estava caindo e se vira para cima.
  - **Vendido**: TEMA estava subindo e se vira para baixo.
- **Critérios de saída**: sinal reverso fecha a posição existente.
- **Indicadores**: Triple Exponential Moving Average.

## Parâmetros-chave

- `TemaLength` – Número de barras para o cálculo do TEMA.
- `CandleType` – Período das velas usadas para análise.
