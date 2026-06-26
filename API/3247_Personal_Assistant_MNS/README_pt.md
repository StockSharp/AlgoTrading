# Estratégia de Personal Assistant MNS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista MetaTrader `personal_assistant_codeBase_MNS` para o StockSharp. Atua como um assistente de trading manual: em vez de gerar sinais autônomos, expõe métodos C# que replicam as ações acionadas por teclas de atalho do EA original (abrir/fechar operações, ajustar volume ou liquidar posições lucrativas). O assistente também registra métricas informativas sobre o símbolo, ordens ativas e níveis de risco configurados em cada vela concluída.

## Como funciona

1. A estratégia subscreve a uma série de velas configurável (`CandleType`, 1 minuto por padrão).
2. Cada vela concluída aciona uma atualização que imprime: posição atual, PnL, número de ordens stop/take ativas, spread, valor do tick e o número mágico configurado.
3. Comandos manuais (ex.: `PressBuy()` ou `PressSell()`) enviam ordens de mercado com o volume do assistente atual. Níveis opcionais de stop-loss e take-profit são traduzidos de distâncias em pip e armazenados internamente na estratégia.
4. Os níveis protetores são emulados em dados de velas: se o preço tocar o stop ou alvo armazenado, a estratégia emite saídas de mercado.
5. Uma regra opcional de mover para break-even (`UseTrailingStop`) é armada após o preço avançar `BreakEvenTriggerPips`; uma vez armada, liquida a posição se o preço recuar para o preço de entrada mais `BreakEvenOffsetPips`.

## Funcionalidades

- Replica os botões 1–8 do assistente MQL via métodos públicos:
  - `PressBuy()` / `PressSell()` – abrir ordens de mercado com níveis protetores opcionais.
  - `PressCloseAll()` – zerar toda a exposição.
  - `IncreaseVolume()` / `DecreaseVolume()` – ajustar o volume do assistente em 0,01 lotes.
  - `CloseLongPositions()` / `CloseShortPositions()` – fechar apenas um lado.
  - `CloseProfitablePositions()` – fechar a posição quando o PnL flutuante é positivo.
- Registra uma legenda de ação detalhada no início quando `DisplayLegend` está habilitado.
- Converte distâncias de risco baseadas em pip em preços absolutos usando o passo de preço e a precisão decimal do instrumento.
- Suporta trailing de break-even para posições compradas e vendidas, imitando a rotina original `MOVETOBREAKEVEN()`.
- Mantém níveis stop/take armazenados independentes para operações compradas e vendidas para que ao mudar de direção os níveis obsoletos sejam descartados automaticamente.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `MagicNumber` | Identificador informacional copiado do input `MagicNo` do MQL. |
| `DisplayLegend` | Habilitar para imprimir a legenda de controle e mensagens de status por vela. |
| `OrderVolume` | Volume base da ordem de mercado (lotes) reutilizado por todas as ações manuais. |
| `Slippage` | Slippage máximo tolerado (em ticks), armazenado como referência. |
| `TakeProfitPips` | Distância de pip para o nível de take-profit armazenado (0 o desabilita). |
| `StopLossPips` | Distância de pip para o nível de stop-loss armazenado (0 o desabilita). |
| `UseTrailingStop` | Habilitar ou desabilitar a lógica de trailing de break-even. |
| `BreakEvenTriggerPips` | Distância de lucro (em pips) necessária antes de o stop de break-even ser armado. |
| `BreakEvenOffsetPips` | Offset (em pips) adicionado ao preço de entrada quando o stop é armado. |
| `CandleType` | Série de velas usada para monitoramento e emulação de níveis. |

## Dicas de uso

- Chamar os métodos auxiliares a partir de ações do Designer, scripts ou controles de UI para imitar pressionamentos de teclas do painel original do MetaTrader.
- Os níveis protetores e distâncias de break-even dependem do instrumento fornecer `PriceStep`, `StepPrice` e `Decimals`. Para instrumentos exóticos sem esses metadados, ajustar as distâncias de pip manualmente ou desabilitar as funcionalidades definindo-as como `0`.
- Como os níveis stop/take são reproduzidos usando máximas e mínimas de velas, picos intra-barra muito rápidos podem não ser capturados a menos que o período da vela seja pequeno. Reduzir o período se uma granularidade maior for necessária.
- `CloseProfitablePositions()` replica o comportamento do "botão 8": verifica o PnL flutuante e fecha toda a posição apenas quando o valor é estritamente positivo.

## Diferenças em relação à versão MetaTrader

- As etiquetas do gráfico são substituídas por entradas de log porque o StockSharp não expõe as mesmas primitivas de desenho dentro das estratégias.
- As ordens de stop-loss e take-profit são simuladas através de saídas de mercado em eventos de velas em vez de ordens pendentes imediatas.
- O gerenciamento de break-even é implementado com ordens de mercado do StockSharp; não modifica ordens protetoras existentes.
- O slippage é mantido como parâmetro informacional; a execução real é tratada pelo conector StockSharp.
