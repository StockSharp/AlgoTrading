# Estratégia de Linha de Gatilho (Trigger Line)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Trigger Line combina uma linha de tendência ponderada com uma média móvel de mínimos quadrados (LSMA). Uma posição comprada é aberta quando a linha de tendência ponderada cruza acima da LSMA, enquanto uma posição vendida é aberta quando cruza abaixo.

## Como funciona
- **Entrada comprada**: a linha de tendência ponderada cruza acima da LSMA.
- **Saída comprada**: a linha de tendência ponderada cruza abaixo da LSMA.
- **Entrada vendida**: a linha de tendência ponderada cruza abaixo da LSMA.
- **Saída vendida**: a linha de tendência ponderada cruza acima da LSMA.
- **Indicadores**: Média Móvel Ponderada, Regressão Linear (LSMA).

## Parâmetros
- **WT Period** – período de retrocesso para a linha de tendência ponderada.
- **LSMA Period** – período de suavização para a LSMA.
- **Candle Type** – período das velas utilizadas nos cálculos.
