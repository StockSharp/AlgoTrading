# Estratégia N Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia N Candles replica o consultor especialista MQL que entra em uma operação quando um número configurável de candles consecutivos compartilham a mesma direção. Assim que os `N` candles concluídos mais recentes são todos de alta, a estratégia envia uma ordem de compra de mercado. Quando todos são de baixa, envia uma ordem de venda de mercado. Nenhuma lógica de saída está incluída; a posição deve ser gerenciada externamente ou por estratégias adicionais.

## Visão geral

- **Regime de mercado**: Funciona melhor em mercados que exibem curtos surtos de momentum.
- **Instrumentos**: Qualquer instrumento que suporte trading contínuo (FX, futuros, cripto).
- **Períodos**: Configuráveis; padrão são candles de 1 hora.
- **Tipos de ordens**: Ordens de mercado sem stops protetores ou alvos.

## Como funciona

1. Em cada candle concluído a estratégia avalia os últimos `N` candles.
2. Se cada candle nessa janela é de alta, emite uma ordem de compra de mercado com o volume configurado.
3. Se cada candle é de baixa, emite uma ordem de venda de mercado.
4. Candles doji (abertura igual ao fechamento) reiniciam a contagem e suprimem o trading até que uma nova sequência se forme.
5. A estratégia não gerencia posições abertas; sinais repetidos adicionam à direção existente em contas de compensação neta.

## Parâmetros

- **Consecutive Candles**: Número de candles idênticos necessários antes de colocar uma ordem.
- **Volume**: Tamanho da ordem de mercado enviada em cada sinal.
- **Candle Type**: Série de candles usada para detecção de sequência (timeframe ou tipo de candle personalizado).

## Notas de uso

- Como a estratégia não tem stops ou saídas, combine-a com gestão manual, estratégias protetoras ou controles de risco de portfólio.
- Em mercados altamente voláteis considere reduzir a contagem de candles ou o timeframe para capturar sequências mais rápidas.
- Sequências consecutivas excessivas podem acumular posições grandes; monitore a alavancagem e os limites da conta.
