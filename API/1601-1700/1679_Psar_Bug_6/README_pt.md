# Estratégia Psar Bug 6
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do script MQL4 "psar_bug_6".

## Lógica
- Usa o indicador Parabolic SAR com passo e aceleração máxima configuráveis.
- Compra quando o preço fecha acima do SAR e anteriormente estava abaixo.
- Vende quando o preço fecha abaixo do SAR e anteriormente estava acima.
- O parâmetro de reversão opcional inverte os sinais de compra/venda.
- A opção `SarClose` fecha a posição existente quando o SAR muda para o lado oposto.
- Distâncias fixas de take-profit e stop-loss em unidades de preço. O trailing stop pode ser ativado.

## Parâmetros
- `SarStep` – passo do fator de aceleração.
- `SarMax` – fator de aceleração máximo.
- `StopLoss` – distância inicial do stop-loss.
- `TakeProfit` – distância do take-profit.
- `Trailing` – ativar trailing stop.
- `TrailStop` – distância do trailing stop quando ativado.
- `SarClose` – fechar posição na reversão do SAR.
- `Reverse` – inverter sinais de trading.
- `CandleType` – tipo de vela para cálculos.

## Notas
A estratégia usa a API de alto nível com assinaturas de velas e vinculação de indicadores. A proteção é iniciada com trailing stop opcional e saídas com ordens de mercado.
