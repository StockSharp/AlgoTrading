# Premarket Gap MomoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Exemplo de impulso de candles apenas comprado, com uma entrada por data de Nova York. “Gap” significa aumento do fechamento sobre o fechamento anterior; não exige gap noturno de abertura ou cruzamento EMA.

## Entrada e tamanho

Somente candles concluídos, de cinco minutos por padrão. O primeiro registra fechamento e volume anteriores. A partir de posição zero exige candle estritamente de alta, intervalo high-low positivo, volume total > `MinVolume`, fechamento anterior positivo e `100 * (close - previousClose) / previousClose >= MinGainPct`.

`bodyPercent = 100 * (close - open) / (high - low)`: ≥90% solicita 100% de `Volume`; ≥85% e <90% solicita 50%; ≥75% e <85% solicita 25%; abaixo de 75% não entra. Limites inferiores são inclusivos.

Quando o instrumento fornece `VolumeStep` positivo, arredonda o tamanho para baixo até esse passo. Tamanho zero ou fora dos limites mínimo/máximo fornecidos é rejeitado. Não aumenta uma posição existente.

Com `UseSession=true`, o envio em horário de Nova York deve estar em `[SessionStart, SessionEnd)`, padrão 04:00–09:30, observando horário de verão. Desativar a sessão mantém o limite diário. Uma entrada enviada consome a permissão da data, mesmo se a ordem falhar depois.

## Manutenção e saída

O primeiro candle vermelho (`close < open`) ou com `volume <= previousVolume` fecha toda a posição comprada real. Candles de alta com volume crescente mantêm a posição; doji também mantém se o volume cresce. Saídas valem fora da sessão de entrada, independentemente de ganho, corpo ou volume mínimo. Não há nova entrada no candle de saída. Ordens pendentes bloqueiam duplicados; os valores anteriores continuam atualizados em todo candle concluído.

Sem indicadores, entradas vendidas, intervalo obrigatório de seis horas ou stop-loss/take-profit separados. Outra entrada exige a próxima data elegível de Nova York.

## Valores padrão

| Parâmetro | Padrão | Significado |
|---|---:|---|
| `MinGainPct` | 5 | Ganho percentual mínimo inclusivo; não negativo |
| `MinVolume` | 15000 | Volume mínimo estrito; não negativo, nas unidades dos dados |
| `UseSession` | true | Filtro de horário de entrada; propriedade C# `IsUseSession` |
| `SessionStart` / `SessionEnd` | 04:00 / 09:30 | Horários ordenados no mesmo dia de Nova York, início incluído e fim excluído |
| `CandleType` | 5 minutos | Candles de sinal concluídos |

Testes usam o arquivo BTC/TON existente para verificar cada ordem/posição real, três tamanhos de corpo, arredondamento de lotes, limite diário, ambas as saídas, manutenções e transição primaveral do horário de verão. TON tem candle válido com os valores publicados 5%/15000; testes mecânicos BTC ajustam explicitamente os limiares. Não demonstram lucratividade.
