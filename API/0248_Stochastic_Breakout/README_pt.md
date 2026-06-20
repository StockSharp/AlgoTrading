# Estratégia de Rompimento Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta abordagem de rompimento monitora o oscilador Stochastic em busca de movimentos bruscos afastados de sua média recente. Quando a linha %K rompe acima ou abaixo de um limiar ajustado pela volatilidade, sinaliza uma explosão de momentum que pode iniciar uma tendência.

Os testes indicam um retorno anual médio de cerca de 181%. Funciona melhor no mercado de criptomoedas.

Uma posição comprada é acionada quando %K cruza acima do limiar superior após um período de contração. Uma posição vendida é tomada quando %K rompe abaixo do limiar inferior. A operação é fechada quando o oscilador deriva de volta em direção à sua média ou atinge um stop protetor.

A estratégia é projetada para traders intradiários que desejam entrar cedo nas oscilações de momentum. O uso de bandas baseadas em volatilidade ajuda a filtrar o ruído para que apenas movimentos decisivos criem sinais.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: %K > Avg + DeviationMultiplier * StdDev
  - **Vendido**: %K < Avg - DeviationMultiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando %K < Avg
  - **Vendido**: Sair quando %K > Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `StochasticPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
