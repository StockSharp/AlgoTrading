# Estratégia Fisher Org v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o indicador Fisher Transform para capturar reversões de tendência. Uma posição comprada é aberta quando o indicador forma um mínimo local, enquanto uma posição vendida é aberta quando aparece um máximo local. Sinais opostos fecham qualquer posição existente.

## Regras
- **Comprado**: `Fisher[t-2] > Fisher[t-1]` e `Fisher[t-1] <= Fisher[t]`
- **Vendido**: `Fisher[t-2] < Fisher[t-1]` e `Fisher[t-1] >= Fisher[t]`

## Parâmetros
- `Fisher Length` – período do Fisher Transform (padrão 7)
- `Candle Type` – período dos candles utilizados para os cálculos

## Indicadores
- Fisher Transform
