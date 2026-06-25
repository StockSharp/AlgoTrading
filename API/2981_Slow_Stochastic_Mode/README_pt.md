# Estratégia Slow Stochastic Mode
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Slow Stochastic Mode** é uma conversão do consultor especialista MetaTrader `Exp_Slow-Stoch.mq5` para a API de alto nível do StockSharp. O sistema opera no preço de fechamento de candles finalizados e usa um oscilador estocástico suavizado para detectar mudanças de regime. Três modos de sinal distintos estão disponíveis para que o trader decida se reage a quebras de nível, giros de momentum ou cruzamentos de linhas.

## Ideia principal

A estratégia observa as linhas %K e %D de um oscilador estocástico lento que é adicionalmente suavizado pelo parâmetro `Slowing`. Dependendo do *Modo de sinal* selecionado, o algoritmo avalia o oscilador uma ou mais barras atrás (controlado por `SignalBar`) e abre uma nova posição ou fecha o lado oposto quando um evento qualificado aparece. As ordens são sempre colocadas com execuções de mercado.

## Modos de sinal

- **Breakdown** – procura %K rompendo através do nível 50. Um cruzamento de baixo para cima de 50 gera uma entrada comprada e fecha posições vendidas. Um cruzamento de cima para baixo de 50 produz uma entrada vendida e fecha posições compradas.
- **Twist** – detecta uma mudança de direção de %K. Quando o oscilador estava caindo duas barras atrás e gira para cima na barra avaliada, a estratégia abre ou reverte para uma negociação comprada. A situação inversa aciona vendidos.
- **CloudTwist** – rastreia a mudança de cor da "nuvem" estocástica observando um cruzamento de %K vs %D. Um cruzamento altista (%K acima de %D) abre ou protege comprados, enquanto um cruzamento baixista (%K abaixo de %D) faz o oposto.

Todos os modos respeitam os quatro interruptores de permissão: entradas compradas/vendidas e saídas compradas/vendidas podem ser habilitadas ou desabilitadas de forma independente.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Período H4 | Tipo de candle usado para cálculos do indicador. |
| `KPeriod` | 5 | Período de retrospectiva para a linha %K. |
| `DPeriod` | 3 | Comprimento da média móvel para %D. |
| `Slowing` | 3 | Suavização extra aplicada a %K antes das comparações. |
| `SignalBar` | 1 | Número de barras fechadas atrás usadas para avaliar os sinais. |
| `StopLossPoints` | 1000 | Distância de stop-loss em passos do instrumento (definir 0 para desabilitar). |
| `TakeProfitPoints` | 2000 | Distância de take-profit em passos do instrumento (definir 0 para desabilitar). |
| `EnableLongEntries` | true | Permite à estratégia abrir posições compradas. |
| `EnableShortEntries` | true | Permite à estratégia abrir posições vendidas. |
| `EnableLongExits` | true | Permite fechar posições compradas quando um sinal de reversão aparece. |
| `EnableShortExits` | true | Permite fechar posições vendidas quando um sinal de reversão aparece. |
| `Mode` | Twist | Modo de sinal selecionado. |

A estratégia usa o indicador integrado do StockSharp `StochasticOscillator` e o alimenta com os comprimentos configurados. O parâmetro `SignalBar` permite reproduzir o comportamento do MetaTrader de referenciar o candle anterior (padrão = 1) ou agir na última barra completada quando definido como 0.

## Gestão de negociações

- As ordens são enviadas com chamadas `BuyMarket` e `SellMarket`. Os giros de posição são tratados automaticamente adicionando o valor absoluto da posição atual ao volume base da ordem.
- A proteção opcional de stop-loss e take-profit é ativada via `StartProtection`. As distâncias são interpretadas como ticks/passos, portanto o StockSharp as multiplica internamente pelo tamanho do passo do instrumento.
- Nenhum trailing stop é anexado; a proteção permanece estática até ser preenchida ou a estratégia sair manualmente.

## Lógica de saída

- No modo **Breakdown**, a mesma quebra de limiar que abre um lado fecha o outro.
- No modo **Twist**, detectar uma reversão de momentum fecha a posição oposta antes de abrir a nova.
- No modo **CloudTwist**, %K cruzando %D tanto aciona a entrada quanto simultaneamente liquida o viés oposto.

Quando as permissões de entrada são desabilitadas, apenas a lógica de saída correspondente permanece ativa, o que permite aos usuários executar a estratégia em um modo de "gerenciar exposição existente".

## Notas de implementação

- O histórico do oscilador é rastreado em pequenos buffers na memória para que a estratégia possa inspecionar os offsets de barra exigidos pelo consultor especialista original.
- Todas as decisões são avaliadas apenas em candles finalizados (`candle.State == Finished`).
- O renderizador de gráfico desenha os candles subjacentes e o oscilador estocástico quando os serviços de gráfico estão disponíveis.

Esta conversão mantém a intenção do sistema MQL5 original enquanto aproveita as vinculações de indicadores, metadados de parâmetros e controles de risco integrados do StockSharp.
