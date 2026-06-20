# Estratégia de Reversão à Média OBV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O On Balance Volume (OBV) rastreia o fluxo acumulativo de volume para determinar se compradores ou vendedores são dominantes. Esta estratégia aguarda o OBV divergir acentuadamente de sua média e então opera antecipando um retorno aos níveis típicos.

Os testes indicam um retorno anual médio de cerca de 79%. Funciona melhor no mercado de ações.

Um sinal de compra ocorre quando o OBV cai abaixo de sua média menos `Multiplier` vezes o desvio padrão e o preço está abaixo da média móvel. Um sinal de venda é gerado quando o OBV sobe acima da banda superior com o preço acima da média. As posições são fechadas quando o OBV cruza de volta pela sua linha média.

A abordagem é útil para traders que consideram os fluxos de volume além da ação do preço. Os stops são colocados a uma porcentagem definida para lidar com situações onde o volume continua acelerando.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: OBV < Avg - Multiplier * StdDev && Close < MA
  - **Vendido**: OBV > Avg + Multiplier * StdDev && Close > MA
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando OBV > Avg
  - **Vendido**: Sair quando OBV < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: OBV
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
