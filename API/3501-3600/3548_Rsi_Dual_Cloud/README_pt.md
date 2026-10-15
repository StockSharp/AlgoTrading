# RSI Estratégia de nuvem dupla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **RSI Estratégia de Nuvem Dupla** é uma versão StockSharp do consultor especialista MetaTrader “RSI Nuvem Dupla EA”.
Ele negocia em uma série de velas configuráveis e analisa dois cálculos do Índice de Força Relativa (RSI) – um cálculo rápido
e uma linha lenta. Os sinais são gerados quando o RSI rápido entra, permanece dentro ou sai de um nível de sobrevenda/sobrecompra definido
zona, ou quando a linha rápida cruza a linha lenta. A estratégia pode opcionalmente inverter seus sinais e pode ser restrita
para operação somente longa ou somente curta.

A estratégia opera apenas com ordens de mercado. Quando um novo sinal é recebido, a posição existente no oposto
direção é fechada antes de abrir uma nova posição. O dimensionamento da posição é controlado através de um único parâmetro de volume.

## Lógica de Sinais
1. **Sinal de entrada** – é acionado quando o rápido RSI cruza a zona:
   - Longo: anterior RSI acima do nível inferior e atual RSI abaixo dele.
   - Curto: anterior RSI abaixo do nível superior e atual RSI acima dele.
2. **Sendo sinal** – dispara enquanto o RSI rápido permanecer dentro da zona:
   - Longo: rápido RSI abaixo do nível inferior.
   - Curto: rápido RSI acima do nível superior.
3. **Sinal de saída** – é acionado quando o RSI rápido sai da zona:
   - Longo: anterior RSI abaixo do nível inferior e atual RSI acima dele.
   - Curto: anterior RSI acima do nível superior e atual RSI abaixo dele.
4. **Sinal de cruzamento** – utiliza o comportamento de nuvem dupla:
   - Longo: cruzamento rápido RSI acima do lento RSI.
   - Curto: cruzamento rápido RSI abaixo do lento RSI.

Qualquer combinação das quatro condições pode ser habilitada. Pelo menos uma condição deve estar ativa para que as entradas ocorram.
Quando a opção **Reverse** está habilitada, os sinais longos e curtos são trocados.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| **Tipo de vela** | A série de velas usada para cálculos (padrão: 1 hora). |
| **Rápido RSI / Lento RSI** | Períodos para cálculos rápidos e lentos RSI. |
| **Nível superior/Nível inferior** | RSI limites para as zonas de sobrecompra e sobrevenda. |
| **Volume do pedido** | Volume para ordens de mercado. |
| **Use Entrada/Estar/Saída/Travessia** | Alterna para cada família de sinais. |
| **Velas Fechadas** | Se habilitado, os sinais serão avaliados apenas nas velas finalizadas. |
| **Reverso** | Troca sinais longos e curtos. |
| **Modo de negociação** | Limita a negociação a posições longas, curtas ou ambas as direções. |

## Notas de uso
- A estratégia assina uma única série de velas e executa dois indicadores RSI vinculados ao API de alto nível.
- Apenas são utilizadas ordens de mercado; qualquer exposição aberta na direção oposta é fechada antes de uma nova negociação ser colocada.
- A configuração padrão corresponde ao consultor especialista original (rápido RSI 5, lento RSI 15, níveis 25/75).
- Combine os alternadores de sinal para reproduzir as combinações de indicadores da versão MetaTrader.
