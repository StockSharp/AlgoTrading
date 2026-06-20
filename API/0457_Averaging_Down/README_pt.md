# Estratégia de Promediação para Baixo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma posição quando o preço se move para fora de uma banda baseada em ATR ao redor do EMA. Se o mercado se mover contra a posição, a estratégia adiciona a ela usando desvios percentuais escalonados (DCA). O lucro é realizado quando o preço retorna à entrada média mais uma porcentagem fixa.

## Parâmetros
- Candle Type – tipo de velas a processar.
- EMA Length – período para o filtro de tendência EMA.
- ATR Length – período para ATR.
- ATR Mult – multiplicador para as bandas ATR.
- TP % – porcentagem de tomada de lucro a partir da entrada média.
- Base Deviation % – desvio inicial para o primeiro nível DCA.
- Step Scale – multiplicador aplicado ao desvio para cada novo nível DCA.
- DCA Size Multiplier – multiplicador de volume para cada ordem DCA.
- Max DCA Levels – número máximo de entradas de promediação.
- Initial Volume – volume da primeira ordem.
