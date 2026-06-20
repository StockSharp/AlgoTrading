# Estratégia de Rompimento Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca explosões de momentum observando Williams %R em relação à sua média histórica. Quando o oscilador se afasta muito além das leituras típicas, pode sinalizar o início de um movimento forte.

Os testes indicam um retorno anual médio de cerca de 91%. Funciona melhor no mercado de ações.

Uma posição comprada é aberta quando %R sobe acima da média mais `Multiplier` vezes um desvio padrão estimado. Uma posição vendida é tomada quando %R cai abaixo da média menos o mesmo multiplicador. A operação é fechada assim que %R retorna em direção à sua média ou um stop-loss é atingido.

A abordagem é voltada para traders de rompimento que desejam participar cedo em tendências emergentes. O risco da posição é gerenciado com um stop percentual baseado no preço de entrada.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: %R > Avg + Multiplier * StdDev
  - **Vendido**: %R < Avg - Multiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando %R < Avg
  - **Vendido**: Sair quando %R > Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `WilliamsRPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Williams %R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
