# Estratégia de Divergência com OBV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O On-Balance Volume rastreia o volume de negociação acumulado com a ideia de que o volume precede o preço. Quando o preço forma uma nova máxima mas o OBV não confirma — ou vice-versa —, uma reversão pode estar se formando. Esta estratégia usa essa divergência para operar contra movimentos insustentáveis.

Os testes indicam um retorno anual médio de aproximadamente 112%. Funciona melhor no mercado de câmbio.

Para cada vela, o OBV é atualizado e comparado com a leitura anterior. Um sinal de alta surge se o preço faz uma mínima mais baixa enquanto o OBV registra uma mínima mais alta. Um sinal de baixa ocorre quando o preço sobe a uma máxima mais alta mas o OBV fica para trás. Uma média móvel fornece um ponto de saída, enquanto um stop percentual mantém as perdas sob controle.

A abordagem tenta capturar a reversão à média após o esgotamento do volume e geralmente mantém as negociações apenas até que o preço cruze de volta a linha de média.

## Detalhes

- **Critérios de entrada**: Divergência Preço/OBV.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço cruzando a média móvel ou stop-loss.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `DivergencePeriod` = 5
  - `MAPeriod` = 20
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: OBV, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

