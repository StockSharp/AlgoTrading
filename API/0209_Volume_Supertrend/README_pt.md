# Volume Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia utiliza os indicadores Volume Supertrend para gerar sinais.
A entrada comprada ocorre quando Volume > Avg(Volume) && Price > Supertrend (surto de volume com tendência de alta). A entrada vendida ocorre quando Volume > Avg(Volume) && Price < Supertrend (surto de volume com tendência de baixa).
É adequada para traders que buscam oportunidades em mercados de tendência.

Os testes indicam um retorno anual médio de aproximadamente 64%. Funciona melhor no mercado de câmbio.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend)
  - **Vendido**: Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando Supertrend vira para baixo
  - **Vendido**: Sair da posição vendida quando Supertrend vira para cima
- **Stops**: Sim.
- **Valores padrão**:
  - `VolumeAvgPeriod` = 20
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Volume Supertrend
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

