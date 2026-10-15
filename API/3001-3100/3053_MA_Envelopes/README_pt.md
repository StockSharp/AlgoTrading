# Estratégia de MA com Envelopes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do especialista do MetaTrader 5 "MA Envelopes". A estratégia busca retrações de preço em direção a uma média móvel envolvida por um canal de envelopes. Quando uma vela concluída fecha entre a média móvel e uma das bandas do envelope durante a janela de trading configurada, a estratégia coloca entradas limitadas na média móvil com ordens de saída protetoras derivadas do envelope.

## Lógica de trading

1. Uma média móvel é calculada com o método, fonte de preço e período selecionados. O mesmo valor é usado para construir bandas simétricas de envelope usando o parâmetro de desvio.
2. Quando uma vela concluída fecha acima da média móvil mas abaixo da banda superior do envelope e o preço ask atual permanece acima da média móvil, uma sequência escalonada de ordens de compra limitadas é preparada no preço da média móvil.
   * Cada compra limitada usa o envelope inferior como nível de stop-loss e o envelope superior mais um offset adicional em pips como take-profit.
   * Até três ordens independentes são gerenciadas, cada uma com seu próprio offset de take-profit (parâmetros SL/TP de `First`, `Second`, `Third`).
3. Quando uma vela concluída fecha abaixo da média móvil mas acima da banda inferior do envelope e o preço bid atual permanece abaixo da média móvil, a lógica é espelhada para ordens de venda limitadas.
4. A janela de trading é controlada por `StartHour` e `EndHour` (hora do terminal). Após a hora de fim, todas as ordens de entrada ainda ativas são canceladas.
5. O risco por trade é estimado através de `MaximumRisk` e reduzido após perdas consecutivas usando `DecreaseFactor`. O volume da ordem é alinhado ao passo de volume e aos limites do instrumento.
6. Uma vez que uma ordem de entrada está completamente preenchida, as ordens de stop-loss e take-profit protetoras são registradas imediatamente. Se uma ordem de saída for acionada, a ordem contraparte é cancelada e, se houver volume de posição restante, novas ordens protetoras são emitidas para o restante.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `MaximumRisk` | Fração do capital disponível arriscado por posição. |
| `DecreaseFactor` | Reduz o tamanho da posição após trades perdedores consecutivos. |
| `First/Second/ThirdStopTakeProfitPips` | Distâncias em pips adicionadas às bandas do envelope para as três ordens escalonadas. |
| `StartHour`, `EndHour` | Limites da sessão de trading em hora do terminal (0–23). |
| `MaPeriod`, `MaShift`, `MaMethodType`, `AppliedPrice` | Configuração da média móvil. |
| `EnvelopeDeviation` | Largura do canal de envelope em porcentagem. |
| `CandleType` | Período das velas usadas para os cálculos. |

## Notas

* Ordens protetoras são recriadas sempre que apenas parte de uma posição é fechada, mantendo o tamanho restante coberto.
* Ordens de entrada pendentes são canceladas ao final da sessão; posições abertas continuam sendo gerenciadas por suas ordens protetoras.
* A estratégia depende de atualizações do livro de ordens para capturar os últimos preços bid/ask; os valores de fechamento das velas são usados como alternativa quando os dados do livro de ordens não estão disponíveis.
