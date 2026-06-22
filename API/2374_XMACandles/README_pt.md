# Estratégia XMA Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Descrição
A estratégia XMA Candles monitora a direção de velas suavizadas calculadas a partir da XMA (Média Móvel Exponencial) dos preços de abertura e fechamento. Uma vela é considerada **de alta** quando o preço de abertura suavizado está abaixo do preço de fechamento suavizado, e **de baixa** quando o preço de abertura suavizado está acima. A estratégia reage às mudanças de cor dessas velas suavizadas.

- Quando uma nova vela de alta aparece após uma vela não-alta, a estratégia fecha qualquer posição vendida e abre uma posição comprada.
- Quando uma nova vela de baixa aparece após uma vela não-baixa, a estratégia fecha qualquer posição comprada e abre uma posição vendida.

## Parâmetros
- `Length` – número de períodos para suavizar os preços de abertura e fechamento.
- `CandleType` – período das velas utilizadas nos cálculos.
- `BuyPosOpen` – permite abrir posições compradas.
- `SellPosOpen` – permite abrir posições vendidas.
- `BuyPosClose` – permite fechar posições compradas quando aparece sinal de baixa.
- `SellPosClose` – permite fechar posições vendidas quando aparece sinal de alta.
- `StopLoss` – stop de proteção em percentagem.
- `TakeProfit` – alvo de lucro em percentagem.

## Regras de trading
1. Aguardar o fechamento de cada vela do período selecionado.
2. Calcular as médias móveis exponenciais para os preços de abertura e fechamento.
3. Determinar a cor da vela:
   - Verde (alta) se abertura suavizada < fechamento suavizado.
   - Vermelha (baixa) se abertura suavizada > fechamento suavizado.
4. Se a cor mudar para alta, fechar vendidos e opcionalmente abrir uma posição comprada.
5. Se a cor mudar para baixa, fechar comprados e opcionalmente abrir uma posição vendida.
6. Stops de proteção e alvos são gerenciados pelos controles de risco integrados.

Esta estratégia é uma conversão do expert MQL5 original "Exp_XMACandles".
