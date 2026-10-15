# Estratégia Tipu MACD EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia é uma porta StockSharp de alto nível do **Tipu MACD EA** de MQL4. Ele negocia um único símbolo usando sinais baseados em MACD e reflete os recursos originais do consultor especialista:

* Filtro opcional de horário de negociação com duas janelas de tempo configuráveis.
* MACD entradas de cruzamento de linha zero e linha de sinal com comprimentos e deslocamento EMA ajustáveis.
* Gerenciamento automático de posição, incluindo take-profit, stop-loss, trailing stop e ponto de equilíbrio.
* Limite de volume que emula a configuração de "lotes máximos" do código-fonte.

Todas as operações utilizam ordens de mercado. Os níveis de proteção são rastreados internamente e as ordens são fechadas assim que uma vela ultrapassa os níveis de stop-loss ou take-profit.

## Lógica de negociação
1. Assine o tipo de vela configurado e calcule um indicador `MovingAverageConvergenceDivergenceSignal` (linha MACD + linha de sinal).
2. Avalie os valores MACD usando o deslocamento selecionado (`MacdShift` 0 = vela atual, 1 = vela anterior) e construa sinais de cruzamento:
   * **Cruzamento de linha zero** (opcional) – compre quando MACD cruzar acima de zero, venda quando cruzar abaixo.
   * **Cruzamento da linha de sinal** (opcional) – compre quando MACD cruzar acima da linha de sinal, venda quando cruzar abaixo.
3. Antes de abrir uma posição, certifique-se de que a hora atual pertence a pelo menos uma das duas janelas de horário quando o filtro está habilitado.
4. Quando um sinal longo aparece:
   * Se o hedge estiver desativado e uma posição curta estiver aberta, opcionalmente feche-a (`CloseOnReverseSignal`) ou pule a nova negociação.
   * Coloque uma ordem de compra a mercado para o menor valor entre `TradeVolume` e o volume restante até que `MaxPositionVolume` seja alcançado.
   * Atualize o instantâneo de entrada longa e calcule os níveis de parada/tomada de proteção, se habilitado.
5. Quando aparecer um sinal curto, siga a lógica simétrica para ordens de venda.
6. Enquanto uma posição está ativa:
   * Monitore paradas e metas em cada vela finalizada e feche a negociação se algum dos níveis for violado.
   * Quando o rastreamento estiver ativado e o preço avançar `TrailingPips + TrailingCushionPips`, mova o stop para manter a distância de `TrailingPips` do preço.
   * Quando o módulo de equilíbrio estiver ativo e o lucro exceder `RiskFreePips`, mova o stop para o preço de entrada.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Série de velas usada para cálculos MACD. |
| `TradeVolume` | Volume de cada entrada no mercado (lotes). |
| `MaxPositionVolume` | Exposição máxima cumulativa longa ou curta permitida. |
| `UseTimeFilter` | Ativa o filtro de horário de negociação de janela dupla. |
| `Zone1StartHour`, `Zone1EndHour` | Horário de início/término da primeira janela de negociação (inclusive horário de câmbio). |
| `Zone2StartHour`, `Zone2EndHour` | Horário de início/término da segunda janela de negociação. |
| `FastPeriod`, `SlowPeriod`, `SignalPeriod` | MACD EMA rápida, EMA lenta e comprimentos de sinal SMA. |
| `MacdShift` | 0 = avalia a barra atual, 1 = avalia a barra anterior (correspondendo a MQL `iShift`). |
| `UseZeroCross` | Ativa MACD entradas cruzadas de linha zero. |
| `UseSignalCross` | Permite MACD vs. entradas cruzadas de linha de sinal. |
| `AllowHedging` | Permite construir exposições longas e curtas sem fechar primeiro o lado oposto. |
| `CloseOnReverseSignal` | Fecha a posição oposta quando um novo sinal aparece (usado quando o hedge está desabilitado). |
| `UseTakeProfit`, `TakeProfitPips` | Habilita e configura a distância de take-profit (pips). |
| `UseStopLoss`, `StopLossPips` | Habilita e configura a distância de stop-loss (pips). |
| `UseTrailingStop`, `TrailingPips`, `TrailingCushionPips` | Permite gerenciamento de rastreamento, define distância de rastreamento e amortecimento (pips). |
| `UseRiskFree`, `RiskFreePips` | Move o stop para o ponto de equilíbrio quando o lucro excede os pips especificados. |

## Notas de uso
* Configure o tipo de vela para corresponder ao período usado em MetaTrader (barras padrão de 15 minutos).
* O tamanho do pip é derivado de `Security.PriceStep`. Se o instrumento não tiver esses metadados, será usado um padrão de 0,0001.
* A estratégia pressupõe a execução imediata das ordens de mercado. Ao operar ao vivo, garanta o manuseio adequado do deslizamento, se necessário.
* Quando as entradas da linha zero e da linha de sinal são desativadas, a estratégia permanece ociosa.
