# Estratégia de Sinal LeMan
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia de Sinal LeMan é uma portabilização do consultor especialista LeManSignal original do MetaTrader. A abordagem analisa máximas e mínimas recentes em dois períodos sequenciais para detectar possíveis reversões de tendência. Quando padrões específicos são encontrados, uma posição comprada ou vendida é aberta no próximo candle.

## Como funciona

1. A estratégia observa candles completos do período de tempo selecionado.
2. Para a barra anterior, compara as máximas mais altas e mínimas mais baixas em dois intervalos consecutivos:
   - `H1` e `H2` são as máximas de dois intervalos adjacentes.
   - `H3` e `H4` são as máximas do próximo par de intervalos.
   - `L1` e `L2` são as mínimas de dois intervalos adjacentes.
   - `L3` e `L4` são as mínimas do próximo par de intervalos.
3. Um sinal de **compra** é acionado se `H3 <= H4` e `H1 > H2`.
4. Um sinal de **venda** é acionado se `L3 >= L4` e `L1 < L2`.
5. As ordens são executadas ao preço de mercado. Qualquer posição oposta aberta é fechada automaticamente.
6. O gerenciamento de risco opcional é aplicado por meio de `StartProtection` com valores padrão de stop-loss e take-profit de 1% e 2% respectivamente.

## Parâmetros

- **Period** – comprimento do lookback do indicador.
- **Signal Bar** – deslocamento usado para confirmar o sinal (padrão 1).
- **Candle Type** – período dos candles a serem analisados.

## Observações

- A estratégia reage apenas a candles concluídos.
- Não mantém coleções adicionais; os buffers internos são limitados ao mínimo necessário para os cálculos.
- Para usar a estratégia, adicione-a a um terminal StockSharp, defina o instrumento e os parâmetros desejados e inicie a estratégia.
