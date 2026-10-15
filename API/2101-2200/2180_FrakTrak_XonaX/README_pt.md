# Estratégia FrakTrak XonaX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

FrakTrak XonaX é uma estratégia de rompimento baseada em níveis de fractais calculados em um período superior. Quando o preço se move além do fractal mais recente por um pequeno offset, a estratégia entra na direção do rompimento. Um take profit fixo e um trailing stop gerenciam a posição aberta.

## Parâmetros
- **Volume** – tamanho da ordem.
- **Take Profit** – distância em pontos para o nível de take-profit.
- **Trailing Stop** – distância em pontos usada para o trailing do stop-loss.
- **Trailing Correction** – distância adicional adicionada ao trailing stop.
- **Candle Type** – período utilizado para construir candles e fractais.

## Regras de trading
1. Calcular fractais superiores e inferiores usando os últimos candles concluídos.
2. Comprar quando o preço de fechamento excede o fractal superior mais 15 pontos e não existe posição comprada. O stop-loss é colocado no último fractal inferior e o take-profit é configurado usando *Take Profit*.
3. Vender quando o preço de fechamento cai abaixo do fractal inferior menos 15 pontos e não existe posição vendida. O stop-loss é colocado no último fractal superior e o take-profit é configurado usando *Take Profit*.
4. Quando uma posição se torna lucrativa em mais de *Trailing Stop* pontos, o stop-loss acompanha o preço com um offset adicional de *Trailing Correction*.
