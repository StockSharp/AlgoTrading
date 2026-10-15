# Estratégia de Arbitragem Estatística de Spread
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera o spread entre dois instrumentos correlacionados. Uma posição comprada no primeiro ativo é aberta quando o spread cai abaixo de sua média por um múltiplo do desvio padrão do spread. A posição é fechada quando o spread retorna à média.

## Detalhes
- **Critérios de entrada**:
  - Comprado: Spread < Média - Multiplicador * DesvPad
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: Fechar quando spread > Média
- **Stops**: Não
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `StdMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Arbitrage
  - Direção: Somente comprado
  - Indicadores: Estatísticas do spread
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
