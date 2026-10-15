# Estratégia Lego 4 Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um sistema modular traduzido do script MetaTrader "exp_Lego_4_Beta". Combina vários indicadores técnicos comuns e permite habilitar ou desabilitar cada componente por meio de parâmetros.

## Algoritmo

1. **Cruzamento de Médias Móveis** – Uma média móvel rápida e uma lenta são calculadas. Uma posição comprada é aberta quando a média rápida cruza acima da lenta. Uma posição vendida é aberta no cruzamento oposto.
2. **Filtro do Oscilador Estocástico** – Quando habilitado, entradas compradas requerem que o valor %K do Estocástico esteja abaixo do nível de sobrevenda, e entradas vendidas requerem que %K esteja acima do nível de sobrecompra.
3. **Saída por RSI** – Quando habilitado, posições compradas existentes são fechadas se o RSI subir acima do limiar alto. Posições vendidas são fechadas quando o RSI cair abaixo do limiar baixo.

## Parâmetros

- `UseMaOpen` – ativar sinais de cruzamento de médias móveis.
- `FastMaLength` / `SlowMaLength` – comprimentos das médias rápida e lenta.
- `MaType` – tipo de média móvel (SMA, EMA, WMA).
- `UseStochasticOpen` – habilitar filtro estocástico para entradas.
- `StochLength` – período principal para cálculo do Estocástico.
- `StochKPeriod` / `StochDPeriod` – períodos de suavização para as linhas %K e %D.
- `StochBuyLevel` / `StochSellLevel` – limiares de sobrevenda e sobrecompra.
- `UseRsiClose` – habilitar saídas baseadas em RSI.
- `RsiPeriod` – comprimento do cálculo de RSI.
- `RsiHigh` / `RsiLow` – limiares de RSI para fechar posições.
- `CandleType` – tipo de candle para assinatura.

## Notas

A estratégia usa `SubscribeCandles` de alto nível com `BindEx` para processar valores de indicadores e segue o estilo recomendado do StockSharp. Apenas ordens de mercado são usadas para entrada e saída.
