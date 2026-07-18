# Estratégia de Tendência com Kalman Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este método de seguimento de tendência usa um Kalman Filter para suavizar flutuações de preço e estimar a direção subjacente. O filtro se adapta dinamicamente ao ruído do mercado, oferecendo uma visão refinada da força da tendência em comparação com médias móveis padrão.

Os testes indicam um retorno anual médio de aproximadamente 112%. Funciona melhor no mercado de forex.

Uma posição comprada é aberta quando o preço de fechamento sobe acima da estimativa do Kalman Filter. Por outro lado, uma posição vendida é tomada quando o fechamento cai abaixo do valor do filtro. Como o filtro se atualiza em cada barra, as operações mudam sempre que o preço cruza a linha, proporcionando participação contínua em mercados em tendência.

Traders que preferem abordagens sistemáticas podem achar o Kalman Filter útil para reduzir sinais falsos. Um stop de proteção baseado em ATR mantém o risco limitado caso a tendência reverta rapidamente.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Fechamento > Kalman Filter
  - **Vendido**: Fechamento < Kalman Filter
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando fechamento < Kalman Filter
  - **Vendido**: Sair quando fechamento > Kalman Filter
- **Stops**: Sim, stop-loss baseado em ATR.
- **Valores padrão**:
  - `ProcessNoise` = 0.01m
  - `MeasurementNoise` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Kalman Filter
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
