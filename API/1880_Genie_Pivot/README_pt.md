# Estratégia Genie Pivot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa a ideia de scalping de reversão **Genie Pivot** originalmente escrita em MQL4. Ela aguarda um padrão de pivô formado por sete candles consecutivos e gerencia a posição aberta com um take profit fixo e um stop trailing.

## Lógica da Estratégia

1. **Detecção do padrão** – um sinal de compra aparece quando os sete mínimos anteriores são estritamente decrescentes e o último candle concluído forma um mínimo mais alto com fechamento acima da máxima anterior. Um sinal de venda é gerado pela condição espelhada nas máximas.
2. **Execução da ordem** – uma vez confirmado um sinal, a estratégia abre uma ordem a mercado com o volume calculado a partir do patrimônio da conta e dos parâmetros de risco configurados.
3. **Gestão da operação** – após a entrada, um take profit e um stop trailing são definidos. O stop trailing é ativado somente quando o lucro excede a distância de trailing. Se o preço reverter no candle seguinte (de baixa para comprado, de alta para vendido), a posição é fechada imediatamente.
4. **Redução de volume** – operações perdedoras consecutivas reduzem o volume negociado de acordo com o parâmetro `Decrease Factor`.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `TakeProfit` | Alvo de lucro em passos de preço a partir do preço de entrada. |
| `TrailingStop` | Distância do stop trailing em passos de preço. |
| `MaximumRisk` | Fração do valor da conta usada para dimensionar a posição. |
| `DecreaseFactor` | Reduz o volume após perdas consecutivas. |
| `BaseVolume` | Volume de fallback quando o valor do portfólio é desconhecido. |
| `CandleType` | Período dos candles a analisar. |

## Notas

A estratégia processa apenas candles concluídos. Nenhuma versão em Python foi disponibilizada ainda.
