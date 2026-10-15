# Previous Day High Low Long
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Apenas comprado: fechamento cruza para cima high ou low observado do dia UTC anterior, com ADX nativo crescente dentro da sessão UTC. Saídas por alvo desde execução real, stop móvel ou fim da sessão.

## Níveis e entrada

Somente candles concluídos, padrão cinco minutos. Agrupa high/low pela data UTC de abertura e congela máximo/mínimo observados quando começa a próxima data. O dia anterior precisa começar com candle observado à meia-noite e as datas serem consecutivas. Não inventa níveis para início parcial ou data ausente; candles atuais não ampliam níveis anteriores.

Cruzamento high: `previousClose <= previousDayHigh && close > previousDayHigh`; low usa o nível baixo, também para cima. ADX nativo `AdxLength=11` deve estar formado e superar estritamente seu valor formado anterior, sem limiar extra ADX ou filtro direcional DI.

Apenas de zero, compra a mercado `Volume` com horário UTC do envio em `[SessionStart,SessionEnd)`, padrão09:30–16:00. Cada candle atualiza fechamento/dia/ADX; cruzamentos rejeitados não são adiados. Um novo cruzamento permite reentrada após fechar, sem limite diário ou espera de seis horas. Sem posições vendidas.

## Proteção e fechamento

`MaxProfit=150` e `MaxStopLoss=15` são distâncias absolutas de preço, não percentuais ou ticks do instrumento. Compra executada inicia entryPrice/highWater; compras parciais adicionais atualizam preço ponderado por volume. Reduções mantêm a base, posição zero reinicia.

Bid positivo executável ou fechamento concluído aumenta highWater ao máximo anterior/atual. Vende toda a posição comprada real se preço≤`highWater-MaxStopLoss` ou≥`entryPrice+MaxProfit`. Prioridade: fim de sessão, stop móvel, alvo. Não trata high de candle como cotação executável.

Timer de tempo de mercado de um minuto, bids e candles fecham em/depois de `SessionEnd` ou na próxima data UTC. Horários ordenados no mesmo dia;00:00–24:00 termina à meia-noite seguinte. Ordens pendentes bloqueiam duplicados; histórico dia/ADX segue. Callback que fecha retorna sem reentrar.

Monitoramento local, sem stop nativo de bolsa. Proteção por cotações entre candles exige Level1 bid; sem ele ficam os fechamentos. Execução pode diferir do limiar.

| Parâmetro | Padrão |
|---|---:|
| `MaxProfit` / `MaxStopLoss` | 150 / 15, distâncias absolutas positivas |
| `AdxLength` | 11, positivo |
| `SessionStart` / `SessionEnd` | 09:30 / 16:00 UTC, início incluído e fim excluído |
| `CandleType` | 5 minutos |

Testes C#/Python agrupam dias e calculam Wilder ATR/DI/DX/ADX independentemente no arquivo BTC/TON. Verificam cada entrada/saída real, ambos os níveis anteriores, rejeições ADX, história parcial inicial, execuções, stops/alvos por cotações e fechamento obrigatório. Não demonstram lucratividade.
