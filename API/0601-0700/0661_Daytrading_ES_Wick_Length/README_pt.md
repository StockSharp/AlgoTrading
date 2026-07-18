# Estratégia de Daytrading ES por Comprimento de Pavio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em uma posição comprada quando o comprimento total do pavio de um candle supera sua média móvel mais um deslocamento, e sai após manter a posição por um número fixo de barras.

## Detalhes

- **Critérios de entrada**: Comprimento total do pavio maior que a média móvel com deslocamento.
- **Critérios de saída**: Posição fechada após manter `Hold periods` barras.
- **Comprado/Vendido**: Somente comprado.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = velas de 1 minuto
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Comprado
  - Indicadores: Moving Average, comprimento de pavio
  - Stops: Não
  - Complexidade: Simples
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
