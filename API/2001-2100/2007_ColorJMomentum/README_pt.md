# Estratégia ColorJMomentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia ColorJMomentum** negocia com base nas mudanças de direção de um indicador de Momentum suavizado com Jurik. A abordagem é derivada do consultor especialista MQL5 original `Exp_ColorJMomentum` e reproduzida usando a API de alto nível do StockSharp.

## Conceito

1. Calcular o *Momentum* padrão da série de preços selecionada.
2. Suavizar os valores de Momentum com a **Jurik Moving Average (JMA)**.
3. Monitorar os dois últimos valores do Momentum suavizado:
   - Se o indicador estava declinando e vira para cima, uma posição **comprada** é aberta.
   - Se o indicador estava subindo e vira para baixo, uma posição **vendida** é aberta.
4. A proteção de posição é tratada por stop loss e take profit opcionais em termos percentuais.

A estratégia nunca lê valores históricos do indicador diretamente. Em vez disso, reage apenas a novos fechamentos de candles e armazena valores anteriores internamente.

## Parâmetros

- **Momentum Length** – período para o cálculo do Momentum.
- **JMA Length** – período de suavização da Jurik Moving Average aplicada ao Momentum.
- **Candle Type** – período usado para assinaturas de candles.
- **Stop Loss %** – percentual para stop loss opcional.
- **Enable Stop Loss** – se o stop loss deve ser ativado.
- **Take Profit %** – percentual para take profit.
- **Enable Long** – permitir abertura de posições compradas.
- **Enable Short** – permitir abertura de posições vendidas.

Todos os parâmetros são criados com `StrategyParam` para que possam ser otimizados no Designer.

## Uso

1. Anexar a estratégia ao instrumento desejado.
2. Configurar os parâmetros ou deixar os valores padrão (Momentum de 8 períodos e JMA de 8 períodos em candles de 8 horas).
3. Executar a estratégia. Ordens serão emitidas via `BuyMarket` e `SellMarket` quando a direção do Momentum se reverter.

## Notas

- A estratégia processa apenas candles finalizados.
- Nenhuma cor explícita é definida para os indicadores – o Designer as escolhe automaticamente.
- O algoritmo evita qualquer LINQ ou coleções personalizadas, seguindo as diretrizes do projeto.
