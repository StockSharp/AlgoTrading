# Estratégia Up3x1 Krohabor D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia usa três médias móveis simples (rápida, média, lenta) para identificar a direção da tendência. Uma posição comprada é aberta quando a MA rápida cruza acima da MA média e ambas as MAs estão acima da MA lenta nas barras atual e anterior. Uma posição vendida é aberta quando a MA rápida cruza abaixo da MA média e ambas as MAs estão abaixo da MA lenta nas barras atual e anterior.

As posições são protegidas com níveis de take profit, stop loss e trailing stop opcional. As ordens são executadas a preços de mercado.

## Parâmetros
- **Volume** – tamanho da ordem.
- **Fast Period** – período da SMA rápida.
- **Middle Period** – período da SMA média.
- **Slow Period** – período da SMA lenta.
- **Take Profit** – distância até o alvo de lucro em unidades de preço.
- **Stop Loss** – distância até o stop de proteção em unidades de preço.
- **Trailing Stop** – distância para ativação do trailing stop em unidades de preço.
- **Candle Type** – período dos candles usados para cálculos.

## Sinais
- **Compra** – MA rápida cruza acima da MA média e ambas as MAs permanecem acima da MA lenta.
- **Venda** – MA rápida cruza abaixo da MA média e ambas as MAs permanecem abaixo da MA lenta.

## Proteções
- Níveis de take profit e stop loss são definidos na entrada.
- Quando habilitado, o trailing stop move o stop de proteção na direção da negociação conforme o preço avança.

## Notas
Esta é uma conversão direta da estratégia MQL original para StockSharp usando a API de alto nível e indicadores integrados.
