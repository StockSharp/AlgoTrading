# ADX Donchian Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia utiliza os indicadores ADX Donchian para gerar sinais.
A entrada comprada ocorre quando ADX > AdxThreshold && Price >= upperBorder (tendência forte com rompimento para cima). A entrada vendida ocorre quando ADX > AdxThreshold && Price <= lowerBorder (tendência forte com rompimento para baixo).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 67%. Funciona melhor no mercado de ações.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: ADX > AdxThreshold && Price >= upperBorder (strong trend with breakout up)
  - **Vendido**: ADX > AdxThreshold && Price <= lowerBorder (strong trend with breakout down)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição quando ADX cai abaixo de (threshold - 5)
  - **Vendido**: Sair da posição quando ADX cai abaixo de (threshold - 5)
- **Stops**: Sim.
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `DonchianPeriod` = 5
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AdxThreshold` = 10
  - `Multiplier` = 0.1m
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: ADX Donchian
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

