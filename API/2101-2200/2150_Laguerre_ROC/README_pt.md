# Estratégia Laguerre ROC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o oscilador de taxa de variação Laguerre para capturar reversões de tendência.

O oscilador Laguerre ROC suaviza a taxa de variação por meio de um filtro Laguerre de quatro estágios.
Os valores são normalizados entre 0 e 1. Dois limiares definem as zonas de sobrecompra e sobrevenda:

- **Up Level** – valores acima deste nível indicam forte momentum de alta.
- **Down Level** – valores abaixo deste nível indicam forte momentum de baixa.

Lógica de trading:

1. Quando o oscilador cai da zona de sobrecompra (valor anterior acima de Up Level
   e valor atual abaixo) a estratégia entra em uma posição comprada.
2. Quando o oscilador sobe da zona de sobrevenda (valor anterior abaixo de Down Level
   e valor atual acima) a estratégia entra em uma posição vendida.
3. Se uma posição comprada estiver aberta e o oscilador ficar de baixa (valor anterior abaixo
   do nível neutro de 0.5) a posição é fechada.
4. Se uma posição vendida estiver aberta e o oscilador ficar de alta (valor anterior acima
   de 0.5) a posição é fechada.

Parâmetros:

- **Period** – comprimento do período de retrovisão para o cálculo da taxa de variação.
- **Gamma** – fator de suavização para o filtro Laguerre.
- **Up Level** – limiar de sobrecompra.
- **Down Level** – limiar de sobrevenda.
- **Candle Type** – período utilizado para os dados de velas.

O exemplo demonstra como a lógica de indicador personalizado pode ser recriada dentro de uma
estratégia StockSharp de alto nível usando taxa de variação embutida e filtragem Laguerre manual.
