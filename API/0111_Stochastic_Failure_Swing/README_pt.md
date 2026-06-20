# Estratégia de Oscilação de Falha do Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Oscilação de Falha do Stochastic monitora o oscilador em busca de uma máxima mais baixa acima de 80 ou uma mínima mais alta abaixo de 20.
Quando o indicador falha em atingir um novo extremo e então reverte, frequentemente sinaliza uma mudança de tendência.

Os testes indicam um retorno anual médio de aproximadamente 70%. Funciona melhor no mercado de ações.

A estratégia compra quando uma mínima mais alta se forma abaixo de 20 e %K cruza de volta acima de %D, ou vende quando uma máxima mais baixa ocorre acima de 80 e %K cruza abaixo.

As operações empregam um pequeno stop percentual e são encerradas quando o stochastic cruza de volta pelo nível do swing anterior.

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
  - Indicadores: Stochastic
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

