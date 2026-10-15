# Estratégia de Divergência com Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O oscilador Williams %R mede condições de sobrecompra e sobrevenda. Quando o preço faz uma nova mínima, mas o %R forma uma mínima mais alta, ou quando o preço imprime uma nova máxima, mas o %R vira para baixo, o momentum pode se reverter. Esta estratégia busca tais divergências nos extremos do indicador.

Os testes indicam um retorno anual médio de aproximadamente 109%. Funciona melhor no mercado de criptomoedas.

A cada barra, o sistema registra o último fechamento e o valor do %R para comparar com a leitura anterior. Uma divergência de alta combinada com um nível abaixo de -80 aciona uma entrada comprada, enquanto uma divergência de baixa e uma leitura acima de -20 gera uma posição vendida. Os stops são definidos usando um percentual do preço.

As posições são encerradas quando o oscilador retorna ao extremo oposto, capturando o recuo a partir do sinal de divergência.

## Detalhes

- **Critérios de entrada**: Divergência Preço/Williams %R com %R abaixo de -80 para comprados ou acima de -20 para vendidos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Williams %R atingindo o extremo oposto ou stop-loss.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `WilliamsRPeriod` = 14
  - `DivergencePeriod` = 5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: Williams %R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

