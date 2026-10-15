# Estratégia EurGbp EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia EurGbp EA espelha o expert advisor original do MetaTrader comparando o momentum MACD horário de EUR/USD e GBP/USD enquanto negocia no instrumento primário configurado (normalmente EUR/GBP). A abordagem explora a força relativa entre os principais pares de euro e libra para antecipar movimentos no par cruzado.

## Indicadores
* **MACD (12, 26, 9)** em EUR/USD (sinal e histograma).
* **MACD (12, 26, 9)** em GBP/USD (sinal e histograma).

Ambos os indicadores são avaliados no mesmo timeframe selecionado pelo parâmetro `Candle Type` (padrão de 1 hora).

## Lógica de negociação
1. Assinar candles do ativo negociado mais EUR/USD e GBP/USD.
2. Calcular sinal e histograma MACD para ambos os pares de referência.
3. **Condição de compra:**
   * Histograma EUR/USD &lt; histograma GBP/USD, **e**
   * Sinal EUR/USD &gt; sinal GBP/USD,
   * Nenhuma posição comprada existente (ou uma posição vendida existente que será zerada).
4. **Condição de venda:**
   * Histograma GBP/USD &lt; histograma EUR/USD, **e**
   * Sinal GBP/USD &gt; sinal EUR/USD,
   * Nenhuma posição vendida existente (ou uma posição comprada existente que será zerada).
5. Apenas uma operação por barra em cada direção é permitida para evitar entradas duplicadas.
6. Ordens de stop-loss e take-profit são anexadas imediatamente após a entrada usando as distâncias em pontos configuradas.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| Candle Type | Timeframe para todas as assinaturas de candles. | 1 hora |
| EURUSD Security | Instrumento que fornece candles EUR/USD. | Deve ser definido |
| GBPUSD Security | Instrumento que fornece candles GBP/USD. | Deve ser definido |
| Volume | Volume da ordem (lotes). | 0.01 |
| Stop Loss | Stop protetor em passos de preço. | 75 |
| Take Profit | Meta de lucro em passos de preço. | 46 |

## Gestão de risco
* `Stop Loss` e `Take Profit` são medidos em passos de preço do ativo negociado. Garanta que o ativo tenha um valor `PriceStep` válido.
* A proteção começa automaticamente quando a estratégia inicia (`StartProtection`).
* Se qualquer distância for zero, a respectiva ordem protetora é ignorada.

## Notas de uso
* Atribua o ativo principal de negociação à instância da estratégia antes de iniciar (por exemplo, EUR/GBP).
* Configure `EURUSD Security` e `GBPUSD Security` para referenciar fontes de dados disponíveis na sua conexão.
* A estratégia exige dados sincronizados para os três ativos no timeframe selecionado para gerar sinais com confiabilidade.
* Apenas ordens a mercado são usadas. Posições opostas existentes são fechadas enviando o volume inverso.

## Notas de conversão
* As entradas originais `_Lots`, `_SL`, `_TP`, `_MagicNumber`, `_Comment`, `_OnlyOneOpenedPos` e `_AutoDigits` são mapeadas para parâmetros StockSharp ou comportamento integrado.
* Rotinas auxiliares de fechamento de ordens da versão MQL são substituídas pela gestão de ordens protetoras de alto nível do StockSharp.
* Tratamento de erros e laços de nova tentativa do código MQL são omitidos porque o modelo de execução StockSharp já gerencia estados de ordens e retentativas.
