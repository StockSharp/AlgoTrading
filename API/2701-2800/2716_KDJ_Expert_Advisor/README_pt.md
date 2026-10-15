# Estratégia do Consultor Especialista KDJ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o MetaTrader 5 «KDJ Expert Advisor» de senlin ge. Ela opera em um único símbolo usando sinais do oscilador KDJ, uma evolução do oscilador estocástico onde a linha %K é suavizada duas vezes. A estratégia observa a diferença entre as linhas %K e %D (frequentemente chamada de linha J) para identificar reversões de momentum, abrindo apenas uma posição por vez. O gerenciamento de operações espelha o expert advisor original: cada operação recebe imediatamente um stop-loss fixo e take-profit expressos em pips e traduzidos em distância de preço usando as configurações do instrumento.

A implementação usa a API de alto nível do StockSharp com uma subscrição de velas e o indicador `Stochastic` integrado, configurado para corresponder aos parâmetros KDJ da versão MQL5. O código detecta automaticamente símbolos Forex de 3 ou 5 dígitos e ajusta o valor do pip de acordo.

## Lógica do indicador
O indicador subjacente funciona em três etapas:

1. **Cálculo RSV** – Para cada vela concluída, calcular o Valor Estocástico Bruto em `KDJ Length` velas:
   \[
   RSV = \frac{Close - LowestLow}{HighestHigh - LowestLow} \times 100
   \]
2. **Suavização %K** – Calcular a média dos últimos valores `Smooth %K` de RSV para obter a linha %K.
3. **Suavização %D** – Calcular a média dos últimos valores `Smooth %D` de %K para obter a linha %D.

A estratégia então analisa `K - D` (referido como *KDC* na fonte original) e a inclinação de %K para detectar reversões.

## Critérios de entrada
Uma posição de mercado é aberta apenas se não houver posição existente para o símbolo. Os sinais são avaliados em velas concluídas:

- **Compra** quando qualquer uma das seguintes condições for verdadeira:
  - `K - D` cruza acima de zero (de negativo a positivo); ou
  - `K - D` está acima de zero e a linha %K está subindo (`K_current > K_previous`).
- **Venda** quando qualquer uma das seguintes condições for verdadeira:
  - `K - D` cruza abaixo de zero (de positivo a negativo); ou
  - `K - D` está abaixo de zero e a linha %K está caindo (`K_current < K_previous`).

Isso corresponde à estrutura booleana do expert advisor MQL5 original, garantindo timing de operação idêntico.

## Gestão de riscos
- Cada ordem executada recebe um stop-loss protetor e take-profit, medidos em pips e convertidos em distância de preço através do tamanho do tick do instrumento. Um valor de zero desabilita o trecho de proteção correspondente.
- A estratégia não faz pirâmide nem média de posições. Permanece zerada até que a posição atual seja fechada pelas ordens protetoras ou por intervenção manual.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| **Candle Type** | Tipo de dados/período das velas de entrada. | Período de 15 minutos |
| **KDJ Length** | Número de velas para o cálculo RSV. | 30 |
| **Smooth %K** | Número de valores RSV usados para suavizar a linha %K. | 3 |
| **Smooth %D** | Número de valores %K usados para suavizar a linha %D. | 6 |
| **Stop Loss (pips)** | Distância do stop-loss protetor. Defina como 0 para desabilitar. | 25 |
| **Take Profit (pips)** | Distância do take-profit protetor. Defina como 0 para desabilitar. | 45 |
| **Order Volume** | Quantidade enviada com ordens a mercado. | 1 |

Todos os parâmetros suportam intervalos de otimização idênticos às entradas do expert advisor original.

## Notas de uso
1. Configure o instrumento e o conector desejados no testador ou no ambiente ao vivo.
2. Ajuste o tipo de vela para corresponder ao período do gráfico que deseja emular do MetaTrader.
3. Opcionalmente, otimize os parâmetros KDJ, stop-loss, take-profit ou volume de ordem.
4. Inicie a estratégia. As ordens são geradas apenas em velas completamente formadas.
5. O gráfico exibe automaticamente velas, o indicador KDJ e as operações executadas para confirmação visual.

## Diferenças do EA original
- Usa o indicador `Stochastic` do StockSharp com períodos de suavização para replicar os buffers KDJ do MQL5; nenhum arquivo de indicador externo é necessário.
- As ordens protetoras são gerenciadas através de `StartProtection`, que envia saídas a mercado quando acionadas.
- O volume é um parâmetro fixo em vez do modelo de risco `MoneyFixedMargin` do MQL5, mantendo a implementação concisa e focada na lógica de sinal.
