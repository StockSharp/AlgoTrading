# Estratégia JFatl Candle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o comportamento do Expert Advisor original **Exp_JFatlCandle_MMRec.mq5** dentro do framework StockSharp.
Ela analisa as mudanças de cor produzidas pelo filtro de candles JFatl e as combina com um bloco adaptativo de gestão de dinheiro
que reduz o tamanho da posição após um número configurável de perdas recentes.

## Ideia de trading

* Constrói candles sintéticos filtrando os valores OHLC clássicos com o kernel da Fast Adaptive Trend Line (FATL).
  A implementação usa a tabela de coeficientes original de 39 taps seguida de uma etapa de suavização exponencial para
  aproximar a média móvel Jurik usada no MetaTrader.
* Detecta transições de cor do corpo do candle sintético:
  * cor **2** (altista) significa que o fechamento filtrado está acima da abertura filtrada;
  * cor **0** (baixista) significa que o fechamento filtrado está abaixo da abertura filtrada;
  * cor **1** marca um corpo neutro.
* Uma cor altista na barra com `SignalBar + 1` períodos de idade obriga a estratégia a fechar qualquer posição vendida e preparar-se
  para uma nova entrada comprada quando a barra com `SignalBar` períodos de idade não for mais altista.
* Uma cor baixista observada da mesma forma fecha posições compradas e habilita uma entrada vendida quando a barra mais recente não é mais baixista.
* Posições compradas e vendidas são dimensionadas pela lógica de MMRecounter. Quando as últimas `TotalTrigger` operações da
  direção correspondente incluem pelo menos `LossTrigger` resultados negativos, a estratégia muda para o tamanho de posição reduzido.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Período dos candles que são alimentados no filtro FATL (padrão: 12 horas).
| `SignalBar` | Número de barras completadas para retrospectar ao ler o buffer de cores. `0` significa usar a barra terminada atual, `1` reproduz os padrões do MT5.
| `SmoothingLength` | Comprimento da suavização exponencial aplicada após o kernel FATL para emular a suavização Jurik.
| `NormalVolume` | Tamanho de posição padrão quando o histórico recente é saudável.
| `ReducedVolume` | Tamanho de posição aplicado após o MMRecounter detectar muitas perdas.
| `BuyTotalTrigger` / `SellTotalTrigger` | Quantidade de operações históricas (por direção) inspecionadas pelo MMRecounter.
| `BuyLossTrigger` / `SellLossTrigger` | Número mínimo de perdas dentro da janela inspecionada que força o tamanho de posição reduzido.
| `EnableBuyEntries` / `EnableSellEntries` | Permitir abertura de posições compradas/vendidas.
| `EnableBuyExits` / `EnableSellExits` | Permitir fechamento de posições compradas/vendidas quando o sinal oposto aparecer.
| `StopLossPoints` | Stop de proteção opcional para ambas as direções expresso em passos de preço do instrumento. Definir como `0` para desativar.
| `TakeProfitPoints` | Alvo de lucro opcional em passos de preço. Definir como `0` para desativar.

## Regras de trading

1. Construir os valores OHLC filtrados e determinar a cor do candle em cada barra terminada.
2. Seja `C1` a cor da barra de `SignalBar + 1` períodos atrás e `C0` a cor da barra de `SignalBar` períodos atrás
   (para `SignalBar = 0` o candle atual é usado como `C0` e o anterior como `C1`).
3. Se `C1 == 2` (altista):
   * fechar qualquer posição vendida quando `EnableSellExits` for `true`;
   * abrir uma posição comprada com o tamanho calculado quando `EnableBuyEntries` for `true` **e** `C0 != 2`.
4. Se `C1 == 0` (baixista):
   * fechar qualquer posição comprada quando `EnableBuyExits` for `true`;
   * abrir uma posição vendida quando `EnableSellEntries` for `true` **e** `C0 != 0`.
5. As posições também podem ser fechadas por limites de stop-loss ou take-profit quando o range do candle toca o nível configurado.

## Gestão de dinheiro

A estratégia armazena o lucro de cada operação comprada e vendida completada separadamente. Quando uma nova entrada é considerada, ela escaneia
até `TotalTrigger` operações anteriores dessa direção. Se pelo menos `LossTrigger` operações dentro dessa janela terminaram com um resultado
negativo, o volume reduzido é usado; caso contrário, o volume normal é negociado.

## Notas

* A lógica de stop-loss e take-profit baseada em passos de preço depende do valor `Security.PriceStep`. Se o instrumento não o fornece,
  assume-se um passo de `1`.
* O filtro FATL precisa de pelo menos 39 candles históricos antes de ficar operacional. Nenhuma operação é gerada até que dados suficientes sejam acumulados.
* A estratégia mantém um histórico compacto de operações para o bloco MMRecounter; assim que o histórico ultrapassa 100 itens, os registros mais antigos
  são descartados automaticamente.
