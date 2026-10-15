# Estratégia Marneni Money Tree
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia traduz o consultor especialista MQL "Marneni Money Tree" para o StockSharp.
Baseia-se em uma média móvel simples (SMA) de 40 períodos e dois valores deslocados para detectar a direção da tendência.
Quando a SMA deslocada por quatro barras está entre a SMA atual e o valor de trinta barras atrás,
- uma ordem de mercado é enviada na direção detectada;
- oito ordens limitadas adicionais são colocadas em distâncias crescentes, definidas por `Order2Pips` até `Order9Pips`.

Configurações compradas colocam limites de compra abaixo do preço atual. Configurações vendidas colocam limites de venda acima do preço.
As posições são fechadas e as ordens restantes canceladas quando a relação da SMA se inverte.

## Parâmetros
- `Order2Pips`–`Order9Pips` — distância em pips para as ordens limitadas de 2 a 9.
- `CandleType` — período utilizado para os cálculos.

O volume base de negociação é fixado em 2 e pode ser ajustado alterando a propriedade `Volume` antes de iniciar a estratégia.
