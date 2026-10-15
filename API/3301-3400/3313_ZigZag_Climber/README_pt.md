# Estratégia ZigZag Climber
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O expert advisor ZigZag Climber gerado pelo fxDreema contém apenas três blocos: um filtro **No trade** seguido das ações **Buy now** e **Sell now**. Quando o terminal detecta que não há posições abertas, ele dispara imediatamente uma ordem de compra a mercado com níveis predefinidos de stop-loss e take-profit e, sem verificações adicionais, coloca uma ordem de venda a mercado simétrica. Ambas as operações herdam os mesmos parâmetros de risco e foram pensadas para coexistir como um par protegido por hedge.

Este port C# reproduz esse comportamento no StockSharp aguardando o primeiro candle concluído do timeframe escolhido e então enviando as pernas de compra e venda em sequência com distâncias protetoras idênticas. Não há geração adicional de sinal, trailing ou gestão de posição, exatamente como no projeto MQL de origem.

## Lógica de negociação
1. Aguardar até que o primeiro candle do timeframe configurado esteja completamente formado.
2. Se a estratégia puder operar e nenhuma ordem tiver sido colocada, enviar uma **compra** a mercado usando o volume fixo.
3. Anexar ordens de stop-loss e take-profit à compra usando distâncias em pips no estilo MetaTrader (convertidas pelo `PriceStep` do instrumento).
4. Enviar imediatamente uma **venda** a mercado com o mesmo volume e anexar níveis protetores espelhados.
5. Não abrir novas ordens pelo restante da execução.

> **Importante:** MetaTrader 4 trabalha em modo hedging, portanto os dois lados podem permanecer abertos simultaneamente. StockSharp usa o modelo de execução da corretora; em contas netting, a segunda ordem compensará a primeira e a estratégia terminará zerada. Use um conector compatível com hedge (por exemplo, gateway MetaTrader configurado para contas hedge) se quiser manter as duas pernas ativas.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `Candle Type` | 1 minuto | Timeframe que dispara a sequência única de entrada. |
| `Trade Volume` | 0.01 | Volume fixo aplicado às duas ordens a mercado. |
| `Stop-Loss (pips)` | 99.9 | Distância do stop protetor em pips MetaTrader (trata símbolos de 4/5 dígitos automaticamente). |
| `Take-Profit (pips)` | 100 | Distância do alvo em pips MetaTrader. |

Todas as distâncias são convertidas para pontos de preço via `PriceStep` e precisão decimal do instrumento antes de serem passadas para `SetStopLoss`/`SetTakeProfit`.

## Gestão de risco
A estratégia depende do serviço integrado `StartProtection()` e dos métodos auxiliares `SetStopLoss`/`SetTakeProfit` para colocar ordens protetoras logo após cada ordem a mercado. Não há lógica de trailing ou break-even.

## Notas de uso
- Atribua o ativo e a carteira desejados antes de iniciar a estratégia. Garanta que o símbolo exponha `PriceStep` e `Decimals` para que a conversão de pips funcione corretamente.
- Como a lógica de entrada roda apenas uma vez, reiniciar a estratégia é a única forma de criar um novo ciclo de hedge.
- Ao testar em um simulador netting, o comportamento realizado será diferente do MetaTrader: a ordem de venda neutralizará a compra quase imediatamente.
