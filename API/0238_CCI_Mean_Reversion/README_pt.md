# Estratégia de Reversão à Média com CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Commodity Channel Index (CCI) mede o quanto o preço se afasta de sua média estatística. Esta estratégia entra quando o CCI se desvia de sua própria média por uma grande margem, esperando um retorno rápido assim que o momentum diminui.

Os testes indicam um retorno anual médio de aproximadamente 151%. Funciona melhor no mercado de ações.

Uma operação comprada ocorre quando o CCI cai abaixo da média menos `DeviationMultiplier` vezes o desvio padrão. Uma operação vendida é aberta quando o CCI sobe acima da média mais esse multiplicador. A posição é fechada quando o CCI cruza de volta pelo valor médio.

Este sistema é adequado para traders de curto prazo que preferem configurações contrárias. Um stop-loss baseado em movimento percentual ajuda a limitar o risco se o mercado não conseguir reverter rapidamente.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: CCI < Avg - DeviationMultiplier * StdDev
  - **Vendido**: CCI > Avg + DeviationMultiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando CCI > Avg
  - **Vendido**: Sair quando CCI < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `CciPeriod` = 20
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

