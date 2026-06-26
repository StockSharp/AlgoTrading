# Estratégia de Exp BlauHlm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Exp BlauHlm** é um port StockSharp do consultor especialista MetaTrader 5 `Exp_BlauHLM.mq5`. O sistema baseia-se no oscilador Blau High-Low Momentum (HLM) que compara máximas e mínimas recentes, suaviza a diferença com um pipeline XMA configurável e reage a três modos de operação distintos:

- **Breakdown** – negocia uma quebra da linha zero do componente do histograma.
- **Twist** – busca torções de momentum dentro do histograma para capturar transições de inclinação.
- **CloudTwist** – trabalha com os envelopes superior e inferior produzidos pelo indicador e reage a cruzamentos de "nuvem".

A implementação StockSharp mantém os mesmos parâmetros, valores padrão e regras de trading enquanto traduz os detalhes de gerenciamento de dinheiro para a propriedade genérica `Volume` da estratégia base.

## Lógica de trading

1. Para cada vela concluída do período configurado, a estratégia calcula o oscilador Blau HLM:
   - Calcular a diferença entre a máxima mais recente e a máxima `XLength - 1` barras atrás e uma diferença espelhada para mínimas.
   - Limitar contribuições negativas a zero e subtraí-las para obter o valor HLM bruto (expresso em pontos quando o instrumento especifica um tamanho de tick).
   - Suavizar a sequência através de quatro médias móveis em cascata com métodos idênticos mas comprimentos independentes.
2. Dependendo do **Mode** selecionado:
   - **Breakdown** abre uma posição comprada quando o valor do histograma anterior é positivo e o novo não é positivo (recuperação da linha zero) e fecha vendidos na mesma situação. Uma regra simétrica trata entradas vendidas/saídas compradas quando o histograma muda de negativo para não negativo.
   - **Twist** compara a inclinação do histograma em três pontos históricos. Uma aceleração local (valor médio subindo após uma queda) aciona a lógica comprada, enquanto uma desaceleração (valor médio caindo após uma subida) ativa a lógica vendida.
   - **CloudTwist** monitora os dois envelopes suavizados. Quando a banda superior anterior está acima da inferior e os novos valores se cruzam abaixo/acima um do outro, sinais comprados ou vendidos são produzidos respectivamente.
3. O gerenciamento de posições segue as permissões `BuyOpen`, `SellOpen`, `BuyClose`, `SellClose` e usa o `Volume` da estratégia para entradas de mercado. Sinais opostos fecham posições existentes antes de abrir uma nova.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------ | --------- |
| `CandleType` | `DataType` | Velas `H4` | Período processado pelo oscilador. |
| `SmoothingMethod` | `SmoothMethod` | `Exponential` | Método de média móvel para cada estágio de suavização (modos legacy não suportados recorrem a EMA). |
| `XLength` | `int` | `2` | Período em barras para medir o momentum bruto de máxima/mínima. |
| `FirstLength` | `int` | `20` | Período do primeiro estágio de suavização. |
| `SecondLength` | `int` | `5` | Período do segundo estágio de suavização. |
| `ThirdLength` | `int` | `3` | Período do terceiro estágio de suavização. |
| `FourthLength` | `int` | `3` | Período do suavizador de sinal final. |
| `Phase` | `int` | `15` | Parâmetro de fase Jurik (limitado a ±100, ignorado por suavizadores não Jurik). |
| `SignalBar` | `int` | `1` | Deslocamento histórico ao comparar valores do indicador. |
| `EntryMode` | `Mode` | `Twist` | Lógica de trading copiada do especialista MQL (`Breakdown`, `Twist`, `CloudTwist`). |
| `BuyOpen` / `SellOpen` | `bool` | `true` | Permitir abertura de posições compradas/vendidas. |
| `BuyClose` / `SellClose` | `bool` | `true` | Permitir fechamento de posições compradas/vendidas com sinal oposto. |

## Notas de conversão

- A biblioteca MQL `SmoothAlgorithms.mqh` inclui filtros proprietários (JJMA, JurX, ParMA, T3, VIDYA, AMA). O StockSharp fornece alternativas integradas para as variantes mais comuns, portanto modos não suportados são aproximados com a média móvel exponencial para manter o fluxo de trabalho intacto.
- Parâmetros de gerenciamento de dinheiro (`MM`, `MarginMode`, `StopLoss`, `TakeProfit`, `Deviation`) controlam o tamanho da ordem e execução no MetaTrader. Neste port, a propriedade genérica `Volume` define o tamanho da posição e ordens são sempre enviadas a mercado.
- O timing do sinal espelha o deslocamento `SignalBar` usado pelo especialista original: a estratégia mantém um buffer circular interno de valores do indicador e realiza comparações em snapshots históricos para que os resultados de otimização permaneçam consistentes.
- A proteção de risco é delegada ao `StartProtection()`; configure regras globais de stop-loss/take-profit na estratégia pai ou conector de trading se necessário.

## Dicas de uso

1. Definir a propriedade `Volume` antes de iniciar a estratégia para definir o número de lotes/contratos por operação.
2. Para símbolos sem um `PriceStep` significativo, o oscilador trabalha em unidades de preço brutas. Considere reescalar os parâmetros se o ativo usar tamanhos de tick grandes.
3. Ao experimentar com suavizadores não exponenciais, lembre que comprimentos muito curtos combinados com extremos de fase Jurik podem levar a sinais instáveis; amplie os períodos para maior estabilidade.
4. Combine a estratégia com controles de risco em nível de portfólio ou as regras de proteção integradas para emular o comportamento original de stop-loss/take-profit.
