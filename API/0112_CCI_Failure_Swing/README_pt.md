# Estratégia CCI Failure Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O CCI Failure Swing é baseado no Commodity Channel Index formando uma máxima mais baixa acima de +100 ou uma mínima mais alta abaixo de -100.
Esta incapacidade de atingir um novo extremo frequentemente sinaliza o fim da tendência anterior.

Os testes indicam um retorno anual médio de aproximadamente 73%. Funciona melhor no mercado de criptomoedas.

A estratégia vai comprado quando o CCI se mantém acima de -100 e vira para cima, ou vendido quando falha perto de +100 e vira para baixo.

Um stop percentual mantém o risco pequeno e as operações saem se o CCI cruzar de volta pelo nível de swing anterior.

## Detalhes

- **Critérios de entrada**: sinal do indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

