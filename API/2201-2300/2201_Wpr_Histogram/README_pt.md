# Estratégia de Histograma WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no comportamento do indicador Williams %R. Monitoriza quando o indicador abandona as zonas de sobrecompra ou sobrevenda e abre posições na direção oposta.

## Lógica

- Quando Williams %R sobe acima do nível alto e depois desce, considera-se que o mercado abandona a zona de sobrecompra. A estratégia abre uma posição comprada.
- Quando Williams %R cai abaixo do nível baixo e depois sobe, o mercado abandona a zona de sobrevenda. A estratégia abre uma posição vendida.
- As posições opostas existentes são fechadas antes de abrir uma nova.

## Parâmetros

- **WPR Period** – período de cálculo de Williams %R.
- **High Level** – limiar para a zona de sobrecompra.
- **Low Level** – limiar para a zona de sobrevenda.
- **Candle Type** – tipo e período das velas utilizadas para os cálculos.

## Notas

A estratégia utiliza apenas ordens de mercado e não define níveis de stop-loss ou take-profit. O dimensionamento de posições depende da propriedade `Volume` definida pelo utilizador.
