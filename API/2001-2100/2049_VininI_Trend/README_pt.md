# Estratégia VininI Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Descrição
Esta estratégia converte o consultor especialista original MQL **Exp_VininI_Trend** para o StockSharp. Usa o Índice de Canal de Commodities (CCI) para emular o oscilador VininI Trend. Uma posição comprada é aberta quando o oscilador ultrapassa o nível superior ou vira para cima. Uma posição vendida é aberta quando o oscilador cai abaixo do nível inferior ou vira para baixo. A estratégia trabalha apenas com candles concluídos.

## Parâmetros
- **CCI Period** – comprimento do indicador CCI.
- **Upper Level** – limiar que aciona sinais de compra.
- **Lower Level** – limiar que aciona sinais de venda.
- **Entry Modes** – `Breakdown` reage a cruzamentos de nível, `Twist` reage a mudanças de direção.
- **Candle Type** – período dos candles usados para os cálculos.

## Original
Convertido da estratégia MQL5 localizada em `MQL/1365/exp_vinini_trend.mq5`.
