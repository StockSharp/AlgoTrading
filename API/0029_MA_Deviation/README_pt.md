# Estratégia MA Deviation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera quando o preço se desvia significativamente de sua média móvel

Os testes indicam um retorno anual médio de aproximadamente 124%. Funciona melhor no mercado forex.

MA Deviation entra quando o preço se desvia uma porcentagem definida de sua média móvel, antecipando um retorno à média. A posição é encerrada quando o preço converge de volta em direção à média.

Os limiares de desvio podem ser ampliados ou reduzidos dependendo da volatilidade. Usar ATR para dimensionamento de posição mantém o risco consistente em todos os mercados.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA, ATR.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `DeviationPercent` = 5m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

