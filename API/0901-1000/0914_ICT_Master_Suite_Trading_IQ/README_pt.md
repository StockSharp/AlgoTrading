# Estratégia ICT Master Suite Trading IQ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia ICT Master Suite opera rompimentos da máxima e mínima da sessão diária. Quando o preço fecha acima da máxima da sessão, a estratégia entra em uma posição comprada; quando o preço fecha abaixo da mínima da sessão, entra em uma posição vendida. As posições são gerenciadas com um stop trailing baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - O preço fecha acima da máxima da sessão atual (comprado).
  - O preço fecha abaixo da mínima da sessão atual (vendido).
- **Comprado/Vendido**: Comprado e Vendido.
- **Critérios de saída**:
  - Stop trailing baseado em ATR.
- **Stops**: Stop trailing por ATR.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `AllowLong` = true
  - `AllowShort` = true
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
