# Estratégia SAR Automatizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo demonstra uma abordagem de trading simples baseada no indicador **Parabolic SAR**.
A estratégia abre uma posição comprada quando o preço atual está acima do valor SAR e abre uma posição vendida quando o preço está abaixo do SAR. Recursos adicionais de gerenciamento de risco incluem stop-loss fixo, take-profit e um trailing stop opcional.

## Parâmetros
- `SarStep` – fator de aceleração para o cálculo do SAR.
- `SarMax` – fator de aceleração máximo para o SAR.
- `StopLoss` – distância do stop-loss em unidades de preço.
- `TakeProfit` – distância do take-profit em unidades de preço.
- `TrailingStop` – distância do trailing stop em unidades de preço.
- `CandleType` – tipo de velas usadas para cálculos do indicador.

## Lógica de trading
1. Inscrever-se em velas e calcular os valores do Parabolic SAR.
2. **Entrada**:
   - Comprar quando o SAR estiver abaixo do preço de fechamento e não houver posição.
   - Vender quando o SAR estiver acima do preço de fechamento e não houver posição.
3. **Saída**:
   - Fechar a posição quando o preço atingir o nível SAR oposto.
   - Aplicar as regras de stop-loss, take-profit e trailing stop.

Esta estratégia é destinada a fins educacionais e mostra como usar indicadores e controles de risco com a API de alto nível do StockSharp.
