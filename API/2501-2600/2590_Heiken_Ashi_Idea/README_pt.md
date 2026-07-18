# Estratégia Heiken Ashi Idea
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia reproduz o comportamento do consultor especialista original **HeikenAshiIdea.mq4** usando a API de alto nível do StockSharp. Aguarda sinais de alta ou baixa alinhados em dois timeframes de candles Heikin Ashi e então coloca ordens limite pendentes a uma distância configurável do mercado. O objetivo é capturar movimentos de continuação fortes quando o candle Heikin Ashi mais recente não tem pavio contra a direção da tendência.

## Lógica de trading

1. **Reconstrução do Heikin Ashi** – a estratégia reconstrói internamente candles Heikin Ashi para o timeframe de trading primário e para um timeframe de confirmação superior. Para cada timeframe são armazenados os últimos dois candles Heikin Ashi de forma que a direção do corpo e a presença de pavios possam ser analisados.
2. **Condição de rompimento** – um setup comprado aparece quando ambos os timeframes mostram:
   - o candle Heikin Ashi mais recente é de alta e sua abertura é igual ao mínimo (sem sombra inferior), e
   - o candle Heikin Ashi anterior também é de alta mas tem sombra inferior.
   Um setup vendido requer as condições baixistas simétricas (sem sombra superior no último candle e sombra superior no anterior).
3. **Filtro de volatilidade ATR** – o Average True Range com comprimento configurável deve estar subindo (`ATR[t] > ATR[t-1]`) se o filtro estiver habilitado. Isso reproduz a verificação de volatilidade `ActiveMarket` original.
4. **Janela de trading** – os sinais são ignorados fora da sessão de trading definida pelo usuário (padrão: 09:00–19:00).
5. **Colocação de ordens** – quando um sinal é válido a estratégia coloca uma única ordem limite pendente:
   - Sinal comprado → ordem de compra limite em `ClosePrice - DistancePoints * PriceStep`.
   - Sinal vendido → ordem de venda limite em `ClosePrice + DistancePoints * PriceStep`.
   Ordens pendentes opostas existentes são canceladas antes de enfileirar uma nova ordem. A estratégia rastreia apenas uma ordem pendente por direção e limpa automaticamente as referências quando a ordem fica inativa.
6. **Gestão de posição** – distâncias opcionais de take-profit e stop-loss são traduzidas em mecanismos protetores do StockSharp via `StartProtection`. Quando um novo candle do timeframe "fechar tudo" abre, a estratégia cancela todas as ordens pendentes e fecha qualquer posição aberta se o sinalizador estiver habilitado. Isso imita o comportamento `UseCloseAll` do EA original.

## Gestão de risco

- Os níveis protetores são expressos em passos de preço (pontos) para ficar próximo à implementação MetaTrader. São opcionais; usar `0` desabilita a proteção correspondente.
- Ordens pendentes só são colocadas quando a distância calculada é positiva e o volume de trading é maior que zero.
- A estratégia nunca faz médias de posições automaticamente; primeiro liquida a ordem pendente oposta antes de agendar uma nova.
- Uma tolerância igual à metade do passo de preço do instrumento é usada ao verificar se os candles Heikin Ashi têm ou não pavios. Isso previne problemas de arredondamento em ponto flutuante enquanto permanece fiel às comparações estritas originais.

## Parâmetros

| Nome | Descrição | Valor padrão |
| --- | --- | --- |
| `DistancePoints` | Distância em passos de preço para as ordens limite pendentes. | `8` |
| `StopLossPoints` | Distância de stop-loss em passos de preço (0 desabilita o stop). | `0` |
| `TakeProfitPoints` | Distância de take-profit em passos de preço (0 desabilita o alvo). | `20` |
| `UseCloseAllOnNewBar` | Fechar posição e cancelar ordens quando um novo candle do timeframe de fechamento abre. | `true` |
| `CandleType` | Tipo de candle primário usado para sinais de trading. | Timeframe `30m` |
| `HigherCandleType` | Tipo de candle de confirmação para o filtro multi-timeframe. | Timeframe `1d` |
| `CloseAllCandleType` | Tipo de candle que aciona a rotina de fechar tudo. | Timeframe `7d` |
| `StartHour` | Primeira hora da sessão de trading (inclusivo). | `9` |
| `EndHour` | Última hora da sessão de trading (inclusivo). | `19` |
| `UseAtrFilter` | Habilitar o filtro de volatilidade crescente ATR. | `true` |
| `AtrPeriod` | Período ATR usado pelo filtro de volatilidade. | `14` |

## Notas adicionais

- A estratégia usa a propriedade `Volume` embutida de `Strategy` como tamanho de ordem base. Ajuste-a antes de iniciar a estratégia.
- Como a implementação do StockSharp usa preços de fechamento de candles para colocação de ordens pendentes, a execução ao vivo pode diferir ligeiramente do código MT4 original que usava cotações bid/ask, mas a ideia central permanece intacta.
- Para estender a lógica para diferentes mercados, basta ajustar os tipos de candles, a janela de trading e os parâmetros de distância mantendo a confirmação multi-timeframe no lugar.
