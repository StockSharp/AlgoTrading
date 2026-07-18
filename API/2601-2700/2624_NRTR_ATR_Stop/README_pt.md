# Estratégia NRTR ATR Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia NRTR ATR Stop reproduz o comportamento do expert MetaTrader `Exp_NRTR_ATR_STOP` usando a API de alto nível do StockSharp. Ela rastreia os níveis NRTR (Non-Repainting Trailing Reverse) construídos a partir do Average True Range (ATR). Quando o preço cruza o trailing stop oposto, a tendência inverte, gerando uma nova entrada de mercado enquanto também fecha qualquer posição aberta na direção anterior.

## Lógica do indicador
* Um único **Average True Range** (`AtrPeriod`) é calculado da série de candles assinada. O valor ATR é multiplicado pelo `Coefficient` para produzir a distância entre o preço e o nível de stop atual.
* Duas linhas de stop dinâmicas são mantidas:
  * `upper stop` protege posições compradas. Segue o preço por baixo enquanto a tendência é de alta.
  * `lower stop` protege posições vendidas. Segue o preço por cima enquanto a tendência é de baixa.
* Quando o preço fecha além do stop oposto, a tendência reverte imediatamente. O stop no novo lado é inicializado usando o extremo do candle anterior mais/menos a distância ATR.
* O expert original atrasa a execução lendo o buffer do indicador `SignalBar` velas atrás. A estratégia espelha esse comportamento através de uma fila interna: cada candle finalizado empurra seu sinal para a fila, e o motor age somente quando o comprimento da fila excede `SignalBar`.

## Regras de negociação
1. **Sinal de compra** – a tendência calculada muda de neutra/baixa para alta. A estratégia opcionalmente fecha qualquer exposição vendida e abre uma nova posição comprada usando uma única ordem de mercado cujo volume equivale ao tamanho de saída necessário mais o `Volume` configurado para a nova entrada comprada.
2. **Sinal de venda** – a tendência muda de neutra/alta para baixa. A estratégia opcionalmente fecha qualquer exposição comprada e abre uma nova posição vendida da mesma forma.
3. As propriedades `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit` e `EnableShortExit` permitem controle preciso sobre quais ações são executadas quando um sinal aparece.
4. Os sinais são processados apenas em candles finalizados e enquanto a estratégia está online e tem permissão para negociar.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `AtrPeriod` | Número de candles usados para cálculo do ATR. |
| `Coefficient` | Multiplicador aplicado ao valor ATR ao construir os trailing stops. |
| `SignalBar` | Número de candles completamente fechados a aguardar antes de agir sobre um sinal armazenado. Definir como `0` para negociar imediatamente no candle atual. |
| `CandleType` | Período dos candles recebidos. |
| `EnableLongEntry` | Permitir abrir posições compradas em sinais de compra. |
| `EnableShortEntry` | Permitir abrir posições vendidas em sinais de venda. |
| `EnableLongExit` | Permitir fechar posições compradas quando ocorre um sinal de venda. |
| `EnableShortExit` | Permitir fechar posições vendidas quando ocorre um sinal de compra. |

## Notas
* A estratégia depende exclusivamente de candles finalizados; ticks intrabarra são ignorados.
* As ordens são enviadas com `BuyMarket`/`SellMarket`, combinando o fechamento de posição e a nova entrada em uma única ordem de mercado para simplicidade.
* Certifique-se de que a propriedade `Volume` está definida com um valor positivo antes de iniciar o trading ao vivo ou o backtesting.
