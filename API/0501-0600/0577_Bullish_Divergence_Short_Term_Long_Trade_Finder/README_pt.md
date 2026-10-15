# Localizador de Operações Compradas de Curto Prazo por Divergência Altista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca divergências altistas entre o preço e o RSI. Quando o preço marca uma mínima mais baixa mas o RSI forma uma mínima mais alta dentro de um intervalo de pivô especificado e o RSI horário está abaixo de 40, a estratégia entra em uma posição comprada. A posição é fechada quando o RSI sobe acima de um limiar, aparece uma divergência baixista ou o stop loss é atingido.

- **Condições de entrada**:
  - A mínima atual está abaixo do preço da mínima do pivô anterior.
  - RSI forma uma mínima mais alta abaixo de `RsiBullConditionMin` e o pivô anterior ocorre dentro de 5–50 barras.
  - O RSI horário está abaixo de `RsiHourEntryThreshold`.
  - O preço de fechamento está abaixo do preço da mínima do pivô anterior.
- **Condições de saída**:
  - RSI cruza acima de `SellWhenRsi`.
  - Divergência baixista: preço marca uma máxima mais alta enquanto o RSI marca uma máxima mais baixa.
  - Stop loss ativado via `StartProtection` em `StopLossPercent`.
- **Indicadores**: RSI.
