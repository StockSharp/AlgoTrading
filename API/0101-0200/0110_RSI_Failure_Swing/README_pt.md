# Estratégia de Oscilação de Falha do RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Oscilação de Falha do RSI é uma técnica clássica de reversão onde o RSI forma uma mínima mais alta em território de sobrevenda ou uma máxima mais baixa em território de sobrecompra.
Essa falha em atingir um novo extremo frequentemente precede uma mudança de tendência.

Os testes indicam um retorno anual médio de aproximadamente 67%. Funciona melhor no mercado de ações.

A estratégia compra quando o RSI se mantém acima de sua mínima anterior e então cruza de volta acima de 30, ou vende quando falha em superar uma máxima anterior e cruza abaixo de 70.

Um stop percentual limita a perda, e as posições são encerradas quando o RSI cruza o nível oposto.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

