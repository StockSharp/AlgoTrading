# Estratégia de Arbitragem Delta Neutral
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia de arbitragem opera o spread entre dois ativos correlacionados mantendo a posição combinada próxima de delta neutral. Ao equilibrar uma posição comprada em um ativo contra uma vendida em outro, tenta lucrar com a reversão à média do spread em vez da direção do mercado.

Os testes indicam um retorno anual médio de aproximadamente 43%. Funciona melhor no mercado de ações.

Um spread comprado é iniciado quando o z-score da diferença de preços cai abaixo de `-EntryThreshold`. O primeiro ativo é comprado e o segundo é vendido em igual tamanho. Um spread vendido faz o oposto quando o z-score sobe acima do limiar positivo. A operação é fechada quando o spread retorna à média móvel.

A operativa delta neutral é popular entre traders quantitativos que buscam exposição de baixa volatilidade. Embora esteja protegida, a proteção por stop-loss ainda é aplicada para se proteger contra divergência extrema entre os ativos.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Spread Z-Score < -EntryThreshold
  - **Vendido**: Spread Z-Score > EntryThreshold
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o spread volta a cruzar acima da média
  - **Vendido**: Sair quando o spread volta a cruzar abaixo da média
- **Stops**: Sim, stop-loss percentual sobre o valor do spread.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Arbitragem
  - Direção: Ambos
  - Indicadores: Spread statistics
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

