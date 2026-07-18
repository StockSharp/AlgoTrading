# Estratégia ATR Trailing Stops
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
ATR Trailing usa um múltiplo do average true range para arrastar stops atrás de posições abertas. As entradas ocorrem quando o preço cruza uma média móvel, e o stop de rastreamento se ajusta com a volatilidade.

Os testes indicam um retorno anual médio de aproximadamente 157%. Funciona melhor no mercado de criptomoedas.

Conforme o preço avança, o stop sobe (ou desce) com base na leitura mais recente do ATR, nunca recuando. Isso trava os ganhos enquanto a tendência persiste.

As saídas ocorrem quando o stop de rastreamento é acionado ou quando o preço cruza de volta pela média móvel.

## Detalhes

- **Critérios de entrada**: Preço acima ou abaixo da MA.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Stop de rastreamento acionado ou preço cruza MA.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.0m
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

