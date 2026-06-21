# Estratégia de Centro de Gravidade OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o oscilador **Center of Gravity OSMA** para detectar potenciais reversões de tendência.
O oscilador multiplica médias móveis simples e ponderadas, suaviza o resultado duas vezes e rastreia
mudanças de direção. Quando o indicador forma um mínimo local e vira para cima, a estratégia
fecha posições vendidas e pode abrir uma nova posição comprada. Quando um máximo local vira para baixo,
as posições compradas são fechadas e shorts opcionais são abertos.

## Como Funciona
1. O preço de fechamento é usado como entrada para o indicador personalizado.
2. O indicador calcula:
   - Média móvel simples (`SMA`) com comprimento `Period`.
   - Média móvel ponderada (`WMA`) com o mesmo comprimento.
   - Produto dessas duas médias.
   - Dois passos de suavização adicionais com comprimentos `SmoothPeriod1` e `SmoothPeriod2`.
3. Regras de negociação:
   - Se o valor anterior era menor que o valor antes dele e o valor atual é maior que o anterior, o oscilador virou para cima. Qualquer posição vendida é fechada e uma comprada pode ser aberta.
   - Se o valor anterior era maior que o valor antes dele e o valor atual é menor que o anterior, o oscilador virou para baixo. Qualquer posição comprada é fechada e uma vendida pode ser aberta.
   - Valores opcionais de stop loss e take profit em unidades de preço protegem as posições abertas.

## Parâmetros
- `Period` – período base para SMA e WMA.
- `SmoothPeriod1` – comprimento da primeira etapa de suavização.
- `SmoothPeriod2` – comprimento da segunda etapa de suavização.
- `StopLoss` – distância do stop loss em unidades de preço (0 para desativar).
- `TakeProfit` – distância do take profit em unidades de preço (0 para desativar).
- `BuyPosOpen` – permitir abertura de posições compradas.
- `SellPosOpen` – permitir abertura de posições vendidas.
- `BuyPosClose` – permitir fechar posições compradas em um sinal de venda.
- `SellPosClose` – permitir fechar posições vendidas em um sinal de compra.
- `CandleType` – tipo de candle (período) para cálculos.

## Notas
- Apenas a versão em C# é fornecida. A pasta Python está intencionalmente ausente.
- Use tabulações para indentação ao modificar o código.
