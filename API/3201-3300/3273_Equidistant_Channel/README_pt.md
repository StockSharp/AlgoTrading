# Estratégia de canal equidistante
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **estratégia de canal equidistante** porta o expert advisor MQL4 original "Equidistant Channel" para a API de alto nível do StockSharp. A estratégia analisa cruzamentos da linha MACD e gerencia posições existentes por toques nas Bollinger Bands, lógica de breakeven e alvos de trailing baseados em dinheiro.

Quando a linha MACD cruza acima do seu sinal, a estratégia abre posições compradas; quando cruza abaixo do sinal, abre posições vendidas. Enquanto uma operação está ativa, a estratégia observa saídas quando o preço alcança Bollinger Bands, quando o lucro flutuante atinge alvos monetários ou percentuais configuráveis, ou quando um limite de drawdown trailing é violado. Um modo breakeven espelha a implementação do MetaTrader movendo o stop de proteção quando o lucro excede um número configurável de passos de preço.

## Indicadores
- **MACD (12, 26, 9)** - gera sinais de entrada em cruzamentos entre a linha MACD e sua linha de sinal.
- **Bollinger Bands (20, 2)** - fornecem níveis de saída sempre que o fechamento do candle atinge a banda superior ou inferior.

## Gestão da posição
- Distâncias opcionais de stop loss, take profit e trailing stop expressas em pontos de preço via `StartProtection`.
- Lógica de take profit e trailing baseada em dinheiro que acompanha o lucro flutuante usando metadados de preço/tamanho do passo do instrumento.
- Take profit percentual calculado a partir do valor inicial do portfólio.
- Modo breakeven que empurra o stop para a entrada mais um offset quando o lucro alcança um gatilho definido.

## Parâmetros
| Grupo | Nome | Padrão | Descrição |
| --- | --- | --- | --- |
| Negociação | Volume | 1 | Volume da ordem para novas entradas. |
| Geral | Tipo de candle | 5 minutos | Série de candles usada para os cálculos. |
| Indicadores | MACD rápido | 12 | Comprimento da EMA rápida para MACD. |
| Indicadores | MACD lento | 26 | Comprimento da EMA lenta para MACD. |
| Indicadores | Sinal MACD | 9 | Comprimento da linha de sinal para MACD. |
| Indicadores | Período BB | 20 | Período de retrospectiva das Bollinger Bands. |
| Indicadores | Desvio BB | 2 | Largura das Bollinger Bands em desvios padrão. |
| Risco | Stop Loss | 20 | Distância do stop loss em pontos de preço. |
| Risco | Take Profit | 50 | Distância do take profit em pontos de preço. |
| Risco | Trailing Stop | 40 | Distância do trailing stop em pontos de preço. |
| Risco | Usar TP (dinheiro) | false | Fecha quando o lucro flutuante atinge um alvo monetário absoluto. |
| Risco | Dinheiro TP | 10 | Valor absoluto de take profit na moeda da conta. |
| Risco | Usar TP (%) | false | Fecha quando o lucro flutuante atinge um percentual do capital inicial. |
| Risco | Percentual TP | 10 | Percentual do capital inicial para o take profit percentual. |
| Risco | Habilitar trailing | true | Habilita a lógica de trailing sobre o lucro flutuante. |
| Risco | Ativar trailing | 40 | Nível de lucro (moeda) que arma a lógica de trailing. |
| Risco | Passo trailing | 10 | Drawdown máximo permitido a partir do pico de lucro (moeda). |
| Risco | Usar stop BB | true | Habilita saídas quando o preço toca Bollinger Bands. |
| Risco | Usar breakeven | true | Habilita o comportamento breakeven. |
| Risco | Gatilho breakeven | 10 | Lucro (passos de preço) necessário para armar o stop breakeven. |
| Risco | Offset breakeven | 5 | Offset (passos de preço) aplicado ao nível breakeven. |

## Observações
- A estratégia funciona com um único instrumento que forneça metadados válidos de `PriceStep` e `StepPrice`, para que os cálculos monetários sejam precisos.
- O módulo de trailing de lucro segue o comportamento do MetaTrader: quando o lucro flutuante excede o limite de ativação, a estratégia registra o máximo corrente e fecha a operação quando o drawdown excede o passo trailing configurado.
- A lógica breakeven espelha o EA original usando gatilhos e offsets baseados em passos de preço.
- Todos os comentários dentro do código da estratégia são escritos em inglês, conforme exigido pelas diretrizes do projeto.
