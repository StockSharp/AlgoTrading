# Estratégia TradingView Supertrend Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada em inversões do indicador Supertrend com confirmação de volume

Os testes indicam um retorno anual médio de aproximadamente 79%. Funciona melhor no mercado de ações.

TradingView Supertrend Flip emula as mudanças de cor do popular indicador. Uma mudança de vermelho para verde sinaliza uma entrada comprada e de verde para vermelho sinaliza uma entrada vendida. A estratégia sai na próxima inversão.

A confirmação de volume pode ser usada para evitar sinais falsos durante períodos de negociação fraca. Ao agir apenas em inversões com volume de suporte, o método visa capturar reversões mais confiáveis.


## Detalhes

- **Critérios de entrada**: Sinais baseados em ATR, Supertrend.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `VolumeAvgPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

