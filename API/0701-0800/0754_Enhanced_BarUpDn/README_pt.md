# Estratégia BarUpDn Aprimorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia busca barras de alta ou baixa combinadas com Bollinger Bands e confirmação de tendência. Ela entra comprada em gaps de alta durante tendências de alta e vendida em gaps de baixa durante tendências de baixa. As saídas utilizam níveis de stop-loss e take-profit baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: vela de alta com gap para cima, fechamento acima da MA de tendência e acima da banda inferior do Bollinger.
  - Vendido: vela de baixa com gap para baixo, fechamento abaixo da MA de tendência e abaixo da banda superior do Bollinger.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O preço toca o stop-loss ou take-profit baseado em ATR (1,5× ATR).
- **Stops**: Stop e take-profit baseados em ATR.
- **Valores padrão**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `MaLength` = 50
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 2
  - `AtrMultiplierTp` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, SMA, ATR
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
