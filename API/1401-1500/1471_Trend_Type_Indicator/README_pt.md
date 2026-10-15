# Estratégia de Indicador de Tipo de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Trend Type Indicator detecta o regime de mercado usando ATR e ADX.
Vai comprado durante tendências de alta, vendido durante tendências de baixa e sai quando as condições ficam laterais.

## Detalhes

- **Critérios de entrada**: +DI maior que -DI e sem movimento lateral
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Tendência oposta ou lateral
- **Stops**: Não
- **Valores padrão**:
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMaLength` = 20
  - `UseAdx` = true
  - `AdxLength` = 14
  - `AdxLimit` = 25
  - `SmoothFactor` = 3
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, ADX
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
